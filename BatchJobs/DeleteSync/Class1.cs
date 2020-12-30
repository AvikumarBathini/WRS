using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ADIntegration
{
    class Program
    {
        static string connectionString = ConfigurationManager.ConnectionStrings["DBConnectionString"].ToString();
        static string crmconnectionString = ConfigurationManager.ConnectionStrings["CrmConnectionString"].ToString();
        static int batchSize = Int32.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        static string mainSuccessLog;
        static string mainErrorLog;
        static string stateSuccessLog;
        static string stateErrorLog;

        static void Main(string[] args)
        {
            try
            {
                //Read AD accounts and write to DB
                WriteToDB();


                //Get entity collection for upsert request
                List<SetStateRequest> setStateRequestList = new List<SetStateRequest>();
                EntityCollection entityCollectionForUpsert = GetADAccountCollection(setStateRequestList);

                //Check if there are entities for upsert request
                if (entityCollectionForUpsert.Entities.Count > 0 && setStateRequestList.Count > 0)
                {
                    List<EntityCollection> entityCollectionForUpsertBatches = SplitCollectionToBatch(entityCollectionForUpsert);
                    List<List<SetStateRequest>> setStateRequestBatches = SplitSetStateRequestToBatch(setStateRequestList);

                    for (int i = 0; i < entityCollectionForUpsertBatches.Count; i++)
                    {
                        mainSuccessLog = string.Empty;
                        mainErrorLog = string.Empty;

                        stateSuccessLog = string.Empty;
                        stateErrorLog = string.Empty;

                        PushToCRM(entityCollectionForUpsertBatches[i]);
                        UpdateBatchStatus(setStateRequestBatches[i]);

                        WriteLogsToDB(mainSuccessLog);
                        WriteLogsToDB(mainErrorLog);

                        WriteLogsToDB(stateSuccessLog);
                        WriteLogsToDB(stateErrorLog);
                    }
                }
            }
            catch (Exception ex)
            {
                string ErrorLog = "('" + DateTime.Now + "','wrs_staff',null,'Error','" + ex.Message.Replace("'", "''") + "','" + ex.StackTrace.Replace("'", "''") + "','Staff')";
                WriteLogsToDB(ErrorLog);
            }

        }

        private static EntityCollection GetADAccountCollection(List<SetStateRequest> setStateRequestList)
        {
            EntityCollection StaffMembers = new EntityCollection();
            StaffMembers.EntityName = "wrs_staff";

            SqlConnection cnn = new SqlConnection(connectionString);
            SqlCommand command;
            cnn.Open();

            string sql = String.Empty;

            sql = "SELECT [FirstName],[LastName],[PrincipalName],[Status],[IsSynced],[CRMGuid] FROM [WRS_ADIntegration].[dbo].[AD_Account] with (nolock) where IsSynced <> 1";

            command = new SqlCommand(sql, cnn);

            SqlDataReader dr = command.ExecuteReader();

            while (dr.Read())
            {
                Entity Staff = new Entity("wrs_staff");

                Staff.Id = (Guid)dr["crmguid"];
                Staff["wrs_firstname"] = dr["firstname"].ToString();
                Staff["wrs_lastname"] = dr["lastname"].ToString();
                Staff["wrs_email"] = dr["principalname"].ToString();

                StaffMembers.Entities.Add(Staff);

                //create setstaterequest collection
                Entity StaffStatus = new Entity("wrs_staff");
                StaffStatus.Id = (Guid)dr["crmguid"];
                if ((bool)dr["status"])
                {
                    SetStateRequest setStateRequest = new SetStateRequest()
                    {
                        EntityMoniker = new EntityReference
                        {
                            Id = StaffStatus.Id,
                            LogicalName = StaffStatus.LogicalName,
                        },
                        State = new OptionSetValue(0),
                        Status = new OptionSetValue(1)
                    };

                    setStateRequestList.Add(setStateRequest);
                }
                else
                {
                    SetStateRequest setStateRequest = new SetStateRequest()
                    {
                        EntityMoniker = new EntityReference
                        {
                            Id = StaffStatus.Id,
                            LogicalName = StaffStatus.LogicalName,
                        },
                        State = new OptionSetValue(1),
                        Status = new OptionSetValue(2)
                    };

                    setStateRequestList.Add(setStateRequest);
                }

            }

            command.Dispose();
            cnn.Close();

            return StaffMembers;
        }


        private static void WriteToDB()
        {
            string values = string.Empty;

            SqlConnection cnn = new SqlConnection(connectionString);

            using (var context = new PrincipalContext(ContextType.Domain, "ncs.corp.int-ads"))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    int count = 0;
                    var allADs = searcher.FindAll();

                    foreach (var result in allADs)
                    {
                        DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;

                        Boolean adAccountStatus = IsAdAccountActive(de);
                        if (values != string.Empty)
                        {
                            values = values + ",";
                        }

                        string firstname = !string.IsNullOrEmpty(de.Properties["givenName"].Value != null ? de.Properties["givenName"].Value.ToString() : string.Empty) ? de.Properties["givenName"].Value.ToString().Replace("'", "''") : string.Empty;
                        string lastname = !string.IsNullOrEmpty(de.Properties["sn"].Value != null ? de.Properties["sn"].Value.ToString() : string.Empty) ? de.Properties["sn"].Value.ToString().Replace("'", "''") : string.Empty;

                        values = values + "('" + firstname + "','" + lastname + "','" + de.Properties["userPrincipalName"].Value + "','" + adAccountStatus + "','" + new Guid((byte[])de.Properties["objectGuid"].Value).ToString() + "')";

                        count++;

                        if (count % 1000 == 0)
                        {
                            if (values != string.Empty)
                            {
                                string sql = String.Empty;
                                sql = "INSERT INTO [dbo].[AD_Delta_Temp] ([FirstName],[LastName],[PrincipalName],[Status],[CRMGuid]) " +
                                       "VALUES " + values;

                                cnn.Open();
                                SqlCommand command;
                                command = new SqlCommand(sql, cnn);
                                SqlDataReader dr = command.ExecuteReader();
                                command.Dispose();
                                cnn.Close();

                                values = string.Empty;
                            }
                        }

                        //if (count >= 2500)
                        //    break;
                    }

                    if (values != string.Empty)
                    {
                        string sql = String.Empty;
                        sql = "INSERT INTO [dbo].[AD_Delta_Temp] ([FirstName],[LastName],[PrincipalName],[Status],[CRMGuid]) " +
                               "VALUES " + values;

                        SqlCommand command;
                        cnn.Open();
                        command = new SqlCommand(sql, cnn);
                        SqlDataReader dr = command.ExecuteReader();
                        command.Dispose();
                        cnn.Close();
                    }
                }
            }

            cnn.Open();
            SqlCommand com = new SqlCommand("MergeDeltaWithMainAD");
            com.Connection = cnn;
            com.CommandType = CommandType.StoredProcedure;
            com.ExecuteNonQuery();
            com.Dispose();
            cnn.Close();

        }

        private static void PushToCRM(EntityCollection input)
        {
            IOrganizationService _orgService;

            // Connect to the CRM web service using a connection string.
            CrmServiceClient svcClient = new CrmServiceClient(crmconnectionString);

            svcClient.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);

            // Cast the proxy client to the IOrganizationService interface.
            _orgService = (IOrganizationService)svcClient.OrganizationWebProxyClient != null ? (IOrganizationService)svcClient.OrganizationWebProxyClient : (IOrganizationService)svcClient.OrganizationServiceProxy;

            ExecuteMultipleRequest requestWithResults = null;
            try
            {
                #region Execute Multiple with Results
                // Create an ExecuteMultipleRequest object.
                requestWithResults = new ExecuteMultipleRequest()
                {
                    // Assign settings that define execution behavior: continue on error, return responses. 
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    },
                    // Create an empty organization request collection.
                    Requests = new OrganizationRequestCollection()
                };

                // Add a CreateRequest for each entity to the request collection.
                foreach (var entity in input.Entities)
                {
                    UpsertRequest upsertRequest = new UpsertRequest { Target = entity };
                    requestWithResults.Requests.Add(upsertRequest);
                }

                // Execute all the requests in the request collection using a single web method call.
                ExecuteMultipleResponse responseWithResults = (ExecuteMultipleResponse)_orgService.Execute(requestWithResults);

                string upsertGuids = string.Empty;

                foreach (var responseItem in responseWithResults.Responses)
                {
                    int requestIndex = responseItem.RequestIndex;
                    UpsertRequest request = (UpsertRequest)requestWithResults.Requests[requestIndex];

                    // A valid response.
                    if (responseItem.Response != null)
                    {
                        if (upsertGuids != string.Empty)
                        {
                            upsertGuids = upsertGuids + ",";
                        }
                        upsertGuids = upsertGuids + "'" + ((EntityReference)responseItem.Response.Results["Target"]).Id.ToString() + "'";

                        if (mainSuccessLog != string.Empty)
                        {
                            mainSuccessLog = mainSuccessLog + ",";
                        }

                        mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','wrs_staff','" + ((EntityReference)responseItem.Response.Results["Target"]).Id.ToString() + "','Success','','','Staff')";
                    }

                    // An error has occurred.
                    else if (responseItem.Fault != null)
                    {
                        if (mainErrorLog != string.Empty)
                        {
                            mainErrorLog = mainErrorLog + ",";
                        }

                        mainErrorLog = mainErrorLog + "('" + DateTime.Now + "','wrs_staff','" + request.Target.Id.ToString() + "','Error','" + responseItem.Fault.Message.Replace("'", "''") + "','" + responseItem.Fault.TraceText + "','Staff')";
                    }

                }

                if (upsertGuids != string.Empty)
                {
                    UpdateDBWithSyncADs(upsertGuids);
                }
                #endregion Execute Multiple with Results

            }
            catch (FaultException<OrganizationServiceFault> fault)
            {
                throw fault;
            }
        }

        private static void UpdateBatchStatus(List<SetStateRequest> requestsForSetState)
        {
            IOrganizationService _orgService;

            // Connect to the CRM web service using a connection string.
            CrmServiceClient svcClient = new CrmServiceClient(crmconnectionString);

            svcClient.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);

            // Cast the proxy client to the IOrganizationService interface.
            _orgService = (IOrganizationService)svcClient.OrganizationWebProxyClient != null ? (IOrganizationService)svcClient.OrganizationWebProxyClient : (IOrganizationService)svcClient.OrganizationServiceProxy;

            // Create an ExecuteMultipleRequest object
            ExecuteMultipleRequest requestWithResults = new ExecuteMultipleRequest()
            {
                // Assign settings that define execution behavior: continue on error, return responses
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                // Create an empty organization request collection
                Requests = new OrganizationRequestCollection()
            };

            requestWithResults.Requests.AddRange(requestsForSetState);

            // Execute all the requests in the request collection using a single web method call
            ExecuteMultipleResponse responseWithResults = (ExecuteMultipleResponse)_orgService.Execute(requestWithResults);

            foreach (var responseItem in responseWithResults.Responses)
            {
                int requestIndex = responseItem.RequestIndex;
                SetStateRequest setStateRequest = (SetStateRequest)requestWithResults.Requests[requestIndex];

                // A valid response.
                if (responseItem.Response != null)
                {
                    if (stateSuccessLog != string.Empty)
                    {
                        stateSuccessLog = stateSuccessLog + ",";
                    }

                    stateSuccessLog = stateSuccessLog + "('" + DateTime.Now + "','wrs_staff','" + setStateRequest.EntityMoniker.Id.ToString() + "','Success - SetState','','','Staff')";
                }

                // An error has occurred.
                else if (responseItem.Fault != null)
                {
                    if (stateErrorLog != string.Empty)
                    {
                        stateErrorLog = stateErrorLog + ",";
                    }

                    stateErrorLog = stateErrorLog + "('" + DateTime.Now + "','wrs_staff','" + setStateRequest.EntityMoniker.Id.ToString() + "','Error - SetState','" + responseItem.Fault.Message.Replace("'", "''") + "','" + responseItem.Fault.TraceText + "','Staff')";
                }

            }
        }

        private static void WriteLogsToDB(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = String.Empty;
                sql = "INSERT INTO [dbo].[Log] ([TimeStamp],[EntityName],[RecordGuid],[Status],[Exception],[StackTrace],[EntityDisplayName]) " +
                       "VALUES " + insertQuery;

                SqlConnection cnn = new SqlConnection(connectionString);
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }

        private static void UpdateDBWithSyncADs(string guids)
        {
            if (guids != string.Empty)
            {
                string sql = String.Empty;
                sql = "UPDATE [WRS_ADIntegration].[dbo].[AD_Account] SET IsSynced =1, LastSyncDate = GetDate() WHERE CRMGuid IN (" + guids + ")";

                SqlConnection cnn = new SqlConnection(connectionString);
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }

        private static List<EntityCollection> SplitCollectionToBatch(EntityCollection entityCollectionFullList)
        {
            List<EntityCollection> entityCollectionList = new List<EntityCollection>();
            EntityCollection tempEntityColection = new EntityCollection();
            int count = 0;

            foreach (Entity entity in entityCollectionFullList.Entities)
            {
                if (count++ == batchSize)
                {
                    entityCollectionList.Add(tempEntityColection);
                    tempEntityColection = new EntityCollection();
                    count = 1;
                }
                tempEntityColection.Entities.Add(entity);
            }

            entityCollectionList.Add(tempEntityColection);
            return entityCollectionList;
        }

        private static List<List<SetStateRequest>> SplitSetStateRequestToBatch(List<SetStateRequest> setStateRequestFullList)
        {
            List<List<SetStateRequest>> setStateRequestList = new List<List<SetStateRequest>>();
            List<SetStateRequest> tempSetStateRequest = new List<SetStateRequest>();
            int count = 0;

            foreach (SetStateRequest setStateRequestItem in setStateRequestFullList)
            {
                if (count++ == batchSize)
                {
                    setStateRequestList.Add(tempSetStateRequest);
                    tempSetStateRequest = new List<SetStateRequest>();
                    count = 1;
                }
                tempSetStateRequest.Add(setStateRequestItem);
            }

            setStateRequestList.Add(tempSetStateRequest);
            return setStateRequestList;
        }

        private static bool IsAdAccountActive(DirectoryEntry de)
        {
            if (de.NativeGuid == null) return false;

            int flags = (int)de.Properties["userAccountControl"].Value;

            return !Convert.ToBoolean(flags & 0x0002);
        }
    }


}
