using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using WRSDataMigrationInt.Infrastructure;
using WRSDataMigrationInt.Infrastructure.Logger;
using WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling;

namespace MailChimpBatch
{
    /// <summary>
    /// this bactjob running by daily. it request memebers data from MailChimp and to update record to crm.
    /// </summary>
    class Program
    {

        public static IOrganizationService CrmService = serviceClient();
        static void Main(string[] args)
        {
            try
            {
                var ModifiedInLastMints = ConfigHelper.GetSysParam<int>("ModifiedInLastMints");
                var memberListURL = ConfigHelper.GetSysParam<string>("GetMemberListURL");
                var apikey = ConfigHelper.GetSysParam<string>("Apikey");
                var listId = ConfigHelper.GetSysParam<string>("ListId");
                var connectionStr = ConfigurationManager.ConnectionStrings["Xrm"].ToString();
                var tableName = ConfigHelper.GetSysParam<string>("LogTableName");
                var keyArray = apikey.Split('-');
                var requestUrl = string.Format(memberListURL, keyArray[1], listId, keyArray[0]);
                var IsAllRecord = ConfigHelper.GetSysParam<bool>("ProcessAll");
                var requestRecordCount = ConfigHelper.GetSysParam<int>("requestCount");
                var memberlist = new List<MembersItem>();
                //if (!IsAllRecord)
                //{
                //    var hours = ConfigHelper.GetSysParam<int>("InAdvanceHours");
                var sinceLastChangeDate = DateTime.UtcNow.AddMinutes(-ModifiedInLastMints).ToString("yyyy-MM-dd HH:mm:ss");
                requestUrl += "&since_last_changed=" + sinceLastChangeDate;
                //}
                var membersJsonData = HttpRequestUrl.GetRequest(requestUrl);
                var members = JsonConvert.DeserializeObject<Root>(membersJsonData);
                var amountCount = members.total_items;
                if (amountCount != 0)
                {
                    var requestCount = Math.Ceiling(amountCount / new decimal(requestRecordCount));
                    for (int i = 0; i < requestCount; i++)
                    {
                        membersJsonData = HttpRequestUrl.GetRequest(requestUrl + "&offset=" + i * requestRecordCount + "&count=" + requestRecordCount);
                        members = JsonConvert.DeserializeObject<Root>(membersJsonData);
                        memberlist.AddRange(members.members);
                    }
                }

                if (memberlist.Count > 0)
                {
                    //var query = new QueryExpression("product")
                    //{
                    //    ColumnSet = new ColumnSet(true)
                    //};
                    //var result = service.RetrieveMultiple(query);
                    ExecuteMultipleRequest multipleRequest = new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings
                        { ContinueOnError = true, ReturnResponses = true },
                        Requests = new OrganizationRequestCollection()
                    };
                    var initCount = 0;
                    var processingTime = 1;
                    foreach (var member in memberlist)
                    {
                        EntityCollection Contcoll = new EntityCollection();
                        Entity contact = new Entity("contact");
                        QueryExpression qe = new QueryExpression("contact");
                        qe.Criteria.Conditions.Add(new ConditionExpression("emailaddress1", ConditionOperator.Equal, member.email_address));
                        qe.Criteria.Conditions.Add(new ConditionExpression("wrs_accountnumber", ConditionOperator.NotNull));
                        qe.Criteria.Conditions.Add(new ConditionExpression("wrs_id", ConditionOperator.NotNull));
                        qe.ColumnSet = new ColumnSet(new string[] { "wrs_id", "emailaddress1" });
                        Contcoll = service.RetrieveMultiple(qe);
                        if (Contcoll.Entities.Count > 0)
                        {
                            Entity subscriptionContactEntity = new Entity("contact", "wrs_id", Contcoll.Entities[0].Attributes["wrs_id"]);

                            subscriptionContactEntity["wrs_subscriptionstatus"] = (member.status == "subscribed" ? true : false);
                            subscriptionContactEntity["emailaddress1"] = member.email_address;
                            subscriptionContactEntity.Attributes["lastname"] = member.merge_fields.FNAME;
                            subscriptionContactEntity.Attributes["firstname"] = member.merge_fields.LNAME;
                            //subscriptionContactEntity["wrs_subscriptionsourcefrom"] = new OptionSetValue((int)SubscriptionFrom.MailChimp);
                            UpsertRequest request = new UpsertRequest
                            {
                                Target = subscriptionContactEntity
                            };

                            multipleRequest.Requests.Add(request);
                        }
                        else
                        {
                            Entity contactToBeCreated = new Entity("contact");
                            contactToBeCreated.Attributes["lastname"] = member.merge_fields.FNAME;
                            contactToBeCreated.Attributes["firstname"] = member.merge_fields.LNAME;
                            contactToBeCreated.Attributes["emailaddress1"] = member.email_address;
                            contactToBeCreated.Attributes["wrs_subscriptionsourcefrom"] = new OptionSetValue((int)SubscriptionFrom.MailChimp);
                            contactToBeCreated.Attributes["wrs_subscriptionstatus"] = member.status == "subscribed" ? true : false;
                            CreateRequest request = new CreateRequest
                            {
                                Target = contactToBeCreated
                            };

                            multipleRequest.Requests.Add(request);
                        }
                        initCount++;
                        if ((memberlist.Count < 999 && memberlist.Count == initCount) || initCount >= 999 * processingTime
                            || ((memberlist.Count - initCount) < 999 && memberlist.Count == initCount))
                        {
                            ExecuteMultipleResponse MultipleResponse = (ExecuteMultipleResponse)service.Execute(multipleRequest);
                            foreach (ExecuteMultipleResponseItem response in MultipleResponse.Responses)
                            {
                                if (response.Fault != null)
                                {
                                    if (response.Response != null)
                                    {
                                        if (response.Response.Results["RecordCreated"] != null && (bool)response.Response.Results["RecordCreated"].Equals(false))
                                        {
                                            string email = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["emailaddress1"].ToString();
                                            string fName = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["firstname"].ToString();
                                            string lName = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["lastname"].ToString();
                                            string name = fName + " " + lName;
                                            string subscriptionStatus = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["wrs_subscriptionstatus"].ToString();
                                            var logInfo = "email address:" + email + " status: " + subscriptionStatus + " name: " + name;
                                            if (response.Response != null)
                                            {
                                                DBDataAccess.InsertLog("information", "upsert record success:" + logInfo, email, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                            }
                                            if (response.Fault != null)
                                            {

                                                DBDataAccess.InsertLog("exception", "fail upsert info:" + logInfo + " error message:" + response.Fault.Message.Replace("'", ""),
                                                    email.Replace("'", ""), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                            }
                                        }
                                        else if (response.Response.Results["RecordCreated"] != null && (bool)response.Response.Results["RecordCreated"].Equals(true))
                                        {
                                            string email = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["emailaddress1"].ToString();
                                            string fName = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["firstname"].ToString();
                                            string lName = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["lastname"].ToString();
                                            string name = fName + " " + lName;
                                            string subscriptionStatus = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["wrs_subscriptionstatus"].ToString();
                                            var logInfo = "email address:" + email + " status: " + subscriptionStatus + " name: " + name;
                                            if (response.Response != null)
                                            {
                                                DBDataAccess.InsertLog("information", "upsert record success:" + logInfo, email, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                            }
                                            if (response.Fault != null)
                                            {

                                                DBDataAccess.InsertLog("exception", "fail upsert info:" + logInfo + " error message:" + response.Fault.Message.Replace("'", ""),
                                                    email.Replace("'", ""), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                            }
                                        }
                                    }
                                }
                                //string email = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["wrs_email"].ToString();
                                //string name = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["wrs_name"].ToString();
                                //string subscriptionStatus = ((Entity)(((multipleRequest.Requests[response.RequestIndex]).Parameters)["Target"])).Attributes["wrs_subscriptionstatus"].ToString();
                                //var logInfo = "email address:" + email + " status: " + subscriptionStatus + " name: " + name;
                                //if (response.Response != null)
                                //{
                                //    DBDataAccess.InsertLog("information", "upsert record success:" + logInfo, email, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                //}
                                //if (response.Fault != null)
                                //{

                                //    DBDataAccess.InsertLog("exception", "fail upsert info:" + logInfo + " error message:" + response.Fault.Message.Replace("'", ""),
                                //        email.Replace("'", ""), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                //}
                            }
                            processingTime++;
                            multipleRequest.Requests.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                throw ex;
            }
        }

        public static IOrganizationService serviceClient()
        {
            CrmServiceClient crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

    }


    public enum SubscriptionFrom
    {
        MailChimp = 167320000,
        CRM = 167320001,
        NC = 167320002
    }
}
