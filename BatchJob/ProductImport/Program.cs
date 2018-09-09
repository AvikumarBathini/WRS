using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using WRS.Xrm;
using System.Data;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace ProductImport
{
    class Program
    {
        public static IOrganizationService CrmService = serviceClient();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        List<PriceGroup> PriceGroupLists = new List<PriceGroup>();
        private string NCId;

        static void Main(string[] args)
        {
            Program p = new Program();

            p.UpsertDiscount(p);

            p.UpsertProductAndProductFamilies(p);

            p.UpsertDiscountProducts(p);

            p.CreatePriceListItem(p);

            p.UpdatePriceListItem(p);

            p.UpdateProductStoreMapping(p);
        }

        private void UpdateProductStoreMapping(Program p)
        {
            ExecuteMultipleRequest multipleDisassociateReq;
            ExecuteMultipleRequest multipleAssociateReq = multipleDisassociateReq = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            DataTable ProductStoreMappingTable = RetrieveRecordsFromDB(Constant.ProductStoreMappingStoreProcName);
            EntityCollection ProductStoreMappingCRM = RetrieveNNRecordsFromCRM(Constant.Product, Constant.Store, Constant.ProductStoreRelationship, new string[] { "wrs_id", "productid" }, new string[] { "wrs_id", "wrs_storeid" });
            if (ProductStoreMappingTable != null && ProductStoreMappingTable.Rows.Count > 0)
            {
                foreach (DataRow dr in ProductStoreMappingTable.Rows)
                {
                    var productId = dr["Product_Id"];
                    var storeId = dr["StoreId"];
                    var published = dr["Published"];
                    if (productId != DBNull.Value && int.Parse(productId.ToString()) > 0 && (bool)published)
                    {
                        var MappingColl = ProductStoreMappingCRM.Entities.Where(e => e.GetAttributeValue<int>("wrs_id") == int.Parse(productId.ToString()) && int.Parse(e.FormattedValues["wrs_store2.wrs_id"]) == int.Parse(storeId.ToString()));
                        if (MappingColl != null && MappingColl.Count() > 0)
                        {
                            Entity entity = (Entity)MappingColl.FirstOrDefault();
                            if (entity != null)
                                ProductStoreMappingCRM.Entities.Remove(entity);
                            else
                                multipleAssociateReq.Requests.Add(PrepareProductStoreAssociateRequest(int.Parse(productId.ToString()), int.Parse(entity.FormattedValues["wrs_store2.wrs_id"])));
                        }
                        else
                        {
                            multipleAssociateReq.Requests.Add(PrepareProductStoreAssociateRequest(int.Parse(productId.ToString()), int.Parse(storeId.ToString())));
                        }
                    }
                }
                if (multipleAssociateReq.Requests.Count > 0)
                {
                    ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleAssociateReq);
                    foreach (var e in emrResponse.Responses)
                    {
                        if (e.Fault != null)
                        {
                            NCId = (((AssociateRequest)(multipleAssociateReq.Requests[e.RequestIndex]))).Target.KeyAttributes["wrs_id"].ToString();
                            string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + "[Product_Store_Mapping]" + "','" + "Product_Id " + NCId + "'";
                            WriteLogsToDB(errorLog);
                        }
                    }
                }
                if (ProductStoreMappingCRM.Entities.Count > 0)
                {
                    foreach (Entity e in ProductStoreMappingCRM.Entities)
                    {
                        multipleDisassociateReq.Requests.Add(PrepareProductStoreDisassociateRequest(e, Constant.ProductStoreRelationship));
                    }
                    ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleDisassociateReq);
                    List<string> existingRecords = new List<string>();
                    foreach (var e in emrResponse.Responses)
                    {
                        if (e.Fault != null)
                        {
                            NCId = (((((AssociateRequest)(multipleDisassociateReq.Requests[e.RequestIndex])))).Target.KeyAttributes.Values).FirstOrDefault().ToString();
                            string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + "[Product_Store_Mapping]" + "','" + "Product_Id: " + NCId + "'";
                            WriteLogsToDB(errorLog);
                        }
                    }
                }

            }
        }

        private AssociateRequest PrepareProductStoreAssociateRequest(int productID, int storeID)
        {
            EntityReference Target = new EntityReference(Product.EntityLogicalName, "wrs_id", productID);
            EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
            relatedEntities.Add(new EntityReference(wrs_store.EntityLogicalName, "wrs_id", storeID));
            Relationship relationship = new Relationship(Constant.ProductStoreRelationship);
            return CreateAssociateRequest(Target, relatedEntities, relationship);
        }

        public DisassociateRequest PrepareProductStoreDisassociateRequest(Entity e, string relation)
        {
            EntityReference target = new EntityReference(Product.EntityLogicalName, (Guid)e.Attributes["productid"]);
            EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
            relatedEntities.Add(new EntityReference(wrs_store.EntityLogicalName, new Guid(e.GetAttributeValue<AliasedValue>("wrs_store2.wrs_storeid").Value.ToString())));
            // Create an object that defines the relationship 
            Relationship relationship = new Relationship(relation);
            return CreateDisassociateRequest(target, relatedEntities, relationship);
        }

        private void ProductsToDraft(Program p)
        {
            //Retrieve records from NC DB
            DataTable productsTable = RetrieveRecordsFromDB(Constant.ProductsToDraftStoreProcName);
            if (productsTable != null && productsTable.Rows.Count > 0)
            {
                ProductsToActiveUnderRevision(p, productsTable);
            }
        }

        private void ProductsToActiveUnderRevision(Program p, DataTable table)
        {
            try
            {
                int totalCount = table.Rows.Count;
                if (totalCount > 0)
                {
                    int requests = (totalCount / batchSize) + 1; //Maximum parallel requests
                    List<ExecuteMultipleRequest> _executeMultipleRequestcollection = new List<ExecuteMultipleRequest>();
                    for (int i = 0; i < requests; i++)
                    {
                        ExecuteMultipleRequest multipleReq = new ExecuteMultipleRequest()
                        {
                            Settings = new ExecuteMultipleSettings()
                            {
                                ContinueOnError = true,
                                ReturnResponses = true
                            },
                            Requests = new OrganizationRequestCollection()
                        };
                        //Add (requests) set of ExecuteMultipleRequest in _executeMultipleRequestcollection
                        _executeMultipleRequestcollection.Add(multipleReq);
                    }
                    for (int j = 0; j < totalCount; j++)
                    {
                        DataRow dr = table.Rows[j];
                        if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
                        {
                            string[] Store_Ids = dr["Store_Ids"].ToString().Split(',');
                            bool isOneStore = Store_Ids.Contains(Constant.OneStoreId); //Check if the product is One Store
                            if (!isOneStore)
                            {
                                if (dr["ProductTypeId"] != DBNull.Value && int.Parse(dr["ProductTypeId"].ToString()) == 10)
                                {
                                    try
                                    {
                                        PublishProductHierarchyRequest Req = new PublishProductHierarchyRequest
                                        {
                                            Target = new EntityReference(Product.EntityLogicalName, "wrs_id", int.Parse(dr["id"].ToString()))
                                        };
                                        CrmService.Execute(Req);
                                    }
                                    catch (Exception ex)
                                    {
                                        continue;
                                    }
                                }
                                else
                                    _executeMultipleRequestcollection[j % requests].Requests.Add(CreateSetStateRequest(Product.EntityLogicalName, int.Parse(dr["id"].ToString()), Constant.AlternateKeyLogicalName, 0, 1));
                            }
                        }
                    }
                    if (_executeMultipleRequestcollection != null && _executeMultipleRequestcollection.Count > 0)
                    {
                        foreach (ExecuteMultipleRequest request in _executeMultipleRequestcollection)
                        {
                            ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(request);
                            foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                            {
                                if (e.Fault != null)
                                {
                                    //ErrorLog(string.Format(("{0} with NC DB ID {1} is failed for upsert with message {2}"), table, ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString(), e.Fault.Message));
                                    string n = (((SetStateRequest)(request.Requests[e.RequestIndex])).EntityMoniker.KeyAttributes["wrs_id"]).ToString();
                                    string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','Product','" + "Product ID : " + n + "'";
                                    WriteLogsToDB(errorLog);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private void CreatePriceListItem(Program p)
        {
            //Retrieve records from NC DB
            DataTable priceListItemTable = RetrieveRecordsFromDB(Constant.PriceListItemCreateStoreProcName);
            if (priceListItemTable != null && priceListItemTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "productpricelevelid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(priceListItemTable, PreparePriceListItemObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Constant.PriceListItem, ProductPriceLevel.EntityLogicalName, "percentage", primaryKeyLogicalName, false, true, false);
                    }
            }
        }

        private void CreateBasePriceListItem(string priceListItem, Program p, string productStoreProcName, string alternateKeyLogicalName)
        {
            //Retrieve records from NC DB
            DataTable priceListItemTable = RetrieveRecordsFromDB(productStoreProcName);
            try
            {
                string primaryKeyLogicalName = "productpricelevelid";
                alternateKeyLogicalName = "percentage";
                List<EntityCollection> _lisEntityCollection = GetBaseEntityCollection(priceListItemTable, PreparePriceListItemObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, priceListItem, ProductPriceLevel.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName, false, true, false);
                    }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private void UpdatePriceListItem(Program p)
        {
            //Retrieve records from NC DB
            DataTable priceListItemTable = RetrieveRecordsFromDB(Constant.PriceListItemUpdateStoreProcName);
            if (priceListItemTable != null && priceListItemTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "productpricelevelid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(priceListItemTable, PreparePriceListItemObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Constant.PriceListItem, ProductPriceLevel.EntityLogicalName, "percentage", primaryKeyLogicalName, false, false, true);
                    }
            }
        }

        private List<EntityCollection> GetBaseEntityCollection(DataTable table, Func<DataRow, bool, Entity> myMethod)
        {
            int totalCount = table.Rows.Count;
            if (totalCount == 0) return null;
            int requests = (totalCount / batchSize) + 1; //Maximum parallel requests
            List<EntityCollection> _lisEntityCollection = new List<EntityCollection>();
            for (int i = 0; i < requests; i++)
            {
                //Add (requests) set of EntityCollection in _lisEntityCollection
                _lisEntityCollection.Add(new EntityCollection());
            }

            //Add maximum of (batchCount) number of records in each _lisEntityCollection -> EntityCollection object
            for (int j = 0; j < totalCount; j++)
            {
                DataRow dr = table.Rows[j];
                Entity _record = myMethod(dr, true);
                if (_record != null)
                    _lisEntityCollection[j % requests].Entities.Add(_record);
            }
            return _lisEntityCollection;
        }

        private List<EntityCollection> GetEntityCollection(DataTable table, Func<DataRow, bool, Entity> myMethod)
        {
            int totalCount = table.Rows.Count;
            if (totalCount == 0) return null;
            int requests = (totalCount / batchSize) + 1; //Maximum parallel requests
            List<EntityCollection> _lisEntityCollection = new List<EntityCollection>();
            for (int i = 0; i < requests; i++)
            {
                //Add (requests) set of EntityCollection in _lisEntityCollection
                _lisEntityCollection.Add(new EntityCollection());
            }

            //Add maximum of (batchCount) number of records in each _lisEntityCollection -> EntityCollection object
            for (int j = 0; j < totalCount; j++)
            {
                DataRow dr = table.Rows[j];
                Entity _record = myMethod(dr, false);
                if (_record != null)
                    _lisEntityCollection[j % requests].Entities.Add(_record);
            }
            return _lisEntityCollection;
        }

        private List<EntityCollection> GetEntityCollection(DataTable table, Func<DataRow, Entity> myMethod)
        {
            int totalCount = table.Rows.Count;
            if (totalCount == 0)
                return null;
            int requests = (totalCount / batchSize) + 1; //Maximum parallel requests
            List<EntityCollection> _lisEntityCollection = new List<EntityCollection>();
            for (int i = 0; i < requests; i++)
            {
                //Add (requests) set of EntityCollection in _lisEntityCollection
                _lisEntityCollection.Add(new EntityCollection());
            }

            //Add maximum of (batchCount) number of records in each _lisEntityCollection -> EntityCollection object
            for (int j = 0; j < totalCount; j++)
            {
                try
                {
                    DataRow dr = table.Rows[j];
                    Entity _record = myMethod(dr);
                    if (_record != null)
                        _lisEntityCollection[j % requests].Entities.Add(_record);
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            return _lisEntityCollection;
        }

        private void UpsertDiscount(Program p)
        {
            DataTable DiscountTable = RetrieveRecordsFromDB(Constant.DiscountStoreProcName);
            int totalCount = DiscountTable.Rows.Count;
            if (totalCount > 0)
            {
                string primaryKeyLogicalName = "wrs_discountid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(DiscountTable, PrepareDiscountObject);
                if (_lisEntityCollection != null)
                {
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Constant.Discount, wrs_discount.EntityLogicalName, Constant.AlternateKeyLogicalName, primaryKeyLogicalName);
                    }
                }
            }
        }

        private void UpsertDiscountProducts(Program p)
        {
            //Retrieve records from NC DB
            DataTable DiscountProductsTable = RetrieveRecordsFromDB(Constant.DiscountProductStoreProcName);
            DataTable tempDiscountProductsTable = RetrieveRecordsFromDB(Constant.DiscountProductStoreProcName);
            QueryExpression exp = new QueryExpression()
            {
                EntityName = "wrs_discountproducts",
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression()
            };
            LinkEntity link1 = new LinkEntity("wrs_discountproducts", "product", "wrs_product", "productid", JoinOperator.LeftOuter);
            link1.EntityAlias = "PRODUCT";
            link1.Columns.AddColumn("wrs_id");
            LinkEntity link2 = new LinkEntity("wrs_discountproducts", "wrs_discount", "wrs_discount", "wrs_discountid", JoinOperator.LeftOuter);
            link2.EntityAlias = "DISCOUNT";
            link2.Columns.AddColumn("wrs_id");
            exp.LinkEntities.Add(link1);
            exp.LinkEntities.Add(link2);
            EntityCollection coll = CrmService.RetrieveMultiple(exp);
            List<EntityCollection> _lisEntityCollection = new List<EntityCollection>();
            int totalCount = DiscountProductsTable.Rows.Count;
            if (totalCount > 0)
            {
                if (coll != null && coll.Entities.Count > 0)
                {
                    foreach (DataRow dr in DiscountProductsTable.Rows)
                    {
                        var discountProdcut = coll.Entities.Where(e => e.GetAttributeValue<AliasedValue>("PRODUCT.wrs_id").Value.ToString() == dr["Product_Id"].ToString() && e.GetAttributeValue<AliasedValue>("DISCOUNT.wrs_id").Value.ToString() == dr["Discount_Id"].ToString());
                        if (discountProdcut != null && discountProdcut.Count() > 0)
                        {
                            coll.Entities.Remove((Entity)discountProdcut.FirstOrDefault());
                            var row = tempDiscountProductsTable.Select("Product_Id = '" + dr["Product_Id"].ToString() + "' AND Discount_Id = '" + dr["Discount_Id"].ToString() + "'");
                            if (row != null && row.Count() > 0)
                            {
                                tempDiscountProductsTable.Rows.Remove((DataRow)row.FirstOrDefault());
                            }
                        }
                    }
                    if (coll.Entities.Count > 0)
                        foreach (Entity prodcutDiscount in coll.Entities)
                        {
                            CrmService.Delete(prodcutDiscount.LogicalName, prodcutDiscount.Id);
                        }
                }
                if (tempDiscountProductsTable.Rows.Count > 0)
                {
                    _lisEntityCollection = GetEntityCollection(tempDiscountProductsTable, PrepareDiscountProductsObject);
                    string primaryKeyLogicalName = "wrs_discountproductsid";
                    if (_lisEntityCollection != null)
                        foreach (EntityCollection ec in _lisEntityCollection)
                        {
                            p.CrmExecuteMultiple(ec, p, Constant.DiscountProduct, wrs_discountproducts.EntityLogicalName, Constant.AlternateKeyLogicalName, primaryKeyLogicalName);
                        }
                }
            }
            else if (coll != null && coll.Entities.Count > 0)
            {
                foreach (Entity prodcutDiscount in coll.Entities)
                {
                    CrmService.Delete(prodcutDiscount.LogicalName, prodcutDiscount.Id);
                }
            }
        }

        private void UpsertProduct(string product, Program p, string storeProc, string alternateKeyLogicalName)
        {
            DataTable ProductTable = RetrieveRecordsFromDB(storeProc);
            try
            {
                string primaryKeyLogicalName = "productid";

                List<EntityCollection> _lisEntityCollection = GetEntityCollection(ProductTable, PrepareProductObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        if (ec.Entities.Count > 0)
                            p.CrmExecuteMultiple(ec, p, product, Product.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                    }

                List<EntityCollection> _lisEntityCollectionPriceListItem = GetBaseEntityCollection(ProductTable, PreparePriceListItemObject);
                if (_lisEntityCollectionPriceListItem != null)
                    foreach (EntityCollection ecPriceListItem in _lisEntityCollection)
                    {
                        if (ecPriceListItem.Entities.Count > 0)
                            p.CrmExecuteMultiple(ecPriceListItem, p, "PriceGroups", ProductPriceLevel.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName, false, true, false);
                    }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        private void UpsertProductAndProductFamilies(Program p)
        {
            DataTable ProductTable = RetrieveRecordsFromDB(Constant.ProductProductFamilyStoreProcName);
            try
            {
                if (ProductTable != null && ProductTable.Rows.Count > 0)
                {
                    string primaryKeyLogicalName = "productid";
                    List<EntityCollection> _listPFEntityCollection = GetEntityCollection(ProductTable, PrepareProductFamilyObject);
                    if (_listPFEntityCollection != null)
                        foreach (EntityCollection ec in _listPFEntityCollection)
                        {
                            if (ec.Entities.Count > 0)
                                p.CrmExecuteMultiple(ec, p, Constant.Product, Product.EntityLogicalName, Constant.AlternateKeyLogicalName, primaryKeyLogicalName);
                        }

                    List<EntityCollection> _listPEntityCollection = GetEntityCollection(ProductTable, PrepareProductObject);
                    if (_listPEntityCollection != null)
                        foreach (EntityCollection ec in _listPEntityCollection)
                        {
                            if (ec.Entities.Count > 0)
                                p.CrmExecuteMultiple(ec, p, Constant.Product, Product.EntityLogicalName, Constant.AlternateKeyLogicalName, primaryKeyLogicalName);
                        }

                    List<EntityCollection> _lisEntityCollectionPriceListItem = GetBaseEntityCollection(ProductTable, PreparePriceListItemObject);
                    if (_lisEntityCollectionPriceListItem != null)
                        foreach (EntityCollection ecPriceListItem in _lisEntityCollectionPriceListItem)
                        {
                            if (ecPriceListItem.Entities.Count > 0)
                                p.CrmExecuteMultiple(ecPriceListItem, p, "PriceGroups", ProductPriceLevel.EntityLogicalName, Constant.AlternateKeyLogicalName, primaryKeyLogicalName, false, true, false);
                        }
                    p.ProductsToActiveUnderRevision(p, ProductTable);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        private static EntityCollection RetrieveNNRecordsFromCRM(string Parent1EntityLogicalname, string Parent2EntityLogicalname, string RelationshipName, string[] Parent1Set, string[] Parent2Set)
        {
            QueryExpression exp = new QueryExpression(Parent1EntityLogicalname);
            exp.ColumnSet = new ColumnSet(Parent1Set);
            LinkEntity linkEntity1 = new LinkEntity(Parent1EntityLogicalname, RelationshipName, Parent1EntityLogicalname + "id", Parent1EntityLogicalname + "id", JoinOperator.Inner);
            LinkEntity linkEntity2 = new LinkEntity(RelationshipName, Parent2EntityLogicalname, Parent2EntityLogicalname + "id", Parent2EntityLogicalname + "id", JoinOperator.Inner);
            linkEntity2.Columns.AddColumns(Parent2Set);
            linkEntity1.LinkEntities.Add(linkEntity2);
            exp.LinkEntities.Add(linkEntity1);
            return CrmService.RetrieveMultiple(exp);
        }

        private void PublishProducts()
        {
            try
            {
                EntityCollection productFamilies = RetrieveProductFamilies();
                PublishProductFamilies(productFamilies);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private EntityCollection RetrieveProductFamilies()
        {
            QueryExpression retrieveProductFamilies = new QueryExpression(Product.EntityLogicalName);
            retrieveProductFamilies.ColumnSet.Columns.Add("productid");
            retrieveProductFamilies.Criteria.Conditions.Add(new ConditionExpression("productstructure", ConditionOperator.Equal, 2));

            return CrmService.RetrieveMultiple(retrieveProductFamilies);
        }

        private void PublishProductFamilies(EntityCollection productFamilies)
        {
            ExecuteMultipleRequest publishProductFamilyReq = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };
            foreach (Entity pf in productFamilies.Entities)
            {
                publishProductFamilyReq.Requests.Add(preparePublishProductFamilyRequest(pf));
            }

            ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(publishProductFamilyReq);

        }

        private PublishProductHierarchyRequest preparePublishProductFamilyRequest(Entity pf)
        {
            PublishProductHierarchyRequest Req = new PublishProductHierarchyRequest
            {
                Target = new EntityReference(Product.EntityLogicalName, pf.Id)
            };
            return Req;
        }

        public static void ErrorLog(string sErrMsg)
        {
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            string sErrorTime = sYear + sMonth + sDay;

            string sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";
            StreamWriter sw = new StreamWriter("C:/" + sErrorTime + ".txt", true);
            sw.WriteLine(sLogFormat + sErrMsg);
            sw.Flush();
            sw.Close();
        }

        public void UpdateSyncStatus(string entityName, string l)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var update_item = new SqlCommand("CrmUpdateAuditLogSyncStatus", conn))
            {
                update_item.CommandType = CommandType.StoredProcedure;
                update_item.Parameters.Add("@EntityId", SqlDbType.NVarChar).Value = l.ToString();
                update_item.Parameters.Add("@EntityName", SqlDbType.NVarChar).SqlValue = entityName;
                conn.Open();
                update_item.ExecuteNonQuery();
                conn.Close();
            }
        }

        public string RetrieveAlternateKey(Guid accountID, string entityLogicalName, string alternate, string primary)
        {
            QueryExpression accountbyID = new QueryExpression(entityLogicalName);
            accountbyID.Criteria.AddCondition(new ConditionExpression(primary, ConditionOperator.Equal, accountID));
            accountbyID.ColumnSet.Columns.Add(alternate);
            Entity e = CrmService.Retrieve(entityLogicalName, accountID, accountbyID.ColumnSet);
            if (e != null && e.Attributes[alternate] != null && entityLogicalName != TransactionCurrency.EntityLogicalName)
                return e.GetAttributeValue<int>(alternate).ToString();
            else if (e != null && e.Attributes[alternate] != null && entityLogicalName.Equals(TransactionCurrency.EntityLogicalName))
                return e.GetAttributeValue<string>(alternate);
            return "";
        }

        public static IOrganizationService serviceClient()
        {
            CrmServiceClient crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

        public DataTable RetrieveRecordsFromDB(string storeProc)
        {
            DataTable DBTable = new DataTable();
            try
            {
                string sqlConfig = ConfigurationManager.ConnectionStrings["SQL"].ConnectionString;
                using (var conn = new SqlConnection(sqlConfig))
                using (var command = new SqlCommand(storeProc, conn))
                using (var da = new SqlDataAdapter(command))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    da.Fill(DBTable);
                }
                return DBTable;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private EntityCollection RetrieveRecordsFromCRM(string relationshipName)
        {
            QueryExpression query = new QueryExpression(relationshipName);
            query.ColumnSet.AllColumns = true;
            return CrmService.RetrieveMultiple(query);
        }

        public Entity PrepareDiscountObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {

                Entity discountToBeCreated = new Entity(wrs_discount.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_discount discountToBeCreatedFromDB = discountToBeCreated.ToEntity<wrs_discount>();

                discountToBeCreatedFromDB.wrs_name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";
                discountToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                if (dr["DiscountTypeId"] != DBNull.Value && int.Parse(dr["DiscountTypeId"].ToString()) != 0)
                    discountToBeCreatedFromDB.wrs_discounttypeid = int.Parse(dr["DiscountTypeId"].ToString());

                discountToBeCreatedFromDB.wrs_usepercentage = dr["UsePercentage"] != DBNull.Value ? (bool)dr["UsePercentage"] : false;

                if (dr["DiscountPercentage"] != DBNull.Value && decimal.Parse(dr["DiscountPercentage"].ToString()) != 0)
                    discountToBeCreatedFromDB.wrs_discountpercentage = decimal.Parse(dr["DiscountPercentage"].ToString());
                if (dr["DiscountAmount"] != DBNull.Value && decimal.Parse(dr["DiscountAmount"].ToString()) != 0)
                    discountToBeCreatedFromDB.wrs_discountamount = decimal.Parse(dr["DiscountAmount"].ToString());
                if (dr["MaximumDiscountAmount"] != DBNull.Value && decimal.Parse(dr["MaximumDiscountAmount"].ToString()) != 0)
                    discountToBeCreatedFromDB.wrs_discountamount = decimal.Parse(dr["MaximumDiscountAmount"].ToString());


                if (dr["StartDateUtc"] != DBNull.Value)
                    discountToBeCreatedFromDB.wrs_startdate = (DateTime?)(dr["StartDateUtc"]);

                if (dr["EndDateUtc"] != DBNull.Value)
                    discountToBeCreatedFromDB.wrs_enddate = (DateTime?)(dr["EndDateUtc"]);

                discountToBeCreatedFromDB.wrs_requiredcouponcode = dr["RequiresCouponCode"] != DBNull.Value ? (bool)dr["RequiresCouponCode"] : false;
                discountToBeCreatedFromDB.wrs_couponcode = dr["CouponCode"] != DBNull.Value ? dr["CouponCode"].ToString() : "";

                discountToBeCreatedFromDB.wrs_iscumulative = dr["IsCumulative"] != DBNull.Value ? (bool)dr["IsCumulative"] : false;
                if (dr["DiscountLimitationId"] != DBNull.Value && int.Parse(dr["DiscountLimitationId"].ToString()) != 0)
                    discountToBeCreatedFromDB.wrs_discountlimitationid = int.Parse(dr["DiscountLimitationId"].ToString());
                if (dr["LimitationTimes"] != DBNull.Value && int.Parse(dr["LimitationTimes"].ToString()) != 0)
                    discountToBeCreatedFromDB.wrs_limitationtimes = int.Parse(dr["LimitationTimes"].ToString());
                if (dr["MaximumDiscountedQuantity"] != DBNull.Value && int.Parse(dr["MaximumDiscountedQuantity"].ToString()) != 0)
                    discountToBeCreatedFromDB.wrs_maximumdiscountedqty = int.Parse(dr["MaximumDiscountedQuantity"].ToString());

                discountToBeCreatedFromDB.wrs_appliedtosubcategories = dr["AppliedToSubCategories"] != DBNull.Value ? (bool)dr["AppliedToSubCategories"] : false;

                return discountToBeCreatedFromDB;
            }
            return null;
        }

        public Entity PrepareProductFamilyObject(DataRow dr)
        {

            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) != 0 && dr["ProductTypeId"] != DBNull.Value && int.Parse(dr["ProductTypeId"].ToString()) == 10)
            {
                Entity productToBeCreated = new Entity(Product.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                Product productToBeCreatedFromDB = productToBeCreated.ToEntity<Product>();
                productToBeCreatedFromDB.ProductNumber = dr["Id"].ToString();
                productToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                productToBeCreatedFromDB.PriceLevelId = new EntityReference("pricelevel", new Guid(ConfigurationManager.AppSettings["DefaultPriceListId"]));
                productToBeCreatedFromDB.DefaultUoMId = new EntityReference("uom", new Guid(ConfigurationManager.AppSettings["DefaultUoMId"]));
                productToBeCreatedFromDB.DefaultUoMScheduleId = new EntityReference("uomschedule", new Guid(ConfigurationManager.AppSettings["DefaultUoMScheduleId"]));
                productToBeCreatedFromDB.Name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";
                productToBeCreatedFromDB.wrs_shortdescription = dr["ShortDescription"] != DBNull.Value ? dr["ShortDescription"].ToString() : "";
                productToBeCreatedFromDB.Description = dr["FullDescription"] != DBNull.Value ? dr["FullDescription"].ToString() : "";
                productToBeCreatedFromDB.wrs_admincomments = dr["AdminComment"] != DBNull.Value ? dr["AdminComment"].ToString() : "";
                productToBeCreatedFromDB.VendorID = dr["VendorId"] != DBNull.Value ? dr["VendorId"].ToString() : "";
                productToBeCreatedFromDB.wrs_subjecttoacl = dr["SubjectToAcl"] != DBNull.Value ? (bool)dr["SubjectToAcl"] : false;
                productToBeCreatedFromDB.wrs_limitedtostore = dr["LimitedToStores"] != DBNull.Value ? (bool)dr["LimitedToStores"] : false;
                productToBeCreatedFromDB.wrs_sku = dr["Sku"] != DBNull.Value ? dr["Sku"].ToString() : "";
                productToBeCreatedFromDB.wrs_manufacturerpartnumber = dr["ManufacturerPartNumber"] != DBNull.Value ? dr["ManufacturerPartNumber"].ToString() : "";
                productToBeCreatedFromDB.wrs_sourcefrom = "NC";
                productToBeCreatedFromDB.wrs_isgiftcard = dr["IsGiftCard"] != DBNull.Value ? (bool)dr["IsGiftCard"] : false;

                if (dr["GiftCardTypeId"] != DBNull.Value && int.Parse(dr["GiftCardTypeId"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_giftcardtypeid = int.Parse(dr["GiftCardTypeId"].ToString());
                productToBeCreatedFromDB.wrs_isshipenabled = dr["IsShipEnabled"] != DBNull.Value ? (bool)dr["IsShipEnabled"] : false;
                productToBeCreatedFromDB.wrs_isfreeshipping = dr["IsFreeShipping"] != DBNull.Value ? (bool)dr["IsFreeShipping"] : false;
                if (dr["WarehouseId"] != DBNull.Value && int.Parse(dr["WarehouseId"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_warehouseid = int.Parse(dr["WarehouseId"].ToString());
                if (dr["StockQuantity"] != DBNull.Value && decimal.Parse(dr["StockQuantity"].ToString()) != 0)
                    productToBeCreatedFromDB.StockVolume = decimal.Parse(dr["StockQuantity"].ToString());
                if (dr["OrderMinimumQuantity"] != DBNull.Value && int.Parse(dr["OrderMinimumQuantity"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_orderminqty = int.Parse(dr["OrderMinimumQuantity"].ToString());
                if (dr["OrderMaximumQuantity"] != DBNull.Value && int.Parse(dr["OrderMaximumQuantity"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_ordermaxqty = int.Parse(dr["OrderMaximumQuantity"].ToString());
                productToBeCreatedFromDB.wrs_allowedquantities = dr["AllowedQuantities"] != DBNull.Value ? dr["AllowedQuantities"].ToString() : "";
                productToBeCreatedFromDB.wrs_hasdiscountapplied = dr["HasDiscountsApplied"] != DBNull.Value ? (bool)dr["HasDiscountsApplied"] : false;
                if (dr["Weight"] != DBNull.Value && decimal.Parse(dr["Weight"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_weight = decimal.Parse(dr["Weight"].ToString());
                if (dr["Width"] != DBNull.Value && decimal.Parse(dr["Width"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_width = decimal.Parse(dr["Width"].ToString());
                if (dr["Length"] != DBNull.Value && decimal.Parse(dr["Length"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_length = decimal.Parse(dr["Length"].ToString());
                if (dr["AvailableStartDateTimeUtc"] != DBNull.Value)
                    productToBeCreatedFromDB.wrs_availablestartdate = (DateTime?)(dr["AvailableStartDateTimeUtc"]);
                if (dr["AvailableEndDateTimeUtc"] != DBNull.Value)
                    productToBeCreatedFromDB.wrs_availableenddate = (DateTime?)(dr["AvailableEndDateTimeUtc"]);
                if (dr["SalesPeriodFrom"] != DBNull.Value)
                    productToBeCreatedFromDB.ValidFromDate = (DateTime?)(dr["SalesPeriodFrom"]);
                if (dr["SalesPeriodTo"] != DBNull.Value)
                    productToBeCreatedFromDB.ValidToDate = (DateTime?)(dr["SalesPeriodTo"]);
                productToBeCreatedFromDB.wrs_published = dr["Published"] != DBNull.Value ? (bool)dr["Published"] : false;
                if (dr["CreatedOnUtc"] != DBNull.Value)
                    productToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)(dr["CreatedOnUtc"]);

                productToBeCreatedFromDB.ProductStructure = new OptionSetValue(2);
                return productToBeCreatedFromDB;
            }
            return null;
        }

        public Entity PrepareProductObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) != 0 && dr["ProductTypeId"] != DBNull.Value && int.Parse(dr["ProductTypeId"].ToString()) == 5)
            {
                Entity productToBeCreated = new Entity(Product.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                Product productToBeCreatedFromDB = productToBeCreated.ToEntity<Product>();
                productToBeCreatedFromDB.ProductNumber = dr["Id"].ToString();
                //productToBeCreatedFromDB.PriceLevelId = new EntityReference(PriceLevel.EntityLogicalName, new Guid("5CAFC4E7-6E81-E711-80FE-C4346BAC4E40"));

                productToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                productToBeCreatedFromDB.QuantityDecimal = int.Parse(ConfigurationManager.AppSettings["DecimalsSupported"]);
                productToBeCreatedFromDB.DefaultUoMId = new EntityReference("uom", new Guid(ConfigurationManager.AppSettings["DefaultUoMId"]));
                productToBeCreatedFromDB.DefaultUoMScheduleId = new EntityReference("uomschedule", new Guid(ConfigurationManager.AppSettings["DefaultUoMScheduleId"]));
                productToBeCreatedFromDB.PriceLevelId = new EntityReference("pricelevel", new Guid(ConfigurationManager.AppSettings["DefaultPriceListId"]));
                productToBeCreatedFromDB.wrs_sourcefrom = "NC";
                if (dr["ParentGroupedProductId"] != DBNull.Value && int.Parse(dr["ParentGroupedProductId"].ToString()) != 0)
                    productToBeCreatedFromDB.ParentProductId = new EntityReference(Product.EntityLogicalName, "wrs_id", int.Parse(dr["ParentGroupedProductId"].ToString()));
                else
                    productToBeCreatedFromDB.ParentProductId = null;
                productToBeCreatedFromDB.Name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";
                productToBeCreatedFromDB.wrs_shortdescription = dr["ShortDescription"] != DBNull.Value ? dr["ShortDescription"].ToString() : "";
                productToBeCreatedFromDB.Description = dr["FullDescription"] != DBNull.Value ? dr["FullDescription"].ToString() : "";
                productToBeCreatedFromDB.wrs_admincomments = dr["AdminComment"] != DBNull.Value ? dr["AdminComment"].ToString() : "";
                productToBeCreatedFromDB.VendorID = dr["VendorId"] != DBNull.Value ? dr["VendorId"].ToString() : "";
                productToBeCreatedFromDB.wrs_subjecttoacl = dr["SubjectToAcl"] != DBNull.Value ? (bool)dr["SubjectToAcl"] : false;
                productToBeCreatedFromDB.wrs_limitedtostore = dr["LimitedToStores"] != DBNull.Value ? (bool)dr["LimitedToStores"] : false;
                productToBeCreatedFromDB.wrs_sku = dr["Sku"] != DBNull.Value ? dr["Sku"].ToString() : "";
                productToBeCreatedFromDB.wrs_manufacturerpartnumber = dr["ManufacturerPartNumber"] != DBNull.Value ? dr["ManufacturerPartNumber"].ToString() : "";
                productToBeCreatedFromDB.wrs_isgiftcard = dr["IsGiftCard"] != DBNull.Value ? (bool)dr["IsGiftCard"] : false;
                if (dr["GiftCardTypeId"] != DBNull.Value && int.Parse(dr["GiftCardTypeId"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_giftcardtypeid = int.Parse(dr["GiftCardTypeId"].ToString());

                productToBeCreatedFromDB.wrs_isshipenabled = dr["IsShipEnabled"] != DBNull.Value ? (bool)dr["IsShipEnabled"] : false;
                productToBeCreatedFromDB.wrs_isfreeshipping = dr["IsFreeShipping"] != DBNull.Value ? (bool)dr["IsFreeShipping"] : false;

                if (dr["WarehouseId"] != DBNull.Value && int.Parse(dr["WarehouseId"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_warehouseid = int.Parse(dr["WarehouseId"].ToString());
                else
                    productToBeCreatedFromDB.wrs_warehouseid = null;

                if (dr["StockQuantity"] != DBNull.Value && decimal.Parse(dr["StockQuantity"].ToString()) != 0)
                    productToBeCreatedFromDB.StockVolume = decimal.Parse(dr["StockQuantity"].ToString());
                else
                    productToBeCreatedFromDB.StockVolume = null;

                if (dr["OrderMinimumQuantity"] != DBNull.Value && int.Parse(dr["OrderMinimumQuantity"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_orderminqty = int.Parse(dr["OrderMinimumQuantity"].ToString());
                else
                    productToBeCreatedFromDB.wrs_orderminqty = null;

                if (dr["OrderMaximumQuantity"] != DBNull.Value && int.Parse(dr["OrderMaximumQuantity"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_ordermaxqty = int.Parse(dr["OrderMaximumQuantity"].ToString());
                else
                    productToBeCreatedFromDB.wrs_ordermaxqty = null;

                productToBeCreatedFromDB.wrs_allowedquantities = dr["AllowedQuantities"] != DBNull.Value ? dr["AllowedQuantities"].ToString() : "";

                if (dr["Price"] != DBNull.Value && decimal.Parse(dr["Price"].ToString()) > 0)
                    productToBeCreatedFromDB.Price = new Money(decimal.Parse(dr["Price"].ToString()));
                else
                    productToBeCreatedFromDB.Price = null;

                if (dr["OldPrice"] != DBNull.Value && decimal.Parse(dr["OldPrice"].ToString()) > 0)
                    productToBeCreatedFromDB.wrs_oldprice = new Money(decimal.Parse(dr["OldPrice"].ToString()));
                else
                    productToBeCreatedFromDB.wrs_oldprice = null;

                if (dr["ProductCost"] != DBNull.Value && decimal.Parse(dr["ProductCost"].ToString()) > 0)
                    productToBeCreatedFromDB.CurrentCost = new Money(decimal.Parse(dr["ProductCost"].ToString()));
                else
                    productToBeCreatedFromDB.CurrentCost = null;

                productToBeCreatedFromDB.wrs_hasdiscountapplied = dr["HasDiscountsApplied"] != DBNull.Value ? (bool)dr["HasDiscountsApplied"] : false;

                if (dr["Weight"] != DBNull.Value && decimal.Parse(dr["Weight"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_weight = decimal.Parse(dr["Weight"].ToString());
                else
                    productToBeCreatedFromDB.wrs_weight = null;

                if (dr["Width"] != DBNull.Value && decimal.Parse(dr["Width"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_width = decimal.Parse(dr["Width"].ToString());
                else
                    productToBeCreatedFromDB.wrs_width = null;

                if (dr["Length"] != DBNull.Value && decimal.Parse(dr["Length"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_length = decimal.Parse(dr["Length"].ToString());
                else
                    productToBeCreatedFromDB.wrs_length = null;

                if (dr["AvailableStartDateTimeUtc"] != DBNull.Value)
                    productToBeCreatedFromDB.wrs_availablestartdate = (DateTime?)(dr["AvailableStartDateTimeUtc"]);
                else
                    productToBeCreatedFromDB.wrs_availablestartdate = null;

                if (dr["AvailableEndDateTimeUtc"] != DBNull.Value)
                    productToBeCreatedFromDB.wrs_availableenddate = (DateTime?)(dr["AvailableEndDateTimeUtc"]);
                else
                    productToBeCreatedFromDB.wrs_availableenddate = null;

                if (dr["SalesPeriodFrom"] != DBNull.Value)
                    productToBeCreatedFromDB.ValidFromDate = (DateTime?)(dr["SalesPeriodFrom"]);
                else
                    productToBeCreatedFromDB.ValidFromDate = null;

                if (dr["SalesPeriodTo"] != DBNull.Value)
                    productToBeCreatedFromDB.ValidToDate = (DateTime?)(dr["SalesPeriodTo"]);
                else
                    productToBeCreatedFromDB.ValidToDate = null;

                if (dr["DisplayOrder"] != DBNull.Value && int.Parse(dr["DisplayOrder"].ToString()) != 0)
                    productToBeCreatedFromDB.wrs_displayorder = int.Parse(dr["DisplayOrder"].ToString());
                else
                    productToBeCreatedFromDB.wrs_displayorder = null;

                productToBeCreatedFromDB.wrs_published = dr["Published"] != DBNull.Value ? (bool)dr["Published"] : false;

                if (dr["CreatedOnUtc"] != DBNull.Value)
                    productToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)(dr["CreatedOnUtc"]);

                return productToBeCreatedFromDB;
            }
            return null;
        }

        private Entity PreparePriceListItemObject(DataRow dr, bool isBasePriceListItem = false)
        {
            if (dr["Id"] != DBNull.Value && decimal.Parse(dr["Id"].ToString()) > 0 &&
                dr["ProductTypeId"] != DBNull.Value && int.Parse(dr["ProductTypeId"].ToString()) == 5)
            {

                //Entity priceLI = new Entity(ProductPriceLevel.EntityLogicalName);
                ProductPriceLevel priceListItem = new ProductPriceLevel();
                //priceListItem.Percentage = decimal.Parse(dr["Id"].ToString());
                if (isBasePriceListItem && dr["RecordState"] != DBNull.Value && dr["RecordState"].ToString() == "Added")
                {
                    if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
                        priceListItem.ProductId = new EntityReference(Product.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                    priceListItem.PriceLevelId = new EntityReference(PriceLevel.EntityLogicalName, new Guid(ConfigurationManager.AppSettings["DefaultPriceListId"]));
                    if (dr["Price"] != DBNull.Value && decimal.Parse(dr["Price"].ToString()) >= 0)
                        priceListItem.Amount = new Money(decimal.Parse(dr["Price"].ToString()));
                    priceListItem.ProductPriceLevelId = new Guid(GetDeterministicGuid("Base" + dr["Id"].ToString()).ToString().ToUpper());
                    priceListItem.UoMId = new EntityReference("uom", new Guid(ConfigurationManager.AppSettings["DefaultUoMId"]));
                    priceListItem.UoMScheduleId = new EntityReference("uomschedule", new Guid(ConfigurationManager.AppSettings["DefaultUoMScheduleId"]));

                    return priceListItem;
                }
                else if (!isBasePriceListItem)
                {
                    PriceGroupLists.Add(new PriceGroup()
                    {
                        Id = int.Parse(dr["Id"].ToString()),
                        ProductId = int.Parse(dr["ProductId"].ToString()),
                        PriceGroupId = int.Parse(dr["PriceGroupId"].ToString())
                    });
                    priceListItem.ProductPriceLevelId = new Guid(GetDeterministicGuid(dr["Id"].ToString()).ToString().ToUpper());

                    if (dr["ProductId"] != DBNull.Value && int.Parse(dr["ProductId"].ToString()) > 0)
                        priceListItem.ProductId = new EntityReference(Product.EntityLogicalName, "wrs_id", int.Parse(dr["ProductId"].ToString()));
                    if (dr["PriceGroupId"] != DBNull.Value && int.Parse(dr["PriceGroupId"].ToString()) > 0)
                    {
                        priceListItem.PriceLevelId = new EntityReference(PriceLevel.EntityLogicalName, "wrs_id", int.Parse(dr["PriceGroupId"].ToString()));
                    }
                    if (dr["Price"] != DBNull.Value && decimal.Parse(dr["Price"].ToString()) >= 0)
                        priceListItem.Amount = new Money(decimal.Parse(dr["Price"].ToString()));
                    priceListItem.UoMId = new EntityReference("uom", new Guid(ConfigurationManager.AppSettings["DefaultUoMId"]));
                    priceListItem.UoMScheduleId = new EntityReference("uomschedule", new Guid(ConfigurationManager.AppSettings["DefaultUoMScheduleId"]));

                    return priceListItem;
                }

            }
            return null;
        }

        private AssociateRequest CreateAssociateRequest(EntityReference target, EntityReferenceCollection relatedEntities, Relationship relationship)
        {

            AssociateRequest ar = new AssociateRequest();
            ar.Target = target;
            ar.RelatedEntities = relatedEntities;
            ar.Relationship = relationship;
            return ar;
        }

        private DisassociateRequest CreateDisassociateRequest(EntityReference target, EntityReferenceCollection relatedEntities, Relationship relationship)
        {

            DisassociateRequest dr = new DisassociateRequest();
            dr.Target = target;
            dr.RelatedEntities = relatedEntities;
            dr.Relationship = relationship;
            return dr;
        }

        private Guid GetDeterministicGuid(string input)

        {

            //use MD5 hash to get a 16-byte hash of the string: 

            MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();

            byte[] inputBytes = Encoding.Default.GetBytes(input);

            byte[] hashBytes = provider.ComputeHash(inputBytes);

            //generate a guid from the hash: 

            Guid hashGuid = new Guid(hashBytes);

            return hashGuid;

        }

        private Entity PrepareDiscountProductsObject(DataRow dr)
        {

            KeyAttributeCollection keys = new KeyAttributeCollection();
            if (dr["Discount_Id"] != DBNull.Value && int.Parse(dr["Discount_Id"].ToString()) > 0)
                keys.Add("new_ncdiscountid", int.Parse(dr["Discount_Id"].ToString()));
            if (dr["Product_Id"] != DBNull.Value && int.Parse(dr["Product_Id"].ToString()) > 0)
                keys.Add("wrs_ncproductid", int.Parse(dr["Product_Id"].ToString()));

            Entity discountProduct = new Entity(wrs_discountproducts.EntityLogicalName, keys);
            wrs_discountproducts discountProductObj = discountProduct.ToEntity<wrs_discountproducts>();
            discountProductObj.new_NCDiscountID = int.Parse(dr["Discount_Id"].ToString());
            discountProductObj.wrs_NCProductID = int.Parse(dr["Product_Id"].ToString());
            discountProductObj.wrs_Discount = new EntityReference(wrs_discount.EntityLogicalName, "wrs_id", int.Parse((dr["Discount_Id"]).ToString()));
            discountProductObj.wrs_Product = new EntityReference(Product.EntityLogicalName, "wrs_id", int.Parse((dr["Product_Id"]).ToString()));

            return discountProduct;

        }

        public UpsertRequest CreateUpsertRequest(Entity target)
        {
            return new UpsertRequest()
            {
                Target = target
            };
        }

        public void CrmExecuteMultiple(EntityCollection ecoll, Program p, string table, string entityLogicalName, string alternate, string primary, bool isOrderNote = false, bool isPriceListItemCreate = false, bool isPriceListItemUpdate = false, bool isOrder = false)
        {
            string mainSuccessLog = string.Empty;

            try
            {
                ExecuteMultipleRequest multipleReq = new ExecuteMultipleRequest()
                {
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };
                //ErrorLog(string.Format("Starting Upsert of {0} {1} records", ecoll.Entities.Count, table));


                foreach (Entity e in ecoll.Entities)
                {
                    if (isOrderNote || isPriceListItemCreate)
                        multipleReq.Requests.Add(CreateCreateRequest(e));
                    else if (isPriceListItemUpdate)
                        multipleReq.Requests.Add(CreateUpdateRequest(e));
                    else
                        multipleReq.Requests.Add(CreateUpsertRequest(e));
                }
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleReq);
                List<string> existingRecords = new List<string>();

                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    if (e.Fault == null)
                    {
                        //string alternateKey = "";
                        object t = null;
                        if (isPriceListItemCreate)
                            ((EntityReference)((CreateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes["objectid"]).KeyAttributes.TryGetValue("wrs_id", out t);
                        else
                            t = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();

                        if (t != null)
                        {
                            //ErrorLog(string.Format("{0} with Guid {1} is created for NC DB ID {2}", table, ID, alternateKey));
                            if (!existingRecords.Contains(t.ToString()))
                                existingRecords.Add(t.ToString());

                            if (mainSuccessLog != string.Empty)
                            {
                                mainSuccessLog = mainSuccessLog + ",";
                            }

                            mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + t.ToString() + "')";
                        }
                    }

                    else if (e.Fault != null)
                    {
                        object t1 = null;
                        object t2 = null;
                        //ErrorLog(string.Format(("{0} with NC DB ID {1} is failed for upsert with message {2}"), table, ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString(), e.Fault.Message));
                        if (isPriceListItemCreate)
                        {
                            //((EntityReference)((CreateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes["objectId"]).KeyAttributes.TryGetValue("wrs_id", out t1);
                            ((EntityReference)((CreateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes["productid"]).KeyAttributes.TryGetValue("wrs_id", out t1);
                            ((EntityReference)((CreateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes["pricelevelid"]).KeyAttributes.TryGetValue("wrs_id", out t2);
                            NCId = t1.ToString();
                            PriceGroupLists.Remove(PriceGroupLists.Where(g => g.PriceGroupId == int.Parse(t2.ToString()) && g.ProductId == int.Parse(t1.ToString())).FirstOrDefault());
                        }
                        else
                            NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();

                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
                    }
                }
                if (!isOrder)
                {
                    if (table == Constant.PriceListItem)
                    {
                        foreach (PriceGroup pg in PriceGroupLists)
                        {
                            p.UpdateSyncStatus(Constant.PriceListItem, pg.Id.ToString());
                        }
                    }
                    else
                        foreach (string l in existingRecords)
                        {
                            if (isOrderNote)
                                p.UpdateSyncStatus("Order", l);
                            else
                                p.UpdateSyncStatus(table, l);
                            //ErrorLog(string.Format("Sync Status updated in NC DB for {0} with ID - {1}", table, l));
                        }
                }

                WriteMigrateStatusLogs(mainSuccessLog);

            }
            catch (Exception e)
            {
                string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                WriteLogsToDB(errorLog);
            }
        }

        private OrganizationRequest CreateCreateRequest(Entity e)
        {
            CreateRequest cr = new CreateRequest();
            cr.Target = e;
            return cr;
        }

        private OrganizationRequest CreateUpdateRequest(Entity e)
        {
            UpdateRequest cr = new UpdateRequest();
            cr.Target = e;
            return cr;
        }

        public SetStateRequest CreateSetStateRequest(string entityName, int alternateKey, string alternateKeyName, int stateCode, int statusCode)
        {
            EntityReference target = new EntityReference(entityName, alternateKeyName, alternateKey);
            SetStateRequest request = new SetStateRequest();
            request.EntityMoniker = target;
            request.State = new OptionSetValue(stateCode);
            request.Status = new OptionSetValue(statusCode);
            return request;
        }

        public int RetreveAlternateKey(Guid accountID, string entityLogicalName, string alternate, string primary)
        {
            QueryExpression accountbyID = new QueryExpression(entityLogicalName);
            accountbyID.Criteria.AddCondition(new ConditionExpression(primary, ConditionOperator.Equal, accountID));
            if (entityLogicalName.Equals(TransactionCurrency.EntityLogicalName))
                accountbyID.ColumnSet.Columns.Add(alternate);
            Entity e = CrmService.Retrieve(entityLogicalName, accountID, accountbyID.ColumnSet);
            if (e != null && e.Attributes[alternate] != null)
                return e.GetAttributeValue<int>(alternate);
            return 0;
        }

        private static void WriteLogsToDB(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = string.Empty;
                sql = "INSERT INTO [dbo].[CrmImportErrorLog] ([CreatedOnUtc],[Message],[StackTrace],[Table],[EntityId]) " +
                       "VALUES " + insertQuery + ")";

                SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQLErrorLog"].ToString());
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }

        private static void WriteMigrateStatusLogs(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = string.Empty;
                sql = "INSERT INTO [dbo].[CrmImportStatusLog] ([CreatedOnUtc],[Message],[Table],[EntityId]) " +
                       "VALUES " + insertQuery;

                SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQLErrorLog"].ToString());
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }
    }

    public class Constant
    {
        public const string OneStoreId = "1";
        public const string AlternateKeyLogicalName = "wrs_id";
        public const string ProductsToDraftStoreProcName = "CrmGetProductsToDraft";
        public const string ProductStoreMappingStoreProcName = "CrmGetProductStoreMapping";
        public const string DiscountProductStoreProcName = "CrmGetDiscountProducts";
        public const string ProductProductFamilyStoreProcName = "CrmGetProductProductFamilies";
        public const string DiscountStoreProcName = "CrmGetDiscounts";
        public const string PriceListItemCreateStoreProcName = "CrmGetPriceListItemsCreate";
        public const string PriceListItemUpdateStoreProcName = "CrmGetPriceListItemsModified";
        public const string Discount = "Discount";
        public const string Product = "product";
        public const string PriceListItem = "PriceGroups";
        public const string DiscountProduct = "Discount_AppliedToProducts";
        public const string Store = "wrs_store";

        public const string ProductStoreRelationship = "wrs_product_wrs_store";
    }

    public class PriceGroup
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int PriceGroupId { get; set; }
    }
}
