using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRSDataMigrationInt.Infrastructure
{
    public class Constants
    {
        public class WRSEntityName
        {
            public readonly static string Entity_Product = "product";

            public readonly static string Entity_Contact = "contact";

            public readonly static string Entity_BlackoutCalendar = "wrs_blackoutcalendar";

            public readonly static string Entity_BlackoutCalendarDetail = "wrs_blackoutcalendardetail";

            public readonly static string Entity_BlackoutCalendarProduct = "wrs_blackoutcalendarproduct";

            public readonly static string Entity_NewsletterSubscription = "wrs_subscription";

            public readonly static string Entity_ResourceManagement= "wrs_resourcemanagement";

            public readonly static string Entity_PriceCalendar = "wrs_pricecalendar";

            public readonly static string Entity_Confihuration = "wrs_configuration";

            public readonly static string Entity_Pricelevel = "pricelevel";

            public readonly static string Entity_park = "wrs_park";
        }

        public class MessageTypes
        {
            public readonly static string MSG_UPDATE = "Update";
            public readonly static string MSG_CREATE = "Create";
            public readonly static string MSG_CANCEL = "Cancel";
            public readonly static string MSG_CLOSE = "Close";
            public readonly static string MSG_DELETE = "Delete";
            public readonly static string MSG_ASSIGN = "Assign";
            public readonly static string MSG_TARGET = "Target";
            public readonly static string MSG_RelationShip = "Relationship";
            public readonly static string MSG_SETSTATE = "SetState";
            public readonly static string MSG_SetStateDynamicEntity = "SetStateDynamicEntity";
        }

        public class WEBAPIURL
        {
            public readonly static string APIURL_UpdateProductPublishStatus = "https://{0}/api/crmInterface/UpdateProductPublish";
            public readonly static string APIURL_UpsertCustomer = "https://{0}/api/crmInterface/UpsertCustomer";
            public readonly static string APIURL_CreateOrUpdateBlackoutCalendar = "https://{0}/api/crmInterface/CreateOrUpdateBlackoutCalendar";
            public readonly static string APIURL_CreateOrUpdateOrDeleteBlackoutCalendarDetail = "https://{0}/api/crmInterface/CreateOrUpdateOrDeleteBlackoutCalendarDetail";
            public readonly static string APIURL_CreateOrDeleteBlackoutCalendarProductMapping = "https://{0}/api/crmInterface/CreateOrDeleteBlackoutCalendarProductMapping";
            public readonly static string APIURL_UpdateNewletterSubscriptionStatus = "https://{0}/api/crmInterface/UpdateSubscribeNewsletter";
            public readonly static string APIURL_UpdateResourceManager = "https://{0}/api/crmInterface/UpdateResourceManager";
            public readonly static string APIURL_PriceCalendarEdit = "https://{0}/api/crmInterface/PriceCalendarEdit";
            public readonly static string ApiSecretKey = "f98y119k107i99a107k118j110g99";
            public readonly static int CustomerId = 933821;
            public readonly static int NCId = 000000;
            public readonly static string APIURL_UpdateProductDescription = "https://{0}/api/crmInterface/UpdateProductDescription";
        }
    }
}
