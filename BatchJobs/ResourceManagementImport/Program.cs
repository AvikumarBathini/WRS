using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using WRS.Xrm;

namespace ResourceManagementImport
{
    class Program
    {
        public static LogHandler logger = new LogHandler("LOGS");
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        public static IOrganizationService CrmService = CrmServiceClient();
        public static Dictionary<int, string> RMAllocationSegmentOptionSet = GetOptionSetData("wrs_resourcemanagement", "wrs_segment");
        public static Dictionary<int, string> RMEventTypeOptionSet = GetOptionSetData("wrs_resourcemanagement", "wrs_eventtype");
        public static Dictionary<int, string> RMResourceOptionSet = GetOptionSetData("wrs_resourcemanagement", "wrs_rmresource");
        private string NCId;

        static void Main(string[] args)
        {
            Program p = new Program();
            try
            {
                const string RMAllocationSegmentStoreProc = "CrmGetRMAllocationSegment";
                const string RMEventTypeStoreProcName = "CrmGetRMEventType";
                const string ResourceManagementStoreProcName = "CrmGetRMEventStock";
                const string RMResourceStoreProcName = "CrmGetRMResource";

                const string RMAllocationSegment = "RMAllocationSegment";
                const string RMEventType = "RMEventType";
                const string RMResource = "RMResource";
                const string ResourceManagement = "RMEventStock";

                p.CreateUpdateDeleteOptionSet(RMAllocationSegment, "wrs_segment", RMAllocationSegmentStoreProc, RMAllocationSegmentOptionSet);

                p.CreateUpdateDeleteOptionSet(RMEventType, "wrs_eventtype", RMEventTypeStoreProcName, RMEventTypeOptionSet);

                p.CreateUpdateDeleteOptionSet(RMResource, "wrs_rmresource", RMResourceStoreProcName, RMResourceOptionSet);

                p.UpsertResourceManagementRecords(ResourceManagement, p, ResourceManagementStoreProcName);
            }
            catch (Exception ex)
            {
                logger.TraceEx(ex);
            }
        }

        private static Dictionary<int, string> GetOptionSetData(string EntityLogicalName, string AttributeLogicalName)
        {
            Dictionary<int, string> optionSetData = new Dictionary<int, string>();
            var attributeRequest = new RetrieveAttributeRequest
            {
                EntityLogicalName = EntityLogicalName,
                LogicalName = AttributeLogicalName,
                RetrieveAsIfPublished = true
            };

            var attributeResponse = (RetrieveAttributeResponse)CrmService.Execute(attributeRequest);
            var attributeMetadata = (EnumAttributeMetadata)attributeResponse.AttributeMetadata;
            foreach (var o in attributeMetadata.OptionSet.Options)
            {
                optionSetData.Add((int)o.Value, o.Label.UserLocalizedLabel.Label);
            }
            return optionSetData;
        }

        private void UpsertResourceManagementRecords(string resourceManagement, Program p, string storeProc)
        {
            DataTable CustomerTable = RetrieveRecordsFromDB(storeProc);
            if (CustomerTable != null && CustomerTable.Rows.Count > 0)
                try
                {
                    string primaryKeyLogicalName = "wrs_resourcemanagementid";
                    string alternatekeyLogicalName = "wrs_id";
                    int totalCount = CustomerTable.Rows.Count;
                    List<EntityCollection> _lisEntityCollection = GetEntityCollection(CustomerTable, PrepareResourceManagementObject);
                    if (_lisEntityCollection != null)
                    {
                        foreach (EntityCollection ec in _lisEntityCollection)
                        {
                            p.CrmExecuteMultiple(ec, p, resourceManagement, wrs_resourcemanagement.EntityLogicalName, alternatekeyLogicalName, primaryKeyLogicalName);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
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

        private Entity PrepareResourceManagementObject(DataRow dr)
        {

            if (dr["RmEventStockId"] != DBNull.Value && int.Parse(dr["RmEventStockId"].ToString()) > 0)
            {
                Entity ResourceManagementToBeCreated = new Entity(wrs_resourcemanagement.EntityLogicalName, "wrs_id", int.Parse(dr["RmEventStockId"].ToString()));
                wrs_resourcemanagement ResourceManagementToBeCreatedFromDB = ResourceManagementToBeCreated.ToEntity<wrs_resourcemanagement>();
                ResourceManagementToBeCreatedFromDB.wrs_id = int.Parse(dr["RmEventStockId"].ToString());

                ResourceManagementToBeCreatedFromDB.wrs_RmEventStockId = int.Parse(dr["RmEventStockId"].ToString());

                if (dr["EventId"] != DBNull.Value && int.Parse(dr["EventId"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_RMEventId = int.Parse(dr["EventId"].ToString());

                if (dr["Quantity"] != DBNull.Value && int.Parse(dr["Quantity"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_Quantity = int.Parse(dr["Quantity"].ToString());

                if (dr["UsedQty"] != DBNull.Value && int.Parse(dr["UsedQty"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_UsedQuantity = int.Parse(dr["UsedQty"].ToString());

                if (dr["ReservedQty"] != DBNull.Value && int.Parse(dr["ReservedQty"].ToString()) >= 0)
                    ResourceManagementToBeCreatedFromDB.wrs_ReservedQuantity = int.Parse(dr["ReservedQty"].ToString());

                ResourceManagementToBeCreatedFromDB.wrs_Status = dr["Active"] != DBNull.Value ? (bool)(dr["Active"]) : false;

                if (dr["RMEventTypePkId"] != DBNull.Value && int.Parse(dr["RMEventTypePkId"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_RMEventTypePkId = int.Parse(dr["RMEventTypePkId"].ToString());

                if (dr["EventType"] != DBNull.Value && int.Parse(dr["EventType"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_EventType = new OptionSetValue(int.Parse(dr["EventType"].ToString()));

                if (dr["RMEventTypePkId"] != DBNull.Value && int.Parse(dr["RMEventTypePkId"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_RMEventTypePkId = int.Parse(dr["RMEventTypePkId"].ToString());

                if (dr["StartDateTime"] != DBNull.Value)
                    ResourceManagementToBeCreatedFromDB.wrs_StartDateTime = ((DateTime)(dr["StartDateTime"])).ToUniversalTime();

                if (dr["OffSaleDateTime"] != DBNull.Value)
                    ResourceManagementToBeCreatedFromDB.wrs_OffSaleDateTime = ((DateTime)(dr["OffSaleDateTime"])).ToUniversalTime();

                ResourceManagementToBeCreatedFromDB.wrs_GalaxyPLU = dr["GalaxyPLU"] != DBNull.Value ? dr["GalaxyPLU"].ToString() : "";


                if (dr["RMResourcePkId"] != DBNull.Value && int.Parse(dr["RMResourcePkId"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_RMResourcePkId = int.Parse(dr["RMResourcePkId"].ToString());

                if (dr["RMResource"] != DBNull.Value && int.Parse(dr["RMResource"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_RMResource = new OptionSetValue(int.Parse(dr["RMResource"].ToString()));

                if (dr["segment"] != DBNull.Value && int.Parse(dr["segment"].ToString()) > 0)
                    ResourceManagementToBeCreatedFromDB.wrs_segment = new OptionSetValue(int.Parse(dr["segment"].ToString()));

                ResourceManagementToBeCreatedFromDB.wrs_sourcefrom = "NC";

                return ResourceManagementToBeCreatedFromDB;
            }
            return null;
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

        public int RetrieveAlternateKey(Guid accountID, string entityLogicalName)
        {
            QueryExpression accountbyID = new QueryExpression(entityLogicalName);
            accountbyID.Criteria.AddCondition(new ConditionExpression("id", ConditionOperator.Equal, accountID));
            accountbyID.ColumnSet.Columns.Add("wrs_id");
            Entity e = CrmServiceClient().Retrieve(entityLogicalName, accountID, accountbyID.ColumnSet);
            if (e != null && e.Attributes["wrs_id"] != null)
                return e.GetAttributeValue<int>("wrs_id");
            return 0;
        }

        public static void ErrorLog(string sErrMsg)
        {
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            string sErrorTime = sYear + sMonth + sDay;

            string sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";
            StreamWriter sw = new StreamWriter("C:/" + sErrorTime, true);
            sw.WriteLine(sLogFormat + sErrMsg);
            sw.Flush();
            sw.Close();
        }

        public static IOrganizationService CrmServiceClient()
        {
            CrmServiceClient crmConnD = null;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

        public UpsertRequest CreateUpsertRequest(Entity target)
        {
            return new UpsertRequest()
            {
                Target = target
            };
        }

        public InsertOptionValueRequest AddOptionSetItem(string entityName, string fieldName, string optionLabel, int optionValue)
        {
            // Create the Insert request
            var insertOptionValueRequest = new InsertOptionValueRequest
            {
                AttributeLogicalName = fieldName,
                EntityLogicalName = entityName,
                Label = new Label(optionLabel, 1033),
                Value = optionValue     // CRM defaulted, if not specified
            };
            return insertOptionValueRequest;

        }

        public UpdateOptionValueRequest UpdateOptionSetItem(string entityName, string fieldName, string optionLabel, int optionValue)
        {
            // Create the Update request
            var updateOptionValueRequest = new UpdateOptionValueRequest
            {
                AttributeLogicalName = fieldName,
                EntityLogicalName = entityName,
                Label = new Label(optionLabel, 1033),
                Value = optionValue     // CRM defaulted, if not specified
            };
            return updateOptionValueRequest;
        }

        public DeleteOptionValueRequest DeleteOptionSetItem(string entityName, string fieldName, int optionValue)
        {
            // Create the Delete request
            var deleteOptionValueRequest = new DeleteOptionValueRequest
            {
                AttributeLogicalName = fieldName,
                EntityLogicalName = entityName,
                Value = optionValue
            };
            return deleteOptionValueRequest;
        }

        public void CreateUpdateDeleteOptionSet(string TableName, string attributeLogicalName, string storeProc, Dictionary<int, string> optionSetValues)
        {
            string mainSuccessLog = string.Empty;
            //Retrieve records from NC DB
            DataTable datatable = RetrieveRecordsFromDB(storeProc);
            if (datatable != null && datatable.Rows.Count > 0)
            {
                foreach (DataRow dr in datatable.Rows)
                {
                    int value = int.Parse(dr["Id"].ToString());
                    string label = dr["Name"].ToString();
                    string EntityId = (dr.Table.Columns.Contains("EntityId") && dr["EntityId"] != DBNull.Value) ? dr["EntityId"].ToString() : string.Empty;
                    try
                    {
                        if (optionSetValues.ContainsKey(value))
                        {
                            if (optionSetValues[value] != label)
                            {
                                UpdateOptionValueRequest req = UpdateOptionSetItem("wrs_resourcemanagement", attributeLogicalName, label, value);
                                UpdateOptionValueResponse resp = (UpdateOptionValueResponse)CrmService.Execute(req);
                                if (!string.IsNullOrEmpty(EntityId))
                                    UpdateSyncStatus(TableName, EntityId);
                            }
                        }
                        else
                        {
                            InsertOptionValueRequest req = AddOptionSetItem("wrs_resourcemanagement", attributeLogicalName, label, value);
                            InsertOptionValueResponse resp = (InsertOptionValueResponse)CrmService.Execute(req);
                            if (!string.IsNullOrEmpty(EntityId))
                                UpdateSyncStatus(TableName, EntityId);
                        }

                        optionSetValues.Remove(value);
                        //if (!string.IsNullOrEmpty(mainSuccessLog))
                        //    mainSuccessLog = mainSuccessLog + ",";
                        //mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + TableName + "','" + value + "')";
                    }
                    catch (Exception e)
                    {
                        string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','','','" + TableName + "ID : " + value + "'";
                        WriteLogsToDB(errorLog);
                        continue;
                    }
                }
                if (optionSetValues.Count > 0)
                {
                    foreach (var item in optionSetValues)
                    {
                        try
                        {
                            DeleteOptionValueRequest req = DeleteOptionSetItem("wrs_resourcemanagement", attributeLogicalName, item.Key);
                            DeleteOptionSetResponse resp = (DeleteOptionSetResponse)CrmService.Execute(req);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
                //WriteMigrateStatusLogs(mainSuccessLog);
            }
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

        private static void WriteLogsToDB(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = String.Empty;
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

        public void CrmExecuteMultiple(EntityCollection ecoll, Program p, string table, string entityLogicalName, string alternate, string primary)
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
                    multipleReq.Requests.Add(CreateUpsertRequest(e));
                }
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)Program.CrmServiceClient().Execute(multipleReq);
                List<string> existingRecords = new List<string>();

                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();

                    if (e.Fault == null)
                    {
                        Guid ID = ((UpsertResponse)e.Response).Target.Id;
                        if (NCId != "")
                        {
                            //ErrorLog(string.Format("{0} with Guid {1} is created for NC DB ID {2}", table, ID, alternateKey));
                            existingRecords.Add(NCId);

                            if (mainSuccessLog != string.Empty)
                            {
                                mainSuccessLog = mainSuccessLog + ",";
                            }

                            mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + NCId + "')";
                        }
                    }

                    else if (e.Fault != null)
                    {
                        //ErrorLog(string.Format(("{0} with NC DB ID {1} is failed for upsert with message {2}"), table, ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString(), e.Fault.Message));
                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
                    }
                }
                foreach (string l in existingRecords)
                {
                    p.UpdateSyncStatus(table, l);
                    //ErrorLog(string.Format("Sync Status updated in NC DB for {0} with ID - {1}", table, l));
                }

                WriteMigrateStatusLogs(mainSuccessLog);
            }
            catch (Exception e)
            {
                string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                WriteLogsToDB(errorLog);
            }
        }

        private static void WriteMigrateStatusLogs(string insertQuery)
        {
            logger.Log(insertQuery);
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
}
