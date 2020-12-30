using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System.Data;
using WRS.Xrm;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net;

namespace BlackoutCalendarImport
{
    class Program
    {
        public static IOrganizationService CrmService = serviceClient();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        private string NCId;

        static void Main(string[] args)
        {
            string alternateKeyLogicalName = "wrs_id";

            const string BlackoutCalendarStoreProcName = "CrmGetBlackoutCalendar";
            const string BlackoutCalendarDetailStoreProcName = "CrmGetBlackoutCalendarDetail";
            const string BlackoutCalendarProductStoreProcName = "CrmGetBlackoutCalendarProducts";

            const string BlackoutCalendar = "BlackoutCalendarItem";
            const string BlackoutCalendarDetail = "BlackoutCalendarDetail";
            const string BlackoutCalendarProduct = "BlackoutCalendarProductMapping";

            Program p = new Program();

            p.UpsertBlackoutCalendar(BlackoutCalendar, p, BlackoutCalendarStoreProcName, alternateKeyLogicalName);

            p.UpsertBlackoutCalendarDetail(BlackoutCalendarDetail, p, BlackoutCalendarDetailStoreProcName, alternateKeyLogicalName);

            p.UpsertBlackoutCalendarProduct(BlackoutCalendarProduct, p, BlackoutCalendarProductStoreProcName, alternateKeyLogicalName);
        }

        private void UpsertBlackoutCalendar(string blackoutCalendar, Program p, string storeProc, string alternateKeyLogicalName)
        {
            DataTable BlackoutCalendarTable = RetrieveRecordsFromDB(storeProc);
            if (BlackoutCalendarTable != null && BlackoutCalendarTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_blackoutcalendar";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(BlackoutCalendarTable, PrepareBlackoutCalendarObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        try
                        {
                            p.CrmExecuteMultiple(ec, p, blackoutCalendar, wrs_blackoutcalendar.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                        }
                        catch (Exception e)
                        {
                            string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + primaryKeyLogicalName + "','Genneric'";
                            WriteLogsToDB(errorLog);
                            continue;
                        }
                    }
            }
        }

        private void UpsertBlackoutCalendarDetail(string blackoutCalendarDetail, Program p, string storeProc, string alternateKeyLogicalName)
        {
            DataTable BlackoutCalendarDetailTable = RetrieveRecordsFromDB(storeProc);
            if (BlackoutCalendarDetailTable != null && BlackoutCalendarDetailTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_blackoutcalendardetailid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(BlackoutCalendarDetailTable, PrepareBlackoutCalendarDetailObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        try
                        {
                            p.CrmExecuteMultiple(ec, p, blackoutCalendarDetail, wrs_blackoutcalendardetail.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                        }
                        catch (Exception e)
                        {
                            string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + primaryKeyLogicalName + "','Genneric'";
                            WriteLogsToDB(errorLog);
                            continue;
                        }
                    }
            }
        }

        private void UpsertBlackoutCalendarProduct(string blackoutCalendarProduct, Program p, string storeProc, string alternateKeyLogicalName)
        {
            DataTable BlackoutCalendarProductTable = RetrieveRecordsFromDB(storeProc);
            if (BlackoutCalendarProductTable != null && BlackoutCalendarProductTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_blackoutcalendardetailid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(BlackoutCalendarProductTable, PrepareBlackoutCalendarProductObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        try
                        {
                            p.CrmExecuteMultiple(ec, p, blackoutCalendarProduct, wrs_blackoutcalendarproduct.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                        }
                        catch (Exception e)
                        {
                            string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + primaryKeyLogicalName + "','Genneric'";
                            WriteLogsToDB(errorLog);
                            continue;
                        }
                    }
            }
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
                catch (Exception e)
                {
                    string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + table + "',''";
                    WriteLogsToDB(errorLog);
                    continue;
                }
            }
            return _lisEntityCollection;
        }

        private Entity PrepareBlackoutCalendarObject(DataRow dr)
        {

            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity BlackoutCalendarToBeCreated = new Entity(wrs_blackoutcalendar.EntityLogicalName, "wrs_id", dr["Id"].ToString());
                wrs_blackoutcalendar BlackoutCalendarToBeCreatedFromDB = BlackoutCalendarToBeCreated.ToEntity<wrs_blackoutcalendar>();

                BlackoutCalendarToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());

                //if (dr["CustomerId"] != DBNull.Value && int.Parse(dr["CustomerId"].ToString()) > 0)
                //    BlackoutCalendarToBeCreatedFromDB.wrs_customerid = new EntityReference(Contact.EntityLogicalName, "wrs_id", int.Parse(dr["CustomerId"].ToString()));

                BlackoutCalendarToBeCreatedFromDB.wrs_name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";
                BlackoutCalendarToBeCreatedFromDB.wrs_remarks = dr["Remarks"] != DBNull.Value ? dr["Remarks"].ToString() : "";

                if (dr["CreatedOnUtc"] != DBNull.Value)
                    BlackoutCalendarToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];

                return BlackoutCalendarToBeCreatedFromDB;
            }
            return null;
        }

        private Entity PrepareBlackoutCalendarDetailObject(DataRow dr)
        {

            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity BlackoutCalendarDetailToBeCreated = new Entity(wrs_blackoutcalendardetail.EntityLogicalName, "wrs_id", dr["Id"].ToString());
                wrs_blackoutcalendardetail BlackoutCalendarDetailToBeCreatedFromDB = BlackoutCalendarDetailToBeCreated.ToEntity<wrs_blackoutcalendardetail>();

                BlackoutCalendarDetailToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());

                if (dr["BlackoutCalendarId"] != DBNull.Value && int.Parse(dr["BlackoutCalendarId"].ToString()) > 0)
                    BlackoutCalendarDetailToBeCreatedFromDB.wrs_blackoutcalendarid = new EntityReference(wrs_blackoutcalendar.EntityLogicalName, "wrs_id", int.Parse(dr["BlackoutCalendarId"].ToString()));

                BlackoutCalendarDetailToBeCreatedFromDB.wrs_name = dr["Name"] != DBNull.Value ? dr["Name"].ToString() : "";

                if (dr["CreatedOnUtc"] != DBNull.Value)
                    BlackoutCalendarDetailToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];

                if (dr["DateFrom"] != DBNull.Value)
                    BlackoutCalendarDetailToBeCreatedFromDB.wrs_datefrom = (DateTime?)dr["DateFrom"];
                else
                    BlackoutCalendarDetailToBeCreatedFromDB.wrs_datefrom = null;

                if (dr["DateTo"] != DBNull.Value)
                    BlackoutCalendarDetailToBeCreatedFromDB.wrs_dateto = (DateTime?)dr["DateTo"];
                else
                    BlackoutCalendarDetailToBeCreatedFromDB.wrs_dateto = null;

                return BlackoutCalendarDetailToBeCreatedFromDB;
            }
            return null;
        }

        private Entity PrepareBlackoutCalendarProductObject(DataRow dr)
        {

            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {

                Entity BlackoutCalendarProductToBeCreated = new Entity(wrs_blackoutcalendarproduct.EntityLogicalName, "wrs_id", dr["Id"].ToString());
                wrs_blackoutcalendarproduct BlackoutCalendarProductToBeCreatedFromDB = BlackoutCalendarProductToBeCreated.ToEntity<wrs_blackoutcalendarproduct>();

                BlackoutCalendarProductToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());

                if (dr["ProductId"] != DBNull.Value && int.Parse(dr["ProductId"].ToString()) > 0)
                {
                    BlackoutCalendarProductToBeCreatedFromDB.wrs_Product = new EntityReference(Product.EntityLogicalName, "wrs_id", int.Parse(dr["ProductId"].ToString()));
                }

                if (dr["BlackoutCalendarId"] != DBNull.Value && int.Parse(dr["BlackoutCalendarId"].ToString()) > 0)
                {
                    BlackoutCalendarProductToBeCreatedFromDB.wrs_BlackoutCalendar = new EntityReference(wrs_blackoutcalendar.EntityLogicalName, "wrs_id", int.Parse(dr["BlackoutCalendarId"].ToString()));
                }

                if (dr["CreatedOnUtc"] != DBNull.Value)
                    BlackoutCalendarProductToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];

                return BlackoutCalendarProductToBeCreatedFromDB;

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

        public static IOrganizationService serviceClient()
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
                    string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','SP','SP_ID'";
                    WriteLogsToDB(errorLog);
                }
            }
            return DBTable;
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
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleReq);
                List<string> existingRecords = new List<string>();

                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    if (e.Fault == null)
                    {
                        Guid ID = ((UpsertResponse)e.Response).Target.Id;
                        string alternateKey = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();

                        if ((!entityLogicalName.Equals(TransactionCurrency.EntityLogicalName) && int.Parse(alternateKey) != 0) ||
                            (entityLogicalName.Equals(TransactionCurrency.EntityLogicalName) && alternateKey != ""))
                        {
                            //ErrorLog(string.Format("{0} with Guid {1} is created for NC DB ID {2}", table, ID, alternateKey));
                            existingRecords.Add(alternateKey);

                            if (mainSuccessLog != string.Empty)
                            {
                                mainSuccessLog = mainSuccessLog + ",";
                            }

                            mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + alternateKey + "')";
                        }
                    }

                    else if (e.Fault != null)
                    {
                        NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
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
}
