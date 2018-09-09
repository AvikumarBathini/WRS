using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
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
        public static Dictionary<string, string> CategoryCodes = GetCategoryCodes();
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        static string memberListURL = ConfigurationManager.AppSettings["GetMemberListURL"];
        static string apikey = ConfigurationManager.AppSettings["MailChimpApikey"];
        static string listId = ConfigurationManager.AppSettings["MailChimpListId"];
        static string tableName = ConfigurationManager.AppSettings["LogTableName"];
        static bool IsAllRecord = Convert.ToBoolean(ConfigurationManager.AppSettings["ProcessAll"]);
        static int requestRecordCount = Convert.ToInt32(ConfigurationManager.AppSettings["requetCount"]);
        static int ModifiedInLastMints = Convert.ToInt32(ConfigurationManager.AppSettings["ModifiedInLastMints"]);
        static string[] DefaultMCMergeFields = new string[] { "FNAME", "LNAME", "ADDRESS", "PHONE" };
        static void Main(string[] args)
        {
            try
            {
                List<members> MemberList = new List<MailChimpBatch.members>();
                var keyArray = apikey.Split('-');
                var requestUrl = string.Format(memberListURL, keyArray[1], listId, keyArray[0]);
                var sinceLastChangeDate = DateTime.UtcNow.AddMinutes(-ModifiedInLastMints).ToString("yyyy-MM-dd HH:mm:ss");
                //requestUrl += "&since_last_changed=" + sinceLastChangeDate;
                //}
                var membersJsonData = HttpRequestUrl.GetRequest(requestUrl);
                JObject results = (JObject)JsonConvert.DeserializeObject(membersJsonData);
                var amountCount = Convert.ToInt32(((Newtonsoft.Json.Linq.JValue)(results["total_items"])).Value);
                if (amountCount != 0)
                {
                    var requestCount = Math.Ceiling(amountCount / new decimal(requestRecordCount));
                    for (int i = 0; i < requestCount; i++)
                    {
                        membersJsonData = HttpRequestUrl.GetRequest(requestUrl + "&offset=" + i * requestRecordCount + "&count=" + requestRecordCount);

                        JObject result = (JObject)JsonConvert.DeserializeObject(membersJsonData);
                        MemberList.AddRange(DeserializeObject(result));
                    }
                }
                else
                    MemberList.AddRange(DeserializeObject(results));

                if (MemberList.Count > 0)
                {
                    ExecuteMultipleRequest multipleRequest = new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings
                        { ContinueOnError = true, ReturnResponses = true },
                        Requests = new OrganizationRequestCollection()
                    };
                    var initCount = 0;
                    var processingTime = 1;
                    foreach (var member in MemberList)
                    {
                        QueryExpression qe = new QueryExpression("wrs_subscription");
                        qe.Criteria.Conditions.Add(new ConditionExpression("wrs_email", ConditionOperator.Equal, member.Email));
                        qe.Criteria.Conditions.Add(new ConditionExpression("wrs_categorycode", ConditionOperator.NotNull));
                        qe.ColumnSet = new ColumnSet(new string[] { "wrs_categorycode", "wrs_email" });
                        EntityCollection ContSubscriptioncoll = CrmService.RetrieveMultiple(qe);

                        if (ContSubscriptioncoll.Entities.Count > 0)
                        {
                            Entity subscriptionContactEntity = new Entity("contact", "wrs_id", ContSubscriptioncoll.Entities[0].Attributes["wrs_id"]);

                            subscriptionContactEntity["wrs_subscriptionstatus"] = member.status == "subscribed" ? true : false;
                            subscriptionContactEntity["emailaddress1"] = member.email_address;
                            subscriptionContactEntity.Attributes["lastname"] = member.merge_fields.FNAME;
                            subscriptionContactEntity.Attributes["firstname"] = member.merge_fields.LNAME;
                            subscriptionContactEntity["wrs_subscriptionsourcefrom"] = new OptionSetValue((int)SubscriptionFrom.MailChimp);
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
                            ExecuteMultipleResponse MultipleResponse = (ExecuteMultipleResponse)CrmService.Execute(multipleRequest);
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

        public static List<members> DeserializeObject(JObject result)
        {
            List<members> mems = new List<MailChimpBatch.members>();
            #region DeSerialize
            foreach (var r in result)
            {
                if (r.Key == "members")
                {
                    members mem = null;
                    foreach (var t in r.Value)
                    {
                        mem = new members();
                        mem.Subscriptions = new Dictionary<string, string>();
                        foreach (var w in t)
                        {
                            string name = ((Newtonsoft.Json.Linq.JProperty)w).Name;

                            switch (name)
                            {
                                case "email_address":
                                    mem.Email = ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)w).Value).Value.ToString();
                                    break;
                                case "merge_fields":
                                    var values = ((Newtonsoft.Json.Linq.JProperty)w).Value;
                                    foreach (var val in values)
                                    {
                                        string _name = ((Newtonsoft.Json.Linq.JProperty)val).Name.ToString();
                                        string _value = ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)val).Value).Value.ToString();
                                        mem.Subscriptions.Add(_name, _value);
                                    }
                                    break;

                                case "status":
                                    mem.Status = ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)w).Value).Value.ToString();
                                    break;

                                case "last_changed":
                                    mem.last_changed = ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)w).Value).Value.ToString();
                                    break;

                                default: break;
                            }
                        }
                        mems.Add(mem);
                    }
                }
            }
            #endregion
            return mems;
        }

        public static Dictionary<string, string> GetCategoryCodes()
        {
            try
            {
                Dictionary<string, string> Categories = new Dictionary<string, string>();
                QueryExpression exp = new QueryExpression("wrs_subscriptioncategory")
                {
                    ColumnSet = new ColumnSet("wrs_categorycode", "wrs_mailchimpcolumntag"),
                    Criteria = new FilterExpression()
                };
                EntityCollection coll = CrmService.RetrieveMultiple(exp);
                if (coll != null && coll.Entities.Count > 0)
                {
                    foreach (Entity category in coll.Entities)
                    {
                        string categoryCode = category.Contains("wrs_categorycode") ? category.GetAttributeValue<string>("wrs_categorycode").ToLower() : string.Empty;
                        string mailchimpcolumntag = category.Contains("wrs_mailchimpcolumntag") ? category.GetAttributeValue<string>("wrs_mailchimpcolumntag") : string.Empty;
                        Categories.Add(categoryCode, mailchimpcolumntag);
                    }
                }
                return Categories;
            }
            catch (Exception ex)
            {
                throw;
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

    public class members
    {
        /// <summary>
        /// 
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string last_changed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Subscriptions { get; set; }
    }

    public enum SubscriptionFrom
    {
        MailChimp = 167320000,
        CRM = 167320001,
        NC = 167320002
    }
}
