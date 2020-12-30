using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WRS.OneTimeLoad.AuditLog
{
    class Program
    {
        public static string fileName = string.Empty;
        public static string[] AuditEntites = new string[] {"wrs_membership"};
        public static string[] AuditEntites1 = new string[] { "mailbox", "wrs_recommendation", "wrs_staff", "wrs_blackoutcalendar",
            "wrs_blackoutcalendarproduct", "organization", "publisher", "contact", "salesorderdetail", "wrs_store", "invoicedetail",
            "wrs_category", "wrs_configuration","productpricelevel", "wrs_servicerecoveryproduct","wrs_resourcemanagement",
            "businessunit","transactioncurrency","wrs_subscription","wrs_stateregion","wrs_discountusagehistory","email",
            "wrs_discount","systemuser","wrs_membershipbooking","pricelevel","orderclose","wrs_producttag","wrs_caselocation",
        "wrs_blackoutcalendardetail","wrs_ordernote","wrs_park","wrs_freebie","wrs_discountproducts","queue",
            "wrs_pricecalendar","salesorder","team",
        "wrs_giftvoucher","wrs_refundrequest","incident","wrs_paymenttransaction","wrs_paymentgateway","wrs_country","product"};
        //,"wrs_membership"

        public static IOrganizationService _serviceProxy = serviceClient();
        public static List<EntityMetadata> entitiesMetaData = new List<EntityMetadata>();
        public static List<string> auditIds = new List<string>();
        static void Main(string[] args)
        {
           // GetAuditIds();
            //RetrieveAuditPartitionListResponse partitionRequest = (RetrieveAuditPartitionListResponse)_serviceProxy.Execute(new RetrieveAuditPartitionListRequest());
            //AuditPartitionDetailCollection partitions = partitionRequest.AuditPartitionDetailCollection;
            DateTime? startDate = GetLastEntryDate();
            int queryCount = 500;
            int pageNumber = 1;
            QueryExpression exp = new QueryExpression()
            {
                EntityName = "audit",
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression()
            };

            while (true)
            {
                if (startDate != null)
                    exp.Criteria.AddCondition("createdon", ConditionOperator.GreaterThan, startDate);
                OrderExpression orderExp = new OrderExpression("createdon", OrderType.Ascending);
                if (!exp.Orders.Contains(orderExp))
                    exp.Orders.Add(orderExp);
                exp.PageInfo = new PagingInfo();
                exp.PageInfo.Count = queryCount;
                exp.PageInfo.PageNumber = pageNumber;
                exp.PageInfo.PagingCookie = null;
                EntityCollection returnCollection = null;
                while (true)
                {
                    try
                    {
                        returnCollection = _serviceProxy.RetrieveMultiple(exp);
                    }
                    catch (Exception)
                    {
                        _serviceProxy = serviceClient();
                        returnCollection = _serviceProxy.RetrieveMultiple(exp);
                        continue;
                    }
                    ExportEntity(returnCollection);
                    if (returnCollection.MoreRecords)
                    {
                        exp.PageInfo.PageNumber++;
                        exp.PageInfo.PagingCookie = returnCollection.PagingCookie;
                    }
                    else
                    {
                        // If no more records in the result nodes, exit the loop.
                        break;
                    }
                }
            }
        }

        public static void GetAuditIds()
        {
            string query = "SELECT [AuditId] FROM [OneStore_CRMLog].[dbo].[WRS_CRMAuditLog_UAT] ";

            DataTable DBTable = new DataTable();
            try
            {
                string sqlConfig = ConfigurationManager.ConnectionStrings["SQL_CONNECTION_STRING"].ConnectionString;
                using (var conn = new SqlConnection(sqlConfig))
                using (var command = new SqlCommand(query, conn))
                using (var da = new SqlDataAdapter(command))
                {
                    command.CommandType = CommandType.Text;
                    da.Fill(DBTable);
                }
                if (DBTable != null && DBTable.Rows.Count > 0)
                {
                    IEnumerable<DataRow> AuditRows = DBTable.Rows.Cast<DataRow>();
                    auditIds.AddRange(AuditRows.Select(x => x.ItemArray[0].ToString()).ToList());
                }
            }

            catch (Exception ex)
            {
              
            }
        }
        private static DateTime? GetLastEntryDate()
        {
            string query = "SELECT TOP 1 [ModifiedOn] FROM [dbo].[WRS_CRMAuditLog] ORDER BY [ModifiedOn] DESC";
            DataTable DBTable = new DataTable();
            try
            {
                string sqlConfig = ConfigurationManager.ConnectionStrings["SQL_CONNECTION_STRING"].ConnectionString;
                using (var conn = new SqlConnection(sqlConfig))
                using (var command = new SqlCommand(query, conn))
                using (var da = new SqlDataAdapter(command))
                {
                    command.CommandType = CommandType.Text;
                    da.Fill(DBTable);
                }
                if (DBTable != null && DBTable.Rows.Count > 0)
                    return (DateTime)DBTable.Rows[0]["ModifiedOn"];
            }

            catch (Exception)
            {
                throw;
            }
            return null;
        }

        /// <summary>
        /// Exports the entity.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="fetchXml">The fetch XML query.</param>
        /// <param name="PrimaryNameAttribute">The primary name attribute.</param>
        /// <param name="columns">The columns.</param>
        /// <param name="DisplayedColumns">The displayed columns.</param>
        /// <param name="sae">The sae.</param>
        /// <returns>The number of exported records</returns>
        public static void ExportEntity(EntityCollection returnCollection)
        {
            //SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL_CONNECTION_STRING"].ToString());
            //SqlCommand command = new SqlCommand();
            //cnn.Open();
            DateTime ModifiedOn = DateTime.Now;
            int count = 0;
            using (SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL_CONNECTION_STRING"].ToString()))
            {
                cnn.Open();
                foreach (Entity e in returnCollection.Entities)
                #region For Each audit entity
                {
                    try
                    {
                        EntityMetadata entityM = null;
                        string logicalName = e.GetAttributeValue<EntityReference>("objectid").LogicalName;

                        Guid auditId = e.Id;
                        string Entity = string.Empty;
                        string Action = string.Empty;
                        string Operation = string.Empty;
                        string Attribute = string.Empty;
                        string OldValue = string.Empty;
                        string NewValue = string.Empty;
                        string ModifiedBy = string.Empty;
                        //if (auditIds.Contains(auditId.ToString()))
                        //    continue;

                        if (AuditEntites.Contains(logicalName))
                        {
                            #region Main Logic
                            entityM = entitiesMetaData.Where(f => f.LogicalName == logicalName).FirstOrDefault();
                            if (entityM == null)
                            {
                                RetrieveEntityRequest req = new RetrieveEntityRequest()
                                {
                                    EntityFilters = EntityFilters.Attributes,
                                    LogicalName = logicalName,
                                    RetrieveAsIfPublished = true
                                };
                                RetrieveEntityResponse res = (RetrieveEntityResponse)_serviceProxy.Execute(req);
                                entityM = res.EntityMetadata;
                                entitiesMetaData.Add(entityM);
                            }
                            if (true)
                            {
                                Entity = e.FormattedValues["objecttypecode"];
                                Operation = e.FormattedValues["operation"];
                                Action = e.FormattedValues["action"];
                                ModifiedBy = e.GetAttributeValue<EntityReference>("userid").Name;
                                ModifiedOn = e.GetAttributeValue<DateTime>("createdon");

                                string[] attributes = e.Contains("attributemask") ? e.GetAttributeValue<string>("attributemask").Split(',') : null;
                                if (attributes.Count() > 1)
                                {
                                    try
                                    {
                                        Guid entityid = ((Audit)e).ObjectId.Id;
                                        AuditDetailCollection details = new AuditDetailCollection();
                                        RetrieveRecordChangeHistoryRequest changeRequest = new RetrieveRecordChangeHistoryRequest();
                                        changeRequest.Target = new EntityReference(logicalName, entityid);
                                        RetrieveRecordChangeHistoryResponse changeResponse = (RetrieveRecordChangeHistoryResponse)_serviceProxy.Execute(changeRequest);
                                        details = changeResponse.AuditDetailCollection;
                                        if (details != null && details.AuditDetails.Count > 0)
                                        {
                                            IEnumerable<AuditDetail> r = details.AuditDetails.Where(f => f.AuditRecord.Id == e.Id);
                                            if (r != null && r.Count() > 0)
                                            {
                                                StringBuilder stbr = new StringBuilder();
                                                foreach (AuditDetail a in r)
                                                {
                                                    foreach (string attrMask in attributes)
                                                    {
                                                        if (string.IsNullOrEmpty(attrMask))             //                                |
                                                        {                           //                                |
                                                            continue;   // Skip the remainder of this iteration. -----+
                                                        }
                                                        int tni;
                                                        bool isNumeric = int.TryParse(attrMask, out tni);

                                                        if (isNumeric)
                                                        {
                                                            AttributeMetadata aData = entityM.Attributes.Where(f => f.ColumnNumber == tni).FirstOrDefault();
                                                            string attributename = aData.LogicalName;
                                                            string attrDisplayName = aData.DisplayName.LocalizedLabels[0].Label;
                                                            var _new = a.GetType().Name == "AuditDetail" ? (((AuditDetail)a)).AuditRecord : (((AttributeAuditDetail)a)).NewValue;
                                                            var _old = a.GetType().Name == "AuditDetail" ? (((AuditDetail)a)).AuditRecord : (((AttributeAuditDetail)a)).OldValue;
                                                            var newvalue = _new != null && _new.Contains(attributename) ? _new.Attributes[attributename] : null;
                                                            var oldvalue = _old != null && _old.Contains(attributename) ? _old.Attributes[attributename] : null;
                                                            switch (aData.AttributeType)

                                                            #region SWITCH
                                                            {
                                                                case AttributeTypeCode.Lookup:
                                                                case AttributeTypeCode.Owner:
                                                                case AttributeTypeCode.Customer:
                                                                    newvalue = (newvalue != null && ((EntityReference)newvalue).Name != null) ? ((EntityReference)newvalue).Name.ToString() : "NULL";
                                                                    oldvalue = (oldvalue != null && ((EntityReference)oldvalue).Name != null) ? ((EntityReference)oldvalue).Name.ToString() : "NULL";
                                                                    break;

                                                                case AttributeTypeCode.DateTime:
                                                                    oldvalue = oldvalue != null ? ((DateTime)oldvalue).ToString() : "NULL";
                                                                    newvalue = newvalue != null ? ((DateTime)newvalue).ToString() : "NULL";
                                                                    break;

                                                                case AttributeTypeCode.BigInt:
                                                                case AttributeTypeCode.Boolean:
                                                                case AttributeTypeCode.Decimal:
                                                                case AttributeTypeCode.Double:
                                                                case AttributeTypeCode.Integer:
                                                                    break;
                                                                case AttributeTypeCode.Money:
                                                                    oldvalue = oldvalue != null ? ((Microsoft.Xrm.Sdk.Money)oldvalue).Value.ToString() : "NULL";
                                                                    newvalue = newvalue != null ? ((Microsoft.Xrm.Sdk.Money)newvalue).Value.ToString() : "NULL";

                                                                    break;
                                                                case AttributeTypeCode.PartyList:
                                                                    string _tempNV = string.Empty;
                                                                    string _tempOV = string.Empty;
                                                                    EntityCollection n = newvalue != null ? (EntityCollection)newvalue : null;
                                                                    EntityCollection o = oldvalue != null ? (EntityCollection)oldvalue : null;
                                                                    if (n != null)
                                                                    {
                                                                        foreach (Entity ae in n.Entities)
                                                                        {
                                                                            if (ae.LogicalName == "activityparty" && ae.Contains("partyid"))
                                                                            {
                                                                                _tempNV += ae.FormattedValues["partyid"].ToString() + ",";
                                                                            }
                                                                        }
                                                                    }
                                                                    if (o != null)
                                                                    {
                                                                        foreach (Entity ae in o.Entities)
                                                                        {
                                                                            if (ae.LogicalName == "activityparty" && ae.Contains("partyid"))
                                                                            {
                                                                                _tempOV += ae.FormattedValues["partyid"].ToString() + ",";
                                                                            }
                                                                        }
                                                                    }
                                                                    newvalue = _tempNV;
                                                                    oldvalue = _tempOV;
                                                                    break;
                                                                case AttributeTypeCode.Picklist:
                                                                    int v1 = newvalue != null ? ((OptionSetValue)newvalue).Value : -1;
                                                                    int v2 = oldvalue != null ? ((OptionSetValue)oldvalue).Value : -1;
                                                                    newvalue = v1 >= 0 ? ((EnumAttributeMetadata)aData).OptionSet.Options.Where(f => f.Value == v1).FirstOrDefault().Label.LocalizedLabels[0].Label.ToString() : "NULL";
                                                                    oldvalue = v2 >= 0 ? ((EnumAttributeMetadata)aData).OptionSet.Options.Where(f => f.Value == v2).FirstOrDefault().Label.LocalizedLabels[0].Label.ToString() : "NULL";
                                                                    break;
                                                                case AttributeTypeCode.State:
                                                                    int a1 = newvalue != null ? ((OptionSetValue)newvalue).Value : -1;
                                                                    int a2 = oldvalue != null ? ((OptionSetValue)oldvalue).Value : -1;
                                                                    newvalue = a1 >= 0 ? ((EnumAttributeMetadata)aData).OptionSet.Options.Where(f => f.Value == a1).FirstOrDefault().Label.LocalizedLabels[0].Label.ToString() : "NULL";
                                                                    oldvalue = a2 >= 0 ? ((EnumAttributeMetadata)aData).OptionSet.Options.Where(f => f.Value == a2).FirstOrDefault().Label.LocalizedLabels[0].Label.ToString() : "NULL";
                                                                    break;
                                                                case AttributeTypeCode.Status:
                                                                    int b1 = newvalue != null ? ((OptionSetValue)newvalue).Value : -1;
                                                                    int b2 = oldvalue != null ? ((OptionSetValue)oldvalue).Value : -1;
                                                                    newvalue = b1 >= 0 ? ((EnumAttributeMetadata)aData).OptionSet.Options.Where(f => f.Value == b1).FirstOrDefault().Label.LocalizedLabels[0].Label.ToString() : "NULL";
                                                                    oldvalue = b2 >= 0 ? ((EnumAttributeMetadata)aData).OptionSet.Options.Where(f => f.Value == b2).FirstOrDefault().Label.LocalizedLabels[0].Label.ToString() : "NULL";
                                                                    break;
                                                                case AttributeTypeCode.String:
                                                                case AttributeTypeCode.Memo:
                                                                    break;
                                                                case AttributeTypeCode.Virtual:
                                                                    break;
                                                                default: break;
                                                            }
                                                            #endregion

                                                            Attribute = attrDisplayName;
                                                            OldValue = oldvalue != null ? oldvalue.ToString() : "NULL";
                                                            NewValue = newvalue != null ? newvalue.ToString() : "NULL";
                                                            string insertQuery = "INSERT INTO [dbo].[WRS_CRMAuditLog_UAT] ([AuditId],[EntityGuid],[Entity],[Action],[Attribute],[OldValue],[NewValue],[ModifiedBy],[ModifiedOn],[AuditLogCreatedOn] ) " +
                                  "VALUES " + string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}',{8},{9});", auditId.ToString(), entityid.ToString(), Entity, Action, attrDisplayName.Replace("'", ""), OldValue.Replace("'", ""), NewValue.Replace("'", ""), ModifiedBy.Replace("'", ""), "convert(datetime,'" + ModifiedOn.ToString("yyyy -MM-dd HH:mm:ss") + "')", "convert(datetime,'" + DateTime.UtcNow.ToString("yyyy -MM-dd HH:mm:ss") + "')");
                                                            try
                                                            {
                                                                bool openConn = (cnn.State == ConnectionState.Open);
                                                                if (!openConn)
                                                                {
                                                                    cnn.Open();
                                                                }
                                                                SqlCommand command = new SqlCommand();
                                                                command = new SqlCommand(insertQuery, cnn);
                                                                SqlDataReader dr = command.ExecuteReader();
                                                                dr.Close();
                                                                command.Dispose();
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                continue;
                                                            }
                                                            count = count + 1;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string insertQuery = "INSERT INTO [dbo].[WRS_CRMAuditLog] ([AuditId],[EntityGuid],[Entity],[Action],[Attribute],[OldValue],[NewValue],[ModifiedBy],[ModifiedOn]) " +
                                         "VALUES " + string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}',{8});", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "NULL", "ERROR", "ERROR", "ERROR", "ERROR", "ERROR", "convert(datetime,'" + ModifiedOn.ToString("yyyy -MM-dd HH:mm:ss") + "')");
                                        bool openConn = (cnn.State == ConnectionState.Open);
                                        if (!openConn)
                                        {
                                            cnn.Open();
                                        }
                                        SqlCommand command = new SqlCommand();
                                        command = new SqlCommand(insertQuery, cnn);
                                        SqlDataReader dr = command.ExecuteReader();
                                        dr.Close();
                                        command.Dispose();
                                        _serviceProxy = serviceClient();
                                        continue;
                                    }
                                }
                            }
                            #endregion

                        }
                    }
                    catch (Exception ex)
                    {
                        string insertQuery = "INSERT INTO [dbo].[WRS_CRMAuditLog] ([AuditId],[EntityGuid],[Entity],[Action],[Attribute],[OldValue],[NewValue],[ModifiedBy],[ModifiedOn]) " +
                         "VALUES " + string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}',{8});", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "NULL", "ERROR", "ERROR", "ERROR", "ERROR", "ERROR", "convert(datetime,'" + ModifiedOn.ToString("yyyy -MM-dd HH:mm:ss") + "')");
                        bool openConn = (cnn.State == ConnectionState.Open);
                        if (!openConn)
                        {
                            cnn.Open();
                        }
                        SqlCommand command = new SqlCommand();
                        command = new SqlCommand(insertQuery, cnn);
                        SqlDataReader dr = command.ExecuteReader();
                        dr.Close();
                        command.Dispose();
                        _serviceProxy = serviceClient();
                        continue;
                    }

                    cnn.Close();
                }
                #endregion 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IOrganizationService serviceClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            CrmServiceClient crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

        /// <summary>
        /// Gets the formatted value.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns>The formatted value.</returns>
        private string getFormattedValue(Entity entity, string attributeName)
        {
            if (attributeName == "transactionid")
                return entity.Id.ToString();

            KeyValuePair<string, object> kvp = new KeyValuePair<string, object>();
            KeyValuePair<string, string> formattedValue = new KeyValuePair<string, string>();
            string value = "";
            if (entity.FormattedValues.Keys.Contains(attributeName))
            {
                formattedValue = entity.FormattedValues.First(k => k.Key == attributeName);
                value = formattedValue.Value;
            }
            else if (entity.Attributes.Keys.Contains(attributeName))
            {
                kvp = entity.Attributes.First(k => k.Key == attributeName);

                Type t = kvp.Value.GetType();
                if (t.Name == "EntityReference")
                {
                    EntityReference er = (EntityReference)kvp.Value;
                    value = er.Name;
                }
                else if (t.Name == "AliasedValue")
                {
                    AliasedValue av = (AliasedValue)kvp.Value;
                    Type t2 = av.Value.GetType();
                    if (t2.Name == "EntityReference")
                    {
                        EntityReference er2 = (EntityReference)av.Value;
                        value = er2.Name;
                    }
                    else
                        value = av.Value.ToString();
                }
                else if (t.Name == "OptionSetValue")
                {
                    OptionSetValue osv = (OptionSetValue)kvp.Value;
                    value = osv.Value.ToString();
                }
                else if (attributeName == "entityimage" && t.Name == "Byte[]")
                {
                    byte[] binaryData = (byte[])kvp.Value;
                    value = System.Convert.ToBase64String(binaryData, 0, binaryData.Length);
                }
                else
                {
                    value = kvp.Value.ToString();
                }
            }
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="insertQuery"></param>
        private static void WriteLogsToDB(string sql)
        {
            if (sql != string.Empty)
            {
                SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL_CONNECTION_STRING"].ToString());
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string[] GetAuditEnabledEntities()
        {
            string fetch =
                "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                  "<entity name='wrs_configuration'>" +
                    "<attribute name='wrs_configurationid' />" +
                    "<attribute name='wrs_name' />" +
                    "<attribute name='createdon' />" +
                    "<attribute name='wrs_value' />" +
                    "<order attribute='wrs_name' descending='false' />" +
                    "<filter type='and'>" +
                      "<condition attribute='wrs_key' operator='eq' value='AUDIT_ENTITES' />" +
                      "<condition attribute='wrs_name' operator='eq' value='WRS_AUDIT' />" +
                    "</filter>" +
                  "</entity>" +
                "</fetch>";
            var results = _serviceProxy.RetrieveMultiple(new FetchExpression(fetch));
            if (results != null && results.Entities.Count > 0)
            {
                string auditEntities = results.Entities.FirstOrDefault().Contains("wrs_value") ?
                    results.Entities.FirstOrDefault().GetAttributeValue<string>("wrs_value") : string.Empty;
                return auditEntities.Split(',');
            }
            return null;
        }

        public List<Entity> RetrieveAllRecords(IOrganizationService service, string fetch)
        {
            var moreRecords = false;
            int page = 1;
            var cookie = string.Empty;
            List<Entity> Entities = new List<Entity>();
            do
            {
                var xml = string.Format(fetch, cookie);
                var collection = service.RetrieveMultiple(new FetchExpression(xml));

                if (collection.Entities.Count >= 0) Entities.AddRange(collection.Entities);

                moreRecords = collection.MoreRecords;
                if (moreRecords)
                {
                    page++;
                    cookie = string.Format("paging-cookie='{0}' page='{1}'", System.Security.SecurityElement.Escape(collection.PagingCookie), page);
                }
            } while (moreRecords);

            return Entities;
        }
    }
}

