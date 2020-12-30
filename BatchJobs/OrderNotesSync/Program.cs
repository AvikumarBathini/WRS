using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
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
using System.Text;
using System.Threading.Tasks;
using WRS.Xrm;

namespace OrderNotesSync
{
    class Program
    {
        public static IOrganizationService CrmService = CrmServiceClient();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        private string NCId;
        const string OrderNote = "OrderNote";
        const string OrderStoreNotesProcName = "CrmGetOrderNotes";
        const string OrderItemStoreProcName = "CrmGetOrderItemDetails";
        const string OrderItem = "OrderItem";
        static void Main(string[] args)
        {
            string alternateKeyLogicalName = "wrs_id";
            const string OrderStoreProcName = "CrmGetOrderDetailsForNotes";
            const string Order = "Order";
            Program p = new Program();

            p.SyncOrderNotes(Order, p, OrderStoreProcName, alternateKeyLogicalName);

        }
        private List<string> GetOrdersList(DataTable table)
        {
            List<string> orderIds = new List<string>();
            int totalCount = table.Rows.Count;
            if (totalCount == 0)
                return null;


            foreach (DataRow dr in table.Rows)
            {
                if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
                {
                    orderIds.Add(dr["Id"].ToString());
                }
            }
            orderIds = orderIds.Distinct().ToList();
            return orderIds;
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
            EntityCollection test = new EntityCollection();


            //Add maximum of (batchCount) number of records in each _lisEntityCollection -> EntityCollection object
            Parallel.For(0, totalCount, j =>
            {
                try
                {
                    var entRec = myMethod(table.Rows[j]);
                    _lisEntityCollection[j % requests].Entities.Add(entRec);
                }
                catch (Exception ex)
                {

                }
            });

            return _lisEntityCollection;
        }

        private void SyncOrderNotes(string Order, Program p, string storeProc, string alternateKeyLogicalName)
        {
            DataTable OrderTable = RetrieveRecordsFromDB(storeProc);
            if (OrderTable != null && OrderTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "salesorderid";
                List<string> _lisEntityCollection = GetOrdersList(OrderTable);
                if (_lisEntityCollection != null)
                    CreateOrderNotes(OrderNote, p, OrderStoreNotesProcName, "wrs_id", _lisEntityCollection);
                //Parallel.ForEach(_lisEntityCollection, ec =>
                //{
                //    p.CrmExecuteMultiple(ec, p, Order, SalesOrder.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName, false, false, false, true);
                //});

                //foreach (EntityCollection ec in _lisEntityCollection)
                //{
                //    p.CrmExecuteMultiple(ec, p, Order, SalesOrder.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName, false, false, false, true);
                //}
            }
        }

        private void CreateOrderNotes(string Order, Program p, string storeProc, string alternateKeyLogicalName, List<string> orderColl)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Id");
            foreach (string e in orderColl)
            {
                dt.Rows.Add(int.Parse(e));
            }
            DataTable OrderNotesTable = RetrieveRecordsFromDB(storeProc, dt);
            if (OrderNotesTable != null && OrderNotesTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_ordernoteid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(OrderNotesTable, PrepareOrderNoteObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Order, wrs_ordernote.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName, true);
                    }
            }
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

        public static IOrganizationService CrmServiceClient()
        {
            CrmServiceClient crmConnD = null;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

        public DataTable RetrieveRecordsFromDB(string storeProc, DataTable dt = null)
        {
            DataTable DBTable = new DataTable();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var command = new SqlCommand(storeProc, conn))
            using (var da = new SqlDataAdapter(command))
            {
                try
                {

                    command.CommandType = CommandType.StoredProcedure;
                    if (dt != null)
                    {
                        SqlParameter tvparam = command.Parameters.AddWithValue("@List", dt);
                        tvparam.SqlDbType = SqlDbType.Structured;
                    }
                    da.Fill(DBTable);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }

            return DBTable;
        }

        private bool CheckContactExists(int ncid)
        {
            QueryExpression qe = new QueryExpression("contact");
            qe.Criteria.Conditions.Add(new ConditionExpression("wrs_id", ConditionOperator.Equal, ncid));
            qe.ColumnSet = new ColumnSet(new string[] { "wrs_id" });
            EntityCollection Contcoll = CrmService.RetrieveMultiple(qe);
            if (Contcoll.Entities.Count > 0)
            {
                return true;
            }
            return false;
        }

        private Entity PrepareOrderNoteObject(DataRow dr)
        {
            //if (dr["Id"] != DBNull.Value)
            //{
            //    Entity ann = new Entity("annotation");
            //    Annotation note = ann.ToEntity<Annotation>();
            //    note.NoteText = dr["Note"] != DBNull.Value ? dr["Note"].ToString() : "";
            //    //note.StepId = dr["Id"].ToString();
            //    if (dr["OrderId"] != DBNull.Value && int.Parse(dr["OrderId"].ToString()) > 0)
            //        note.ObjectId = new EntityReference(SalesOrder.EntityLogicalName, "wrs_id", int.Parse(dr["OrderId"].ToString()));
            //    if (dr["CreatedOnUtc"] != DBNull.Value)
            //        note.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];
            //    return note;
            //}
            //return null;

            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity ordernote = new Entity(wrs_ordernote.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_ordernote ordernoteObj = ordernote.ToEntity<wrs_ordernote>();
                ordernoteObj.wrs_id = int.Parse(dr["Id"].ToString());
                ordernoteObj.wrs_name = dr["Note"] != DBNull.Value ? dr["Note"].ToString() : "";
                if (dr["OrderId"] != DBNull.Value && int.Parse(dr["OrderId"].ToString()) != 0)
                    ordernoteObj.wrs_orderid = new EntityReference(SalesOrder.EntityLogicalName, "wrs_id", int.Parse(dr["OrderId"].ToString()));

                ordernoteObj.wrs_displaytocustomer = dr["DisplayToCustomer"] != DBNull.Value ? (bool)dr["DisplayToCustomer"] : false;
                if (dr["CreatedOnUtc"] != DBNull.Value)
                    ordernoteObj.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];

                return ordernoteObj;
            }
            return null;
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
                    try
                    {
                        if (e != null)
                            multipleReq.Requests.Add(CreateUpsertRequest(e));
                    }
                    catch (Exception ex)
                    {

                    }

                }
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleReq);
                List<string> existingRecords = new List<string>();

                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    if (e.Fault == null)
                    {
                        string alternateKey = "";
                        alternateKey = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
                        existingRecords.Add(alternateKey);
                        if (mainSuccessLog != string.Empty)
                        {
                            mainSuccessLog = mainSuccessLog + ",";
                        }
                        mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + alternateKey + "')";
                    }

                    else if (e.Fault != null)
                    {
                        if (((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target != null)
                            NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
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
}
