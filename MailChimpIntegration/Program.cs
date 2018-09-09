using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WRSDataMigrationInt.Infrastructure;

namespace MailChimpIntegration
{
    class Program
    {
        public static IOrganizationService CrmService = serviceClient();

        static void Main(string[] args)
        {
            try
            {
                var connectionStr = ConfigurationManager.ConnectionStrings["Xrm"].ToString();
                int ModifiedInLastMints = Convert.ToInt32(ConfigurationManager.ConnectionStrings["ModifiedInLastMints"].ToString());
                var GetMemberListURL = ConfigurationManager.ConnectionStrings["GetMemberListURL"].ToString();
                var MailChimpApikey = ConfigurationManager.ConnectionStrings["MailChimpApikey"].ToString();
                var MailChimpListId = ConfigurationManager.ConnectionStrings["MailChimpListId"].ToString();
                int requestRecordCount = Convert.ToInt32(ConfigurationManager.ConnectionStrings["requestCount"].ToString());
                var keyArray = MailChimpApikey.Split('-');
                var requestUrl = string.Format(GetMemberListURL, keyArray[1], MailChimpListId, keyArray[0]);
                //var IsAllRecord = ConfigHelper.GetSysParam<bool>("ProcessAll");
                //var requestRecordCount = ConfigHelper.GetSysParam<int>("requestCount");
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
                    Dictionary<Guid, bool> ContactGuid = new Dictionary<Guid, bool>();
                    foreach (var member in memberlist)
                    {
                        EntityCollection NewsLetterSub = new EntityCollection();
                        QueryExpression qe = new QueryExpression("wrs_subscription");
                        qe.Criteria.Conditions.Add(new ConditionExpression("wrs_email", ConditionOperator.Equal, member.email_address));
                        qe.ColumnSet = new ColumnSet(new string[] { "wrs_contactid", "wrs_email", "wrs_subscriptionstatus" });
                        NewsLetterSub = CrmService.RetrieveMultiple(qe);
                        if (NewsLetterSub.Entities.Count > 0)
                        {
                            foreach (Entity ns in NewsLetterSub.Entities)
                            {
                                Guid _cGuid = ns.Contains("wrs_contactid") ? ns.GetAttributeValue<EntityReference>("wrs_contactid").Id : Guid.Empty;
                                bool _subscribed = (member.status == "subscribed" ? true : false);
                                if (!ContactGuid.ContainsKey(_cGuid))
                                    ContactGuid.Add(_cGuid, _subscribed);
                                Entity _newsLetter = new Entity("wrs_subscription", ns.Id);
                                _newsLetter["wrs_subscriptionstatus"] = _subscribed;
                                CrmService.Update(_newsLetter);
                            }
                        }
                        else
                        {
                            Entity contactToBeCreated = new Entity("contact");
                            contactToBeCreated.Attributes["lastname"] = member.merge_fields.FNAME;
                            contactToBeCreated.Attributes["firstname"] = member.merge_fields.LNAME;
                            contactToBeCreated.Attributes["emailaddress1"] = member.email_address;
                            contactToBeCreated.Attributes["wrs_sourcefrom"] = "MailChimp";
                            contactToBeCreated.Attributes["wrs_subscriptionstatus"] = member.status == "subscribed" ? true : false;
                            CrmService.Create(contactToBeCreated);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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

    public class Merge_fields
    {
        /// <summary>
        /// 
        /// </summary>
        public string FNAME { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string LNAME { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string BIRTHDAY { get; set; }
    }

    public class Stats
    {
        /// <summary>
        /// 
        /// </summary>
        public int avg_open_rate { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int avg_click_rate { get; set; }
    }

    public class Location
    {
        /// <summary>
        /// 
        /// </summary>
        public int latitude { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int longitude { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int gmtoff { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int dstoff { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string country_code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string timezone { get; set; }
    }

    public class MembersItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string email_address { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string unique_email_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string email_type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Merge_fields merge_fields { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Stats stats { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ip_signup { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string timestamp_signup { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ip_opt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string timestamp_opt { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int member_rating { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string last_changed { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string language { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string vip { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string email_client { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Location location { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string list_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<_linksItem> _links { get; set; }
    }

    public class _linksItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string rel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string href { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string targetSchema { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string schema { get; set; }
    }

    public class Root
    {
        /// <summary>
        /// 
        /// </summary>
        public List<MembersItem> members { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string list_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int total_items { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<_linksItem> _links { get; set; }
    }

}
