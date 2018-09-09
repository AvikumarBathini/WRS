using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.Text;
using WRS.Plugins.WebAPI;
using WRSDataMigrationInt.Infrastructure;
using WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling;

namespace WRS.Plugin.Product
{
    public class BlackoutCalendarDetailCreateOrUpdateOrDelete : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Extract the tracing service for use in debugging sandboxed plug-ins.
            // If you are not registering the plug-in in the sandbox, then you do
            // not have to add any tracing service related code.
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target"))
            {
                // Obtain the target entity from the input parameters.
                Entity targetEntity = new Entity();
                var entityName = "";
                if (context.InputParameters["Target"] is Entity)
                {
                    targetEntity = (Entity)context.InputParameters["Target"];
                    entityName = targetEntity.LogicalName;
                }
                else if (context.InputParameters["Target"] is EntityReference)
                {
                    var entityRefer = (EntityReference)context.InputParameters["Target"];
                    entityName = entityRefer.LogicalName;
                }
                // Verify that the target entity represents an entity type you are expecting. 
                // For example, an account. If not, the plug-in was not registered correctly.
                if (entityName != Constants.WRSEntityName.Entity_BlackoutCalendarDetail || (context.MessageName != Constants.MessageTypes.MSG_UPDATE && context.MessageName != Constants.MessageTypes.MSG_CREATE))
                    return;

                if (targetEntity.Contains("wrs_apiresponse")) return;
                // Obtain the organization service reference which you will need for
                // web service calls.
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                try
                {
                    var sourecFrom = "";
                    var postImage = new Entity();
                    var preImage = new Entity();
                    if (targetEntity.Contains("wrs_sourcefrom"))
                    {
                        sourecFrom = targetEntity.GetAttributeValue<string>("wrs_sourcefrom");
                    }
                    if (context.PostEntityImages.Contains("PostImage"))
                    {
                        postImage = context.PostEntityImages["PostImage"];
                        if (postImage != null)
                        {
                            sourecFrom = postImage.GetAttributeValue<string>("wrs_sourcefrom");
                        }
                    }
                    if (context.PreEntityImages.Contains("PreImage"))
                    {
                        preImage = context.PreEntityImages["PreImage"];
                        if (preImage != null)
                        {
                            sourecFrom = preImage.GetAttributeValue<string>("wrs_sourcefrom");
                        }
                    }
                    if (string.IsNullOrEmpty(sourecFrom) || sourecFrom != "crm") return;

                    //get authentication info
                    var authUserName = GetCrmDataTool.GetConfirurationByParaGroupAndKey(service, "WRS_API", "userName");
                    var userSecret = GetCrmDataTool.GetConfirurationByParaGroupAndKey(service, "WRS_API", "password");
                    if (string.IsNullOrEmpty(authUserName) || string.IsNullOrEmpty(userSecret))
                    {
                        throw new Exception("API authentication username and password can not null");
                    }
                    var getEncodeByte = Convert.FromBase64String(userSecret);
                    userSecret = Encoding.UTF8.GetString(getEncodeByte);
                    var customerId = GetCrmDataTool.GetConfirurationByParaGroupAndKey(service, "WRS_API", "customerId");

                    var id = 0;
                    var name = "";
                    var dateFrom = "";
                    var dateTo = "";
                    var blackoutCalendarId = 0;
                    var requestType = "";
                    if (context.MessageName == Constants.MessageTypes.MSG_CREATE)
                    {
                        name = targetEntity.GetAttributeValue<string>("wrs_name");
                        if (targetEntity.Contains("wrs_datefrom"))
                        {
                            dateFrom = targetEntity.GetAttributeValue<DateTime>("wrs_datefrom").ToLocalTime().ToString("yyyy-MM-dd");
                        }
                        if (targetEntity.Contains("wrs_dateto"))
                        {
                            dateTo = targetEntity.GetAttributeValue<DateTime>("wrs_dateto").ToLocalTime().ToString("yyyy-MM-dd");
                        }
                        if (targetEntity.Contains("wrs_blackoutcalendarid"))
                        {
                            var blackoutcalendar = targetEntity.GetAttributeValue<EntityReference>("wrs_blackoutcalendarid").Id;
                            var requeryEntityById = service.Retrieve(Constants.WRSEntityName.Entity_BlackoutCalendar, blackoutcalendar, new ColumnSet("wrs_id"));
                            if (requeryEntityById != null)
                            {
                                blackoutCalendarId = requeryEntityById.GetAttributeValue<int>("wrs_id");
                            }
                        }
                        requestType = "create";
                    }
                    else if (context.MessageName == Constants.MessageTypes.MSG_UPDATE)
                    {
                        if (postImage.Contains("wrs_id"))
                        {
                            id = postImage.GetAttributeValue<int>("wrs_id");
                        }
                        if (id == 0) { throw new Exception("NC id can not be null"); }
                        if (targetEntity.Contains("wrs_name") || targetEntity.Contains("wrs_datefrom") || targetEntity.Contains("wrs_dateto"))
                        {
                            name = postImage.GetAttributeValue<string>("wrs_name");
                            if (postImage.Contains("wrs_datefrom"))
                            {
                                dateFrom = postImage.GetAttributeValue<DateTime>("wrs_datefrom").ToLocalTime().ToString("yyyy-MM-dd");
                            }
                            if (postImage.Contains("wrs_dateto"))
                            {
                                dateTo = postImage.GetAttributeValue<DateTime>("wrs_dateto").ToLocalTime().ToString("yyyy-MM-dd");
                            }
                            requestType = "update";
                        }
                        else if (targetEntity.Contains("statecode"))
                        {
                            var status = targetEntity.GetAttributeValue<OptionSetValue>("statecode").Value;
                            if (status == 1)
                            {
                                requestType = "delete";
                            }
                            else { return; }
                        }
                        else
                        {
                            return;
                        }
                    }
                    var requestEntity = new BlackoutCalendarDetailFromPluginModel();
                    requestEntity.ApiSecretKey = Constants.WEBAPIURL.ApiSecretKey;
                    requestEntity.DetailId = id;
                    requestEntity.Name = name;
                    requestEntity.DateFrom = dateFrom;
                    requestEntity.DateTo = dateTo;
                    requestEntity.BlackoutCalendarId = blackoutCalendarId;
                    requestEntity.RequestType = requestType;
                    requestEntity.CustomerId = string.IsNullOrEmpty(customerId) ? Constants.WEBAPIURL.CustomerId : Convert.ToInt32(customerId);
                    var requestData = EntityToJson(requestEntity);//new JavaScriptSerializer().Serialize(requestEntity);//JsonConvert.SerializeObject(requestEntity);
                    var requestApiUrl = GetCrmDataTool.GetApiDoaminName(service, Constants.WEBAPIURL.APIURL_CreateOrUpdateOrDeleteBlackoutCalendarDetail);
                    string[] userN = authUserName.Split('\\');
                    string userName = string.Empty;
                    string domain = string.Empty;
                    if (userN.Length > 1)
                    {
                        userName = userN[1];
                        domain = userN[0];
                    }
                    else
                    {
                        userName = authUserName;
                    }
                    //var response = HttpRequestUrl.PostSecurityRequest(requestApiUrl, requestData, authUserName.Split('\\')[1], userSecret, authUserName.Split('\\')[0]);
                    var response = HttpRequestUrl.PostSecurityRequest(requestApiUrl, requestData, userName, userSecret, domain);
                    if (!string.IsNullOrEmpty(response))
                    {
                        var blackoutCalendarDetail = new Entity(Constants.WRSEntityName.Entity_BlackoutCalendarDetail);
                        if (context.MessageName != Constants.MessageTypes.MSG_DELETE)
                        {
                            blackoutCalendarDetail.Id = targetEntity.Id;
                            if (response.Length >= 4000)
                            {
                                blackoutCalendarDetail["wrs_apiresponse"] = response.Substring(0, 4000);
                            }
                            else
                            {
                                blackoutCalendarDetail["wrs_apiresponse"] = response;
                            }
                            service.Update(blackoutCalendarDetail);
                        }
                        var responseEntity = Deserialize<ResponseModel.BlackoutCalendarDetailResult>(response);
                        if (responseEntity.IsSuccess)
                        {
                            if (context.MessageName == Constants.MessageTypes.MSG_CREATE)
                            {
                                blackoutCalendarDetail.Id = targetEntity.Id;
                                blackoutCalendarDetail["wrs_id"] = responseEntity.Id;
                                service.Update(blackoutCalendarDetail);
                            }
                        }
                        else
                        {
                            throw new Exception("API to update failure. error message:" + responseEntity.Message);
                        }
                    }
                    else
                    {
                        throw new Exception("API to update failure");
                    }
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    ExceptionHandle.HandleExceptionForRethrow(tracingService, ex);
                }
            }
        }
        public string EntityToJson(object requestEntity)
        {
            var serializer = new DataContractJsonSerializer(requestEntity.GetType());
            MemoryStream ms = new MemoryStream();
            serializer.WriteObject(ms, requestEntity);
            byte[] myByte = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(myByte, 0, (int)ms.Length);
            string dataString = Encoding.UTF8.GetString(myByte);
            ms.Close();
            return dataString;
        }

        public static T Deserialize<T>(string json)
        {
            T obj = Activator.CreateInstance<T>();
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                return (T)serializer.ReadObject(ms);
            }
        }
    }

    public class BlackoutCalendarDetailFromPluginModel
    {
        public string ApiSecretKey { get; set; }

        public string RequestType { get; set; }

        public int BlackoutCalendarId { get; set; }

        public int DetailId { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// date in "yyyy-MM-dd" format
        /// </summary>
        public string DateFrom { get; set; }

        /// <summary>
        /// date in "yyyy-MM-dd" format
        /// </summary>
        public string DateTo { get; set; }

        public int CustomerId { get; set; }
    }

}
