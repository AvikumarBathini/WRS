using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ComponentModel;
using System.Configuration;

namespace WRSDataMigrationInt.Infrastructure
{
    public static class ConfigHelper
    {
        /// <summary>
        /// Get AppSetttings parameters from config
        /// </summary> 
        /// <param name="key">appsettings's key name</param>
        /// <returns></returns>
        public static T GetSysParam<T>(string key)
        {
            var result = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(result))
            {
                return default(T);
            }

            var type = typeof(T);
            var converter = TypeDescriptor.GetConverter(type);
            if (converter == null || !converter.CanConvertFrom(typeof(string)))
            {
                throw new InvalidCastException(string.Format("Unable to convert from the value of {0} to {1}, which value is:{2}",
                    key, type.FullName, result));
            }

            return (T)converter.ConvertFrom(result);
        }

        public static T Convert<T>(object result)
        {
            var type = typeof(T);
            var converter = TypeDescriptor.GetConverter(type);

            if (!converter.CanConvertFrom(typeof(string)))
            {
                throw new InvalidCastException(
                    string.Format("Unable to convert from the value to {0}, which value is:{1}", type.FullName, result));
            }

            if (string.IsNullOrWhiteSpace(result as string))
            {
                return default(T);
            }

            return (T)converter.ConvertFrom(result);
        }

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
    }
}
