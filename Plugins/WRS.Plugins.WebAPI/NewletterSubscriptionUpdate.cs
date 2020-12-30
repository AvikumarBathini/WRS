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
    public class NewletterSubscriptionUpdate : IPlugin
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
                if (targetEntity.LogicalName != Constants.WRSEntityName.Entity_NewsletterSubscription || (context.MessageName != Constants.MessageTypes.MSG_UPDATE))
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
                    //if (targetEntity.Contains("wrs_sourcefrom"))
                    //{
                    //    sourecFrom = targetEntity.GetAttributeValue<string>("wrs_sourcefrom");
                    //}
                    if (context.PostEntityImages.Contains("PostImage"))
                    {
                        postImage = context.PostEntityImages["PostImage"];
                        if (postImage != null)
                        {
                            sourecFrom = postImage.GetAttributeValue<string>("wrs_sourcefrom");
                        }
                    }
                    if (string.IsNullOrEmpty(sourecFrom) || sourecFrom != "crm") return;
                    if (targetEntity.Contains("wrs_email") || targetEntity.Contains("wrs_subscriptionstatus"))
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

                        var email = postImage.GetAttributeValue<string>("wrs_email");
                        var subscribeStatus = postImage.GetAttributeValue<bool>("wrs_subscriptionstatus");
                        var requestEntity = new NewsletterUpdateFromPluginModel();
                        requestEntity.ApiSecretKey = Constants.WEBAPIURL.ApiSecretKey;
                        requestEntity.EmailAddress = email;
                        requestEntity.Subscribe = subscribeStatus;
                        requestEntity.CustomerId = string.IsNullOrEmpty(customerId) ? Constants.WEBAPIURL.CustomerId : Convert.ToInt32(customerId);
                        var requestData = EntityToJson(requestEntity);//new JavaScriptSerializer().Serialize(requestEntity);//JsonConvert.SerializeObject(requestEntity);
                        var requestApiUrl = GetCrmDataTool.GetApiDoaminName(service, Constants.WEBAPIURL.APIURL_UpdateNewletterSubscriptionStatus);
                        var response = HttpRequestUrl.PostSecurityRequest(requestApiUrl, requestData, authUserName.Split('\\')[1], userSecret, authUserName.Split('\\')[0]);
                        if (!string.IsNullOrEmpty(response))
                        {
                            var newsletterSubscription = new Entity(Constants.WRSEntityName.Entity_NewsletterSubscription);
                            newsletterSubscription.Id = postImage.Id;
                            if (response.Length >= 4000)
                            {
                                newsletterSubscription["wrs_apiresponse"] = response.Substring(0, 4000);
                            }
                            else
                            {
                                newsletterSubscription["wrs_apiresponse"] = response;
                            }
                            service.Update(newsletterSubscription);
                            var responseEntity = Deserialize<ResponseModel.SubscribeNewsletterResponse>(response);
                            if (responseEntity.IsSuccess)
                            {
                                if (responseEntity.SubscribeNewsletterResult == "Enter valid email")
                                {
                                    throw new Exception("API to update failure. error message: Enter valid email");
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

    public class NewsletterUpdateFromPluginModel
    {
        public string ApiSecretKey { get; set; }

        public string EmailAddress { get; set; }

        public bool Subscribe { get; set; }

        public int CustomerId { get; set; }
    }
}
