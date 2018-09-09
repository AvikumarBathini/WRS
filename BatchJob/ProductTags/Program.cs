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
using System.Data.SqlClient;
using System.Configuration;

namespace ProductTags
{
    class Program
    {
        public static IOrganizationService CrmService = serviceClient();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        private string NCId;
        static void Main(string[] args)
        {
            Program p = new Program();
            p.UpsertProductTags(p);
            p.UpdateProductTagProductMapping(p);
        }

        private void UpdateProductTagProductMapping(Program p)
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

            DataTable ProductTagsProductsMappingTable = RetrieveRecordsFromDB(Constant.ProductTagsProductStoreProcName);
            EntityCollection ProductTagsProductsMappingCRM = RetrieveNNRecordsFromCRM(Constant.Product, Constant.ProductTag, Constant.ProductTagProductRelationship, new string[] { "wrs_id" }, new string[] { "wrs_id" });
            if (ProductTagsProductsMappingTable != null && ProductTagsProductsMappingTable.Rows.Count > 0)
            {
                foreach (DataRow dr in ProductTagsProductsMappingTable.Rows)
                {
                    var productId = dr["Product_Id"];
                    if (productId != DBNull.Value && int.Parse(productId.ToString()) > 0)
                    {
                        var MappingColl = ProductTagsProductsMappingCRM.Entities.Where(e => e.GetAttributeValue<int>("wrs_id") == int.Parse(productId.ToString()));
                        List<int> productTags = new List<int>();
                        if (MappingColl != null && MappingColl.Count() > 0)
                        {
                            foreach (var id in dr["ProductTag_Ids"].ToString().Split(','))
                            {
                                var coll = MappingColl.Where(e => int.Parse(e.FormattedValues["wrs_producttag2.wrs_id"]) == int.Parse(id));
                                if (coll != null && coll.Count() > 0 && (Entity)coll.FirstOrDefault() != null)
                                    ProductTagsProductsMappingCRM.Entities.Remove((Entity)coll.FirstOrDefault());
                                else
                                    multipleAssociateReq.Requests.Add(PrepareProductTagProductAssociateRequest(int.Parse(productId.ToString()), int.Parse(id)));
                            }
                        }
                        else
                        {
                            foreach (var id in dr["ProductTag_Ids"].ToString().Split(','))
                            {
                                multipleAssociateReq.Requests.Add(PrepareProductTagProductAssociateRequest(int.Parse(productId.ToString()), int.Parse(id)));
                            }
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
                            string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + "[Product_ProductTag_Mapping]" + "','" + "Product_Id " + NCId + "'";
                            WriteLogsToDB(errorLog);
                        }
                    }
                }
                if (ProductTagsProductsMappingCRM.Entities.Count > 0)
                {
                    foreach (Entity e in ProductTagsProductsMappingCRM.Entities)
                    {
                        multipleDisassociateReq.Requests.Add(PrepareProductTagProductDisassociateRequest(e, Constant.ProductTagProductRelationship));
                    }
                    ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleDisassociateReq);
                    List<string> existingRecords = new List<string>();
                    foreach (var e in emrResponse.Responses)
                    {
                        if (e.Fault != null)
                        {
                            NCId = (((DisassociateRequest)(multipleDisassociateReq.Requests[e.RequestIndex]))).Target.KeyAttributes["productid"].ToString();
                            string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + "[Product_ProductTag_Mapping]" + "','" + "Product_Id: " + NCId + "'";
                            WriteLogsToDB(errorLog);
                        }
                    }
                }
            }
        }

        private void UpsertProductTags(Program p)
        {
            DataTable ProductTagTable = RetrieveRecordsFromDB(Constant.ProductTagStoreProcName);
            int totalCount = ProductTagTable.Rows.Count;
            if (totalCount > 0)
            {
                string primaryKeyLogicalName = "wrs_discountid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(ProductTagTable, PrepareProductTagObject);
                if (_lisEntityCollection != null)
                {
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, "ProductTag", wrs_discount.EntityLogicalName, Constant.AlternateKeyLogicalName, primaryKeyLogicalName);
                    }
                }
            }
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
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var command = new SqlCommand(storeProc, conn))
            using (var da = new SqlDataAdapter(command))
            {
                try
                {
                    command.CommandType = CommandType.StoredProcedure;
                    da.Fill(DBTable);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
            return DBTable;
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

        private Entity PrepareProductTagObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity productTag = new Entity(wrs_producttag.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_producttag productTagObj = productTag.ToEntity<wrs_producttag>();
                productTagObj.wrs_id = int.Parse(dr["Id"].ToString());
                productTagObj.wrs_name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";

                return productTagObj;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="productTagID"></param>
        /// <returns></returns>
        private AssociateRequest PrepareProductTagProductAssociateRequest(int productID, int productTagID)
        {
            EntityReference Target = new EntityReference(Product.EntityLogicalName, "wrs_id", productID);
            EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
            relatedEntities.Add(new EntityReference(wrs_producttag.EntityLogicalName, "wrs_id", productTagID));
            Relationship relationship = new Relationship(Constant.ProductTagProductRelationship);
            return CreateAssociateRequest(Target, relatedEntities, relationship);
        }

        public DisassociateRequest PrepareProductTagProductDisassociateRequest(Entity e, string relation)
        {
            EntityReference target = new EntityReference(Product.EntityLogicalName, (Guid)e.Attributes["productid"]);
            EntityReferenceCollection relatedEntities = new EntityReferenceCollection();
            relatedEntities.Add(new EntityReference(wrs_producttag.EntityLogicalName, "wrs_id", int.Parse(e.FormattedValues["wrs_producttag2.wrs_id"])));
            // Create an object that defines the relationship 
            Relationship relationship = new Relationship(relation);

            return CreateDisassociateRequest(target, relatedEntities, relationship);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="relatedEntities"></param>
        /// <param name="relationship"></param>
        /// <returns></returns>
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
                        string alternateKey = "";
                        object t = null;
                        if (isOrderNote)
                            ((EntityReference)((CreateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes["objectid"]).KeyAttributes.TryGetValue("wrs_id", out t);
                        else
                            alternateKey = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();

                        if (alternateKey != null)
                        {
                            //ErrorLog(string.Format("{0} with Guid {1} is created for NC DB ID {2}", table, ID, alternateKey));
                            existingRecords.Add(alternateKey.ToString());

                            if (mainSuccessLog != string.Empty)
                            {
                                mainSuccessLog = mainSuccessLog + ",";
                            }

                            mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + alternateKey + "')";
                        }
                    }

                    else if (e.Fault != null)
                    {
                        object t1 = null;
                        //ErrorLog(string.Format(("{0} with NC DB ID {1} is failed for upsert with message {2}"), table, ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString(), e.Fault.Message));
                        if (isOrderNote)
                        {
                            //((EntityReference)((CreateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes["objectId"]).KeyAttributes.TryGetValue("wrs_id", out t1);
                            ((EntityReference)((CreateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes["objectid"]).KeyAttributes.TryGetValue("wrs_id", out t1);
                            NCId = t1.ToString();
                        }
                        else
                            NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();

                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
                    }
                }
                if (!isOrder)
                {
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

        private List<EntityCollection> GetEntityCollection(DataTable table, Func<DataRow, Entity> myMethod)
        {
            int totalCount = table.Rows.Count;
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

    }

    public class Constant
    {
        public const string AlternateKeyLogicalName = "wrs_id";
        public const string ProductTagStoreProcName = "CrmGetProductTags";
        public const string ProductTagsProductStoreProcName = "CrmGetProductProductTagMappings";
        public const string ProductTag = "wrs_producttag";
        public const string Product = "product";
        public const string ProductTagProductRelationship = "wrs_wrs_producttag_product";
    }
}
