using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WRS.Xrm;

namespace PaymentTrans
{
    class Program
    {
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        public static IOrganizationService CrmService = CrmServiceClient();
        private string NCId;

        static void Main(string[] args)
        {
            string alternateKeyLogicalName = "wrs_id";
            const string PaymentTransStoreProcName = "CrmGetPaymentTransaction";
            const string PaymentTrans = "PaymentTransaction";

            const string PaymentGatewayStoreProcName = "CrmGetPaymentGateway";
            const string PaymentGateway = "PaymentGatewayItem";

            Program p = new Program();

            p.UpsertPaymentTransaction(PaymentTrans, p, PaymentTransStoreProcName, alternateKeyLogicalName);
            p.UpsertPaymentGateway(PaymentGateway, p, PaymentGatewayStoreProcName, alternateKeyLogicalName);
        }

        private void UpsertPaymentTransaction(string paymentTrans, Program p, string storeProc, string alternateKeyLogicalName)
        {

            DataTable PaymentTransTable = RetrieveRecordsFromDB(storeProc);
            if (PaymentTransTable != null && PaymentTransTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_paymenttransactionid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(PaymentTransTable, PreparePaymentTransObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, paymentTrans, wrs_paymenttransaction.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                    }
            }
        }

        private void UpsertPaymentGateway(string paymentGateway, Program p, string storeProc, string alternateKeyLogicalName)
        {

            DataTable PaymentGatewayTable = RetrieveRecordsFromDB(storeProc);
            if (PaymentGatewayTable != null && PaymentGatewayTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_paymentgatewayid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(PaymentGatewayTable, PreparePaymentGatewayObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, paymentGateway, wrs_paymentgateway.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
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
                catch (Exception ex)
                {
                    continue;
                }
            }
            return _lisEntityCollection;
        }


        private Entity PreparePaymentTransObject(DataRow dr)
        {
            if (dr["id"] != DBNull.Value && int.Parse(dr["id"].ToString()) > 0)
            {
                Entity paymentTrans = new Entity(wrs_paymenttransaction.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_paymenttransaction paymentTransObj = paymentTrans.ToEntity<wrs_paymenttransaction>();


                paymentTransObj.wrs_id = int.Parse(dr["Id"].ToString());
                paymentTransObj.wrs_bankauthid = dr["BankAuthId"].ToString();
                //paymentTransObj.wrs_batchno = dr["BatchNo"].ToString();
                paymentTransObj.wrs_cardexpiry = dr["CardExpiry"].ToString();
                paymentTransObj.wrs_cardno = dr["CardNo"].ToString();
                paymentTransObj.wrs_merchantref = dr["MerchantRef"].ToString();
                paymentTransObj.wrs_paymentguid = dr["PaymentGuid"].ToString();
                paymentTransObj.wrs_paymentmid = dr["PaymentMID"].ToString();
                paymentTransObj.wrs_paymentmode = dr["PaymentMode"].ToString();
                paymentTransObj.wrs_tid = dr["TID"].ToString();
                paymentTransObj.wrs_tranresponsecode = dr["TranResponseCode"].ToString();
                paymentTransObj.wrs_transactionmessage = dr["TranMessage"].ToString();
                paymentTransObj.wrs_transref = dr["TranRef"].ToString();
                paymentTransObj.wrs_transtatus = dr["TranStatus"].ToString();

                if (dr["CreatedOnUtc"] != DBNull.Value)
                    paymentTransObj.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];

                if (dr["TranDate"] != DBNull.Value)
                    paymentTransObj.wrs_transdate = (DateTime)dr["TranDate"];
                if (dr["OrderId"] != DBNull.Value && int.Parse(dr["OrderId"].ToString()) > 0)
                {
                    paymentTransObj.wrs_orderid = new EntityReference(SalesOrder.EntityLogicalName, "wrs_id", int.Parse(dr["OrderId"].ToString()));
                }

                if (dr["TranAmount"] != DBNull.Value && decimal.Parse(dr["TranAmount"].ToString()) > 0)
                {
                    paymentTransObj.wrs_transamount = new Money(decimal.Parse(dr["TranAmount"].ToString()));
                }

                switch (int.Parse((dr["PaymentStatus"]).ToString()))
                {
                    case 10:
                        paymentTransObj.wrs_paymentstatus = new OptionSetValue(10);
                        break;

                    case 20:
                        paymentTransObj.wrs_paymentstatus = new OptionSetValue(20);
                        break;

                    case 30:
                        paymentTransObj.wrs_paymentstatus = new OptionSetValue(30);
                        break;

                    case 35:
                        paymentTransObj.wrs_paymentstatus = new OptionSetValue(35);
                        break;

                    case 40:
                        paymentTransObj.wrs_paymentstatus = new OptionSetValue(40);
                        break;

                }

                return paymentTransObj;
            }
            return null;
        }

        private Entity PreparePaymentGatewayObject(DataRow dr)
        {
            if (dr["id"] != DBNull.Value && int.Parse(dr["id"].ToString()) > 0)
            {
                Entity paymentGateway = new Entity(wrs_paymentgateway.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_paymentgateway paymentGatewayObj = paymentGateway.ToEntity<wrs_paymentgateway>();


                paymentGatewayObj.wrs_id = int.Parse(dr["Id"].ToString());
                paymentGatewayObj.wrs_apiendpoint = dr["ApiEndpoint"].ToString();
                paymentGatewayObj.wrs_failedurl = dr["FailedUrl"].ToString();
                paymentGatewayObj.wrs_logo = dr["Logo"].ToString();
                paymentGatewayObj.wrs_name = dr["Name"].ToString();
                paymentGatewayObj.wrs_paymenturl = dr["PaymentUrl"].ToString();
                paymentGatewayObj.wrs_successurl = dr["SuccessUrl"].ToString();
                paymentGatewayObj.wrs_systemname = dr["SystemName"].ToString();


                if (dr["CreatedOnUtc"] != DBNull.Value)
                    paymentGatewayObj.wrs_nccreatedon = (DateTime?)dr["CreatedOnUtc"];

                if (dr["UpdatedOnUtc"] != DBNull.Value)
                    paymentGatewayObj.wrs_ncupdatedon = (DateTime)dr["UpdatedOnUtc"];


                if (dr["PaymentTransactionLimit"] != DBNull.Value && decimal.Parse(dr["PaymentTransactionLimit"].ToString()) > 0)
                {
                    paymentGatewayObj.wrs_paymenttranslimit = new Money(decimal.Parse(dr["PaymentTransactionLimit"].ToString()));
                }

                if (dr["TimeOut"] != DBNull.Value && int.Parse(dr["TimeOut"].ToString()) > 0)
                {
                    paymentGatewayObj.wrs_timeout = int.Parse(dr["TimeOut"].ToString());
                }

                if (dr["CreatedById"] != DBNull.Value && int.Parse(dr["CreatedById"].ToString()) > 0)
                {
                    paymentGatewayObj.wrs_nccreatedbyid = int.Parse(dr["CreatedById"].ToString());
                }

                if (dr["UpdatedById"] != DBNull.Value && int.Parse(dr["UpdatedById"].ToString()) > 0)
                {
                    paymentGatewayObj.wrs_ncupdatedbyid = int.Parse(dr["UpdatedById"].ToString());
                }

                return paymentGatewayObj;
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

        public static IOrganizationService CrmServiceClient()
        {
            CrmServiceClient crmConnD = null;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
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
                    multipleReq.Requests.Add(CreateUpsertRequest(e));
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
                        object t1 = null;
                        //ErrorLog(string.Format(("{0} with NC DB ID {1} is failed for upsert with message {2}"), table, ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString(), e.Fault.Message));
                        NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();

                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
                    }
                }
                if (!isOrder)
                {
                    foreach (string l in existingRecords)
                    {
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
