using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.Text;
using WRSDataMigrationInt.Infrastructure;
using WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling;


namespace WRS.Plugin.Product
{
    public class NCIDPopolateForUpdate : IPlugin
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
                if (targetEntity.LogicalName != Constants.WRSEntityName.Entity_Contact || (context.MessageName != Constants.MessageTypes.MSG_UPDATE && context.MessageName != Constants.MessageTypes.MSG_CREATE))
                    return;
                if (targetEntity.Contains("wrs_apiresponse")) return;
                if (targetEntity.Contains("wrs_id")) return;
                // Obtain the organization service reference which you will need for
                // web service calls.
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                try
                {
                    var preImage = new Entity();
                    
                    if (context.PreEntityImages.Contains("PreImage"))
                    {
                        preImage = context.PreEntityImages["PreImage"];
                        
                    }
                    if (preImage.Contains("emailaddress1") || preImage.Contains("telephone1") || preImage.Contains("firstname") || preImage.Contains("lastname"))
                    {
                        var authUserName = GetConfirurationByParaGroupAndKey(service, "WRS_API", "userName");
                        var password = GetConfirurationByParaGroupAndKey(service, "WRS_API", "password");
                        if (string.IsNullOrEmpty(authUserName) || string.IsNullOrEmpty(password))
                        {
                            throw new Exception("API authentication username and password can not null");
                        }
                        var getEncodeByte = Convert.FromBase64String(password);
                        password = Encoding.UTF8.GetString(getEncodeByte);
                        var customerId = GetConfirurationByParaGroupAndKey(service, "WRS_API", "customerId");
                        string emailAddress;
                        string phoneNumber;
                        string firstName;
                        string lastName;

                        emailAddress = preImage.GetAttributeValue<string>("emailaddress1");
                        phoneNumber = preImage.GetAttributeValue<string>("telephone1");
                        firstName = preImage.GetAttributeValue<string>("firstname");
                        lastName = preImage.GetAttributeValue<string>("lastname");

                        var requestEntity = new UpsertCustomerFromPluginModel
                        {
                            ApiSecretKey = Constants.WEBAPIURL.ApiSecretKey,
                            EmailAddress = emailAddress,
                            FirstName = firstName,
                            LastName = lastName,
                            PhoneNumber = phoneNumber,
                            CustomerId = string.IsNullOrEmpty(customerId) ? Constants.WEBAPIURL.CustomerId : Convert.ToInt32(customerId)
                        };
                        var requestData = EntityToJson(requestEntity);//new JavaScriptSerializer().Serialize(requestEntity);//JsonConvert.SerializeObject(requestEntity);
                        var requestApiUrl = GetApiDoaminName(service, Constants.WEBAPIURL.APIURL_UpsertCustomer);
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

                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                        //var response = HttpRequestUrl.PostSecurityRequest(requestApiUrl, requestData, authUserName.Split('\\')[1], password, authUserName.Split('\\')[0]);
                        var response = HttpRequestUrl.PostSecurityRequest(requestApiUrl, requestData, userName, password, domain);
                        if (!string.IsNullOrEmpty(response))
                        {
                            var contact = new Entity(Constants.WRSEntityName.Entity_Contact);
                            contact.Id = targetEntity.Id;
                            if (response.Length >= 4000)
                            {
                                contact["wrs_apiresponse"] = response.Substring(0, 4000);
                            }
                            else
                            {
                                contact["wrs_apiresponse"] = response;
                            }
                            service.Update(contact);
                            var responseEntity = Deserialize<ResponseModel.UpsertCustomerResult>(response);
                            if (responseEntity.IsSuccess)
                            {
                                contact["wrs_id"] = int.Parse(responseEntity.CustomerId);
                                service.Update(contact);
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

        public string GetConfirurationByParaGroupAndKey(IOrganizationService service, string parameterGroup, string key)
        {
            string responseValue = "";
            var query = new QueryExpression(Constants.WRSEntityName.Entity_Confihuration);
            query.ColumnSet = new ColumnSet("wrs_value");
            var condition1 = new ConditionExpression("wrs_name", ConditionOperator.Equal, parameterGroup);
            var condition2 = new ConditionExpression("wrs_key", ConditionOperator.Equal, key);
            query.Criteria.Conditions.Add(condition1);
            query.Criteria.Conditions.Add(condition2);
            var result = service.RetrieveMultiple(query);
            if (result.Entities.Count > 0)
            {
                if (result.Entities[0].Contains("wrs_value"))
                {
                    responseValue = result.Entities[0].GetAttributeValue<string>("wrs_value");
                }
            }
            return responseValue;
        }

        public string GetApiDoaminName(IOrganizationService service, string url)
        {
            var domainName = GetConfirurationByParaGroupAndKey(service, "WRS_API", "apiServerDomainName");
            return string.Format(url, domainName);
        }
    }

    //public class UpsertCustomerFromPluginModel
    //{
    //    public string ApiSecretKey { get; set; }

    //    public string EmailAddress { get; set; }

    //    public string FirstName { get; set; }

    //    public string LastName { get; set; }

    //    public string PhoneNumber { get; set; }

    //    public int CustomerId { get; set; }
    //}
}
