using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRS.OneTimeLoad.Product
{
    class Program
    {
        public static IOrganizationService CrmService = serviceClient();
        static void Main(string[] args)
        {
            PublishAllProducts();
        }

        private static void PublishAllProducts()
        {
            string fetch =
                  "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>" +
                      "<entity name='product'>" +
                        "<attribute name='name' />" +
                        "<attribute name='productid' />" +
                        "<attribute name='productnumber' />" +
                        "<attribute name='description' />" +
                        "<attribute name='statecode' />" +
                        "<attribute name='productstructure' />" +
                        "<order attribute='productnumber' descending='false' />" +
                        "<filter type='and'>" +
                          "<condition attribute='statecode' operator='eq' value='3' />" +
                        //<condition attribute="statecode" operator="eq" value="3" />
                        "</filter>" +
                      "</entity>" +
                    "</fetch>";
            EntityCollection coll = CrmService.RetrieveMultiple(new FetchExpression(fetch));
            if (coll != null && coll.Entities.Count > 0)
                foreach (Entity product in coll.Entities)
                {
                    PublishProductHierarchyRequest Req = new PublishProductHierarchyRequest
                    {
                        Target = new EntityReference(product.LogicalName, product.Id)
                    };
                    CrmService.Execute(Req);
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
}
