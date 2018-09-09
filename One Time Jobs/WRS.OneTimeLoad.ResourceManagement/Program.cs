using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRS.OneTimeLoad.ResourceManagement
{
    class Program
    {
        public static IOrganizationService CrmService = serviceClient();
        static void Main(string[] args)
        {
            string path = @"C:\Users\avikb\Source\Workspaces\NCS.MSCRMCC\WRS\Dev\WRS\One Time Jobs\WRS.OneTimeLoad.ResourceManagement\Files\StockIdListToDeactivate.txt";
            string[] lines = System.IO.File.ReadAllLines(path);
            foreach (string line in lines)
            {
                Entity rm = new Entity("wrs_resourcemanagement", "wrs_id", int.Parse(line.ToString()));
                //rm.Attributes.Add("statecode", new OptionSetValue(1));
                //rm.Attributes.Add("statuscode", new OptionSetValue(2));
                rm.Attributes.Add("wrs_status", false);
                CrmService.Update(rm);
            }

        }

        public static IOrganizationService serviceClient()
        {
            CrmServiceClient crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

        public static EntityCollection RetrieveAllRecordsUsingQueryExp(IOrganizationService service, QueryExpression exp)
        {
            var moreRecords = false;
            int page = 1;
            var cookie = string.Empty;
            var entityCollection = new EntityCollection();
            List<EntityCollection> coll = new List<EntityCollection>();
            for (int i = 0; i < 5; i++)
            {
                coll.Add(new EntityCollection());
            }
            do
            {
                EntityCollection collection = service.RetrieveMultiple(exp);
                if (collection.Entities.Count > 0)
                {
                    coll[page % 5].Entities.AddRange(collection.Entities);
                    //entityCollection.Entities.AddRange(collection.Entities);
                }
                moreRecords = collection.MoreRecords;
                if (moreRecords)
                {
                    page++;
                    cookie = string.Format("paging-cookie='{0}' page='{1}'", System.Security.SecurityElement.Escape(collection.PagingCookie), page);
                }
                Console.WriteLine(page);
            } while (moreRecords);
            return entityCollection;
        }


    }
}
