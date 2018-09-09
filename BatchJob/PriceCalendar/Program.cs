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

namespace PriceCalendar
{
    class Program
    {
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);

        private string NCId;

        static void Main(string[] args)
        {
            string alternateKeyLogicalName = "wrs_pricecalendarncid";
            const string PriceCalendarStoreProcName = "CrmGetPriceCalendar";
            const string PriceCalendar = "PriceCalendars";

            Program p = new Program();

            p.UpsertPriceCalendar(PriceCalendar, p, PriceCalendarStoreProcName, alternateKeyLogicalName);
        }

        private void UpsertPriceCalendar(string priceCalendar, Program p, string storeProc, string alternateKeyLogicalName)
        {

            DataTable PriceCalendarTable = RetrieveRecordsFromDB(storeProc);
            if (PriceCalendarTable != null && PriceCalendarTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_id";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(PriceCalendarTable, PreparePriceCalendarObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, priceCalendar, Product.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                    }
            }
        }

        private Entity PreparePriceCalendarObject(DataRow dr)
        {
            Entity priceCalendar = new Entity(wrs_pricecalendar.EntityLogicalName, "wrs_pricecalendarncid", int.Parse(dr["Id"].ToString()));
            wrs_pricecalendar priceCalendarObj = priceCalendar.ToEntity<wrs_pricecalendar>();

            if (dr["id"] != DBNull.Value && int.Parse(dr["id"].ToString()) > 0)
            {
                priceCalendarObj.wrs_PriceCalendarNCId = int.Parse(dr["Id"].ToString());
                priceCalendarObj.wrs_name = "Price Calendar# " + dr["id"].ToString();
                if (dr["CalendarDate"] != DBNull.Value)
                    priceCalendarObj.wrs_CalendarDate = (DateTime)dr["CalendarDate"];
                if (dr["CalendarTypeId"] != DBNull.Value && int.Parse(dr["CalendarTypeId"].ToString()) > 0)
                {
                    priceCalendarObj.wrs_park = new EntityReference(wrs_park.EntityLogicalName, "wrs_id", int.Parse(dr["CalendarTypeId"].ToString()));
                }

                if (dr["PriceGroupId"] != DBNull.Value && int.Parse(dr["PriceGroupId"].ToString()) > 0)
                {
                    priceCalendarObj.wrs_pricelist = new EntityReference(PriceLevel.EntityLogicalName, "wrs_id", int.Parse(dr["PriceGroupId"].ToString()));
                }

                return priceCalendarObj;
            }
            return null;
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

        public static IOrganizationService serviceClient()
        {
            CrmServiceClient crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
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
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)Program.serviceClient().Execute(multipleReq);
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

        public UpsertRequest CreateUpsertRequest(Entity target)
        {
            return new UpsertRequest()
            {
                Target = target
            };
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

    }
}
