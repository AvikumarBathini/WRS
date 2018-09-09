using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WRSDataMigrationInt.Infrastructure;
using WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling;

namespace WRS.Plugin.Product
{
    public class ProductUpdate : IPlugin
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
                if (targetEntity.LogicalName != Constants.WRSEntityName.Entity_Product || context.MessageName != Constants.MessageTypes.MSG_UPDATE)
                    return;

                // Obtain the organization service reference which you will need for
                // web service calls.
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                try
                {
                    if (!targetEntity.Contains("wrs_sourcefrom"))
                        return;
                    if (targetEntity.GetAttributeValue<string>("wrs_sourcefrom").ToLower() == "nc")
                    {
                        Entity prodcut = service.Retrieve(targetEntity.LogicalName, targetEntity.Id, new ColumnSet("statecode", "statuscode", "wrs_published"));
                        if (prodcut.Contains("statecode"))
                        {
                            int statecode = prodcut.GetAttributeValue<OptionSetValue>("statecode").Value;
                            int statuscode = prodcut.GetAttributeValue<OptionSetValue>("statuscode").Value;
                            bool wrs_published = prodcut.Contains("wrs_published") ? prodcut.GetAttributeValue<bool>("wrs_published") : false;
                            //If its Active, change to Under Revision
                            if (statecode == 0 || (statuscode == 167320000 && wrs_published))
                            {
                                EntityReference target = new EntityReference(targetEntity.LogicalName, targetEntity.Id);
                                SetStateRequest request = new SetStateRequest();
                                request.EntityMoniker = target;
                                request.State = new OptionSetValue(3);
                                request.Status = new OptionSetValue(3);
                                service.Execute(request);
                            }
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
    public class UpdateProductModel
    {
        public int ProductId { get; set; }

        public bool IsPublish { get; set; }

        public string ApiSecretKey { get; set; }

        public int CustomerId { get; set; }
    }
}
