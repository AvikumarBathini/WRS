using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace WRS.Action.Customer
{
    public class UpsertCustomer : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            StringBuilder sb = new StringBuilder();

            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is EntityReference)
            {
                EntityReference contactRef = (EntityReference)context.InputParameters["Target"];
                if (contactRef.LogicalName == "contact")
                {
                    Entity con = service.Retrieve(contactRef.LogicalName, contactRef.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("emailaddress1", "telephone1", "firstname", "lastname"));

                    string authUserName = GetConfirurationByParaGroupAndKey(service, "WRS_API", "userName");
                    string password = GetConfirurationByParaGroupAndKey(service, "WRS_API", "password");
                    if (string.IsNullOrEmpty(authUserName) || string.IsNullOrEmpty(password))
                    {
                        throw new Exception("API authentication username and password can not null");
                    }
                    var getEncodeByte = Convert.FromBase64String(password);
                    password = Encoding.UTF8.GetString(getEncodeByte);
                    string customerId = GetConfirurationByParaGroupAndKey(service, "WRS_API", "customerId");

                    string emailAddress = con.Contains("emailaddress1") ? con.GetAttributeValue<string>("emailaddress1") :  string.Empty;
                    string phoneNumber = con.Contains("telephone1") ? con.GetAttributeValue<string>("telephone1") : string.Empty;
                    string firstName = con.Contains("firstname") ? con.GetAttributeValue<string>("firstname") :  string.Empty;
                    string lastName = con.Contains("lastname") ? con.GetAttributeValue<string>("lastname") : string.Empty;

                    if (string.IsNullOrEmpty(emailAddress) && string.IsNullOrEmpty(phoneNumber))
                        return;

                    var requestEntity = new UpsertCustomerFromPluginModel
                    {
                        ApiSecretKey = "f98y119k107i99a107k118j110g99",
                        EmailAddress = emailAddress.Trim(),
                        FirstName = firstName.Trim(),
                        LastName = lastName.Trim(),
                        PhoneNumber = phoneNumber.Trim(),
                        CustomerId = string.IsNullOrEmpty(customerId) ? 933821 : Convert.ToInt32(customerId)
                    };
                    string requestData = EntityToJson(requestEntity);//new JavaScriptSerializer().Serialize(requestEntity);//JsonConvert.SerializeObject(requestEntity);
                    string requestApiUrl = GetApiDoaminName(service, "https://{0}/api/crmInterface/UpsertCustomer");
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

                    //ServicePointManager.Expect100Continue = true;
                    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });


                    //var response = HttpRequestUrl.PostSecurityRequest(requestApiUrl, requestData, authUserName.Split('\\')[1], password, authUserName.Split('\\')[0]);
                    // var response = HttpRequestUrl.PostSecurityRequest(requestApiUrl, requestData, userName, password, domain);


                    #region API Call
                    string response = "";
                    System.Net.HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestApiUrl);
                    byte[] bytes = Encoding.GetEncoding("UTF-8").GetBytes(requestData);
                    httpWebRequest.Method = "Post";
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    httpWebRequest.ContentLength = (long)bytes.Length;
                    httpWebRequest.Timeout = 60000;
                    httpWebRequest.UseDefaultCredentials = false;
                    httpWebRequest.PreAuthenticate = false;
                    if (domain != string.Empty)
                    {
                        httpWebRequest.Credentials = new NetworkCredential(userName, password, domain);
                    }
                    else
                    {
                        httpWebRequest.Credentials = new NetworkCredential(userName, password);
                    }
                    Stream requestStream = httpWebRequest.GetRequestStream();
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();
                    Stream responseStream = httpWebRequest.GetResponse().GetResponseStream();
                    if (responseStream != null)
                    {
                        StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding("UTF-8"));
                        response = streamReader.ReadToEnd();
                        streamReader.Close();
                        responseStream.Close();
                    }
                    //return str;
                    #endregion


                    if (!string.IsNullOrEmpty(response))
                    {
                        var contact = new Entity("contact");
                        contact.Id = contactRef.Id;
                        if (response.Length >= 4000)
                        {
                            contact["wrs_apiresponse"] = response.Substring(0, 4000);
                        }
                        else
                        {
                            contact["wrs_apiresponse"] = response;
                        }
                        UpsertCustomerResult responseEntity = Deserialize<UpsertCustomerResult>(response);
                        if (responseEntity.IsSuccess)
                        {
                            contact["wrs_id"] = int.Parse(responseEntity.CustomerId);
                            if (!con.Contains("wrs_accountnumber"))
                                contact["wrs_accountnumber"] = responseEntity.AccountNumber;
                            service.Update(contact);
                        }
                        else
                        {
                            service.Update(contact);
                            throw new InvalidPluginExecutionException("API to update failure. error message:" + responseEntity.Message);
                        }
                    }
                    else
                    {
                        throw new InvalidPluginExecutionException("API to update failure");
                    }
                }
            }
        }

        public string GetApiDoaminName(IOrganizationService service, string url)
        {
            var domainName = GetConfirurationByParaGroupAndKey(service, "WRS_API", "apiServerDomainName");
            return string.Format(url, domainName);
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

        private string GetConfirurationByParaGroupAndKey(IOrganizationService service, string parameterGroup, string key)
        {
            string responseValue = "";
            var query = new QueryExpression("wrs_configuration");
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
    }

    public class UpsertCustomerResult
    {
        public int Id { get; set; }

        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public string CustomerId { get; set; }

        public string AccountNumber { get; set; }
    }

    public class UpsertCustomerFromPluginModel
    {
        public string ApiSecretKey { get; set; }

        public string EmailAddress { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public int CustomerId { get; set; }
    }
}
