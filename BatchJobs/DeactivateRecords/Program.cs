using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using WRS.Xrm;

namespace DeactivateRecords
{
    class Program
    {
        public static IOrganizationService CrmService = CrmServiceClient();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);

        private string NCId;
        private static string tableName;
        private string logicalName;
        static string alternateKeyName = "wrs_id";
        static void Main(string[] args)
        {
            string deleteStoreProcName = "CrmGetRecordsToBeDeleted";
            DataTable deactivateRecordsTable = RetrieveRecordsFromDB(deleteStoreProcName);
            Program p = new Program();

            try
            {

                int counter = 0;

                int totalCount = deactivateRecordsTable.Rows.Count;

                EntityCollection ec;
                if (totalCount > 0 && totalCount < batchSize)
                {
                    ec = new EntityCollection();

                    for (int i = 0; i < totalCount; i++)
                    {
                        DataRow dr = deactivateRecordsTable.Rows[i];
                        Entity e = new Entity();
                        if (dr["EntityId"] != DBNull.Value && int.Parse(dr["EntityId"].ToString()) > 0)
                        {
                            int entityId = int.Parse(dr["EntityId"].ToString());
                            string entityName = dr["EntityName"].ToString();
                            e = PrepareObjectToDeactivate(entityName, entityId);

                            if (e != null)
                                ec.Entities.Add(e);
                        }
                    }
                    p.CrmExecuteMultiple(ec, p, alternateKeyName, tableName);

                }
                else
                {
                    while (totalCount > batchSize)
                    {
                        ec = new EntityCollection();
                        for (int i = counter; i < batchSize + counter; i++)
                        {

                            DataRow dr = deactivateRecordsTable.Rows[i];
                            Entity e = new Entity();
                            if (dr["EntityId"] != DBNull.Value && int.Parse(dr["EntityId"].ToString()) > 0)
                            {
                                int entityId = int.Parse(dr["EntityId"].ToString());
                                string entityName = dr["EntityName"].ToString();
                                e = PrepareObjectToDeactivate(entityName, entityId);

                                if (e != null)
                                    ec.Entities.Add(e);
                            }

                        }
                        p.CrmExecuteMultiple(ec, p, alternateKeyName, tableName);

                        counter += batchSize;
                        totalCount -= batchSize;

                    }

                    if (totalCount < batchSize && totalCount > 0)
                    {
                        ec = new EntityCollection();

                        for (int i = counter; i < deactivateRecordsTable.Rows.Count; i++)
                        {
                            DataRow dr = deactivateRecordsTable.Rows[i];
                            Entity e = new Entity();
                            if (dr["EntityId"] != DBNull.Value && int.Parse(dr["EntityId"].ToString()) > 0)
                            {
                                int entityId = int.Parse(dr["EntityId"].ToString());
                                string entityName = dr["EntityName"].ToString();
                                e = PrepareObjectToDeactivate(entityName, entityId);

                                if (e != null)
                                    ec.Entities.Add(e);
                            }
                        }
                        p.CrmExecuteMultiple(ec, p, alternateKeyName, tableName);

                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public static Entity PrepareEntityObject(string entityLogicalName, int id)
        {
            Entity e = new Entity(entityLogicalName, "wrs_id", id);
            e.Attributes["wrs_id"] = id;
            return e;
        }

        public static Entity PrepareEntityObject(string entityLogicalName, string alternateKey, int id)
        {
            Entity e = new Entity(entityLogicalName, alternateKey, id);
            e.Attributes[alternateKey] = id;
            return e;
        }

        public static DataTable RetrieveRecordsFromDB(string storeProc)
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

        public void CrmExecuteMultiple(EntityCollection ecoll, Program p, string alternate, string table)
        {
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
                    multipleReq.Requests.Add(CreateDeactivateRequest(e));
                }
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleReq);
                List<int> existingRecords = new List<int>();

                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    if (e.Fault == null)
                    {
                        //Guid ID = new Guid(e.Response.Results["id"].ToString());
                        int alternateKey;
                        string alternateKeyName = alternate;
                        logicalName = ((UpdateRequest)(multipleReq.Requests[e.RequestIndex])).Target.LogicalName;
                        if (logicalName != TransactionCurrency.EntityLogicalName)
                            alternateKey = (int)((UpdateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternateKeyName];
                        else
                        {
                            alternateKeyName = "name";
                            alternateKey = int.Parse(((UpdateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternateKeyName].ToString());
                        }
                        if (alternateKey != 0)
                        {
                            //ErrorLog(string.Format("{0} with Guid {1} is created for NC DB ID {2}", table, ID, alternateKey));
                            existingRecords.Add(alternateKey);
                        }
                    }

                    else if (e.Fault != null)
                    {
                        //ErrorLog(string.Format(("{0} with NC DB ID {1} is failed for upsert with message {2}"), table, ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString(), e.Fault.Message));
                        NCId = ((UpdateRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternateKeyName].ToString();
                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + logicalName + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
                    }
                }
                foreach (int l in existingRecords)
                {
                    p.UpdateSyncStatus(logicalName, l);
                    //ErrorLog(string.Format("Sync Status updated in NC DB for {0} with ID - {1}", table, l));
                }
            }
            catch (Exception e)
            {
                string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + logicalName + "','" + NCId + "'";
                WriteLogsToDB(errorLog);
            }
        }

        private UpdateRequest CreateDeactivateRequest(Entity e)
        {
            UpdateRequest upd = new UpdateRequest();
            e.Attributes["statecode"] = new OptionSetValue(1);

            upd.Target = e;

            return upd;
        }

        public int RetreveAlternateKey(Guid accountID, string entityLogicalName, string alternate)
        {
            Entity e = CrmService.Retrieve(entityLogicalName, accountID, new ColumnSet(new string[] { alternate }));
            if (e != null && e.Attributes[alternate] != null)
                return e.GetAttributeValue<int>(alternate);
            return 0;
        }

        public void UpdateSyncStatus(string entityName, int l)
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

        private static Entity PrepareObjectToDeactivate(string entityName, int entityId)
        {
            Entity e = new Entity();
            switch (entityName)
            {
                case "Currency":
                    e = PrepareEntityObject(TransactionCurrency.EntityLogicalName, "name", entityId);
                    break;
                case "Country":
                    e = PrepareEntityObject(wrs_country.EntityLogicalName, entityId);
                    break;
                case "Customer":
                    e = PrepareEntityObject(Contact.EntityLogicalName, entityId);
                    e.Attributes["wrs_sourcefrom"] = "NC";
                    break;
                case "Discount":
                    e = PrepareEntityObject(wrs_discount.EntityLogicalName, entityId);
                    break;
                case "Discount_AppliedToProducts":
                    e = PrepareEntityObject(wrs_discountproducts.EntityLogicalName, entityId);
                    break;
                case "DiscountUsageHistory":
                    e = PrepareEntityObject(wrs_discountusagehistory.EntityLogicalName, entityId);
                    break;
                case "Order":
                    e = PrepareEntityObject(SalesOrder.EntityLogicalName, entityId);
                    break;
                case "OrderItem":
                    e = PrepareEntityObject(SalesOrderDetail.EntityLogicalName, entityId);
                    break;
                case "StateProvince":
                    e = PrepareEntityObject(wrs_stateregion.EntityLogicalName, entityId);
                    break;
                case "Store":
                    e = PrepareEntityObject(wrs_store.EntityLogicalName, entityId);
                    break;
                case "BlackoutCalendar":
                    e = PrepareEntityObject(wrs_blackoutcalendar.EntityLogicalName, entityId);
                    e.Attributes["wrs_sourcefrom"] = "NC";
                    break;
                case "BlackoutCalendarDetail":
                    e = PrepareEntityObject(wrs_blackoutcalendardetail.EntityLogicalName, entityId);
                    e.Attributes["wrs_sourcefrom"] = "NC";
                    break;
                case "BlackoutCalendarProductMapping":
                    e = PrepareEntityObject(wrs_blackoutcalendarproduct.EntityLogicalName, entityId);
                    e.Attributes["wrs_sourcefrom"] = "NC";
                    break;
                case "MembershipBooking":
                    e = PrepareEntityObject(wrs_membershipbooking.EntityLogicalName, entityId);
                    break;
                case "Passes":
                    e = PrepareEntityObject(wrs_membership.EntityLogicalName, entityId);

                    break;
                case "RMEventStock":
                    e = PrepareEntityObject(wrs_resourcemanagement.EntityLogicalName, entityId);
                    break;
                case "OrderNote":
                    e = PrepareEntityObject(wrs_ordernote.EntityLogicalName, entityId);
                    break;
            }
            return e;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IOrganizationService CrmServiceClient()
        {
            CrmServiceClient crmConnD = null;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }
    }

    public sealed class ManagedTokenOrganizationServiceProxy : OrganizationServiceProxy
    {
        private AutoRefreshSecurityToken<OrganizationServiceProxy, IOrganizationService> _proxyManager;

        public ManagedTokenOrganizationServiceProxy(Uri serviceUri, ClientCredentials userCredentials)
            : base(serviceUri, null, userCredentials, null)
        {
            this._proxyManager = new AutoRefreshSecurityToken<OrganizationServiceProxy, IOrganizationService>(this);
        }

        public ManagedTokenOrganizationServiceProxy(IServiceManagement<IOrganizationService> serviceManagement,
            SecurityTokenResponse securityTokenRes)
            : base(serviceManagement, securityTokenRes)
        {
            this._proxyManager = new AutoRefreshSecurityToken<OrganizationServiceProxy, IOrganizationService>(this);
        }

        public ManagedTokenOrganizationServiceProxy(IServiceManagement<IOrganizationService> serviceManagement,
            ClientCredentials userCredentials)
            : base(serviceManagement, userCredentials)
        {
            this._proxyManager = new AutoRefreshSecurityToken<OrganizationServiceProxy, IOrganizationService>(this);
        }

        protected override void AuthenticateCore()
        {
            this._proxyManager.PrepareCredentials();
            base.AuthenticateCore();
        }

        protected override void ValidateAuthentication()
        {
            this._proxyManager.RenewTokenIfRequired();
            base.ValidateAuthentication();
        }
    }

    ///<summary>
    /// Class that wraps acquiring the security token for a service
    /// </summary>

    public sealed class AutoRefreshSecurityToken<TProxy, TService>
        where TProxy : ServiceProxy<TService>
        where TService : class
    {
        private TProxy _proxy;

        ///<summary>
        /// Instantiates an instance of the proxy class
        /// </summary>

        /// <param name="proxy">Proxy that will be used to authenticate the user</param>
        public AutoRefreshSecurityToken(TProxy proxy)
        {
            if (null == proxy)
            {
                throw new ArgumentNullException("proxy");
            }

            this._proxy = proxy;
        }

        ///<summary>
        /// Prepares authentication before authenticated
        /// </summary>

        public void PrepareCredentials()
        {
            if (null == this._proxy.ClientCredentials)
            {
                return;
            }

            switch (this._proxy.ServiceConfiguration.AuthenticationType)
            {
                case AuthenticationProviderType.ActiveDirectory:
                case AuthenticationProviderType.OnlineFederation:
                    this._proxy.ClientCredentials.UserName.UserName = null;
                    this._proxy.ClientCredentials.UserName.Password = null;
                    break;
                case AuthenticationProviderType.Federation:
                case AuthenticationProviderType.LiveId:
                    this._proxy.ClientCredentials.Windows.ClientCredential = null;
                    break;
                default:
                    return;
            }
        }

        ///<summary>
        /// Renews the token (if it is near expiration or has expired)
        /// </summary>

        public void RenewTokenIfRequired()
        {
            if (null != this._proxy.SecurityTokenResponse &&
            DateTime.UtcNow.AddMinutes(15) >= this._proxy.SecurityTokenResponse.Response.Lifetime.Expires)
            {
                try
                {
                    this._proxy.Authenticate();
                }
                catch (CommunicationException)
                {
                    if (null == this._proxy.SecurityTokenResponse ||
                        DateTime.UtcNow >= this._proxy.SecurityTokenResponse.Response.Lifetime.Expires)
                    {
                        throw;
                    }

                    // Ignore the exception 
                }
            }
        }
    }

    public class XrmConnectionProvider
    {
        private static IOrganizationService instance;
        private static object _lockObject = new object();


        private XrmConnectionProvider() { }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IOrganizationService serviceClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            CrmServiceClient crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }
    }
}
