using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.Text;
using WRSDataMigrationInt.Infrastructure;
using WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling;

namespace WRS.Plugin.Product
{
    public class ContactCreateOrUpdate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity && context.Depth == 1)
            {
                Entity targetEntity = (Entity)context.InputParameters["Target"];
                if (targetEntity.LogicalName != Constants.WRSEntityName.Entity_Contact || (context.MessageName != Constants.MessageTypes.MSG_UPDATE && context.MessageName != Constants.MessageTypes.MSG_CREATE))
                    return;
                if (targetEntity.Contains("wrs_apiresponse")) return;
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                try
                {
                    var postImage = new Entity();
                    var sourecFrom = "";
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

                    if (sourecFrom.ToLower() != "nc")
                    {
                        if (targetEntity.Contains("emailaddress1") || targetEntity.Contains("telephone1") || targetEntity.Contains("firstname") || targetEntity.Contains("lastname"))
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
                            string emailAddress = (postImage != null && postImage.Contains("emailaddress1")) ? postImage.GetAttributeValue<string>("emailaddress1").Replace(" ", "") : (targetEntity.Contains("emailaddress1") ? targetEntity.GetAttributeValue<string>("emailaddress1").Replace(" ", "") : string.Empty);
                            string phoneNumber = (postImage != null && postImage.Contains("telephone1")) ? postImage.GetAttributeValue<string>("telephone1").Replace(" ", "") : (targetEntity.Contains("telephone1") ? targetEntity.GetAttributeValue<string>("telephone1").Replace(" ", "") : string.Empty);
                            string firstName = (postImage != null && postImage.Contains("firstname")) ? postImage.GetAttributeValue<string>("firstname") : (targetEntity.Contains("firstname") ? targetEntity.GetAttributeValue<string>("firstname") : string.Empty);
                            string lastName = (postImage != null && postImage.Contains("lastname")) ? postImage.GetAttributeValue<string>("lastname") : (targetEntity.Contains("lastname") ? targetEntity.GetAttributeValue<string>("lastname") : string.Empty);
                            string ncid = (postImage != null && postImage.Contains("wrs_id")) ? postImage.GetAttributeValue<int>("wrs_id").ToString() : (targetEntity.Contains("wrs_id") ? targetEntity.GetAttributeValue<int>("wrs_id").ToString() : string.Empty);
                            if (string.IsNullOrEmpty(emailAddress) && string.IsNullOrEmpty(phoneNumber))
                            {
                                throw new InvalidPluginExecutionException("Please provide Email or Phone Number.");
                            }
                            if (context.MessageName == Constants.MessageTypes.MSG_UPDATE && string.IsNullOrEmpty(ncid))
                            {
                                throw new InvalidPluginExecutionException("Please provide NC ID.");
                            }

                            var requestEntity = new UpsertCustomerFromPluginModel
                            {
                                ApiSecretKey = Constants.WEBAPIURL.ApiSecretKey,
                                EmailAddress = emailAddress,
                                FirstName = firstName,
                                LastName = lastName,
                                PhoneNumber = phoneNumber,
                                CustomerId = string.IsNullOrEmpty(customerId) ? Constants.WEBAPIURL.CustomerId : Convert.ToInt32(customerId),
                                NCId = string.IsNullOrEmpty(ncid) ? Constants.WEBAPIURL.NCId : Convert.ToInt32(ncid)

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
                                    if (context.MessageName == Constants.MessageTypes.MSG_CREATE)
                                    {
                                        contact["wrs_id"] = int.Parse(responseEntity.CustomerId);
                                        contact["wrs_accountnumber"] = responseEntity.AccountNumber;
                                        service.Update(contact);
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
                    else
                        return;
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
    public class UpsertCustomerFromPluginModel
    {
        public string ApiSecretKey { get; set; }

        public string EmailAddress { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public int CustomerId { get; set; }

        public int NCId { get; set; }
    }



}
