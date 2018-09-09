using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WRSDataMigrationInt.Infrastructure;

namespace WRS.Plugins.WebAPI
{
    public static class GetCrmDataTool
    {
        public static string GetConfirurationByParaGroupAndKey(IOrganizationService service, string parameterGroup, string key)
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

        public static string GetApiDoaminName(IOrganizationService service, string url)
        {
            var domainName = GetConfirurationByParaGroupAndKey(service, "WRS_API", "apiServerDomainName");
            return string.Format(url, domainName);
        }
    }
}
