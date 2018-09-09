using Microsoft.Xrm.Sdk;
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
    public class ResourceManagerStateUpdate : IPlugin
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
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity targetEntity = (Entity)context.InputParameters["Target"];

                // Verify that the target entity represents an entity type you are expecting. 
                // For example, an account. If not, the plug-in was not registered correctly.
                if (targetEntity.LogicalName != Constants.WRSEntityName.Entity_ResourceManagement || (context.MessageName != Constants.MessageTypes.MSG_UPDATE))
                    return;
                if (targetEntity.Contains("wrs_apiresponse")) return;
                // Obtain the organization service reference which you will need for
                // web service calls.
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                try
                {
                    var sourecFrom = "";
                    if (context.PostEntityImages.Contains("PostImage"))
                    {
                        var postImage = context.PostEntityImages["PostImage"];
                        if (postImage != null)
                        {
                            sourecFrom = postImage.GetAttributeValue<string>("wrs_sourcefrom");
                        }
                    }
                    if (string.IsNullOrEmpty(sourecFrom) || sourecFrom != "crm") return;
                    if (targetEntity.Contains("wrs_status"))
                    {
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

                        var requestEntity = new ResourceManagementFromPluginModel();
                        if (targetEntity.Contains("wrs_status"))
                        {
                            var preImage = context.PreEntityImages["PreImage"];
                            requestEntity.StockId = preImage.GetAttributeValue<int>("wrs_id");
                            requestEntity.RequestType = "UpdateRMEventStockActive";
                            requestEntity.Active = targetEntity.GetAttributeValue<bool>("wrs_status");
                        }
                        requestEntity.ApiSecretKey = Constants.WEBAPIURL.ApiSecretKey;
                        requestEntity.CustomerId = string.IsNullOrEmpty(customerId) ? Constants.WEBAPIURL.CustomerId : Convert.ToInt32(customerId);
                        var requestData = EntityToJson(requestEntity);//new JavaScriptSerializer().Serialize(requestEntity);//JsonConvert.SerializeObject(requestEntity);
                        var requestApiUrl = GetCrmDataTool.GetApiDoaminName(service, Constants.WEBAPIURL.APIURL_UpdateResourceManager);
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
                            var ResourceManagement = new Entity(Constants.WRSEntityName.Entity_ResourceManagement);
                            ResourceManagement.Id = targetEntity.Id;
                            if (response.Length >= 4000)
                            {
                                ResourceManagement["wrs_apiresponse"] = response.Substring(0, 4000);
                            }
                            else
                            {
                                ResourceManagement["wrs_apiresponse"] = response;
                            }
                            service.Update(ResourceManagement);
                            var responseEntity = Deserialize<ResponseModel.UpdateResourceManagement>(response);
                            if (responseEntity.IsSuccess)
                            {                                
                            }
                            else
                            {
                                throw new Exception("API update failure. error message:" + responseEntity.Message);
                            }
                        }
                        else
                        {
                            throw new Exception("API update failure");
                        }
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

    public class ResourceManagementFromPluginStateModel
    {
        public string ApiSecretKey { get; set; }

        public string RequestType { get; set; }

        public int CustomerId { get; set; }

        public int StockId { get; set; }

        public int AdjustQuantity { get; set; }

        public bool Active { get; set; }
    }

}
