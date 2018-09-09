using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRSDataMigrationInt.Infrastructure
{
    public class RequestModel
    {
        public class UpdateProductFromPluginModel
        {
            public int ProductId { get; set; }

            public bool IsPublish { get; set; }

            public string ApiSecretKey { get; set; }

            public int CustomerId { get; set; }
        }

        public class UpdateProductModel
        {
            [JsonProperty(PropertyName = "productId")]
            public int ProductId { get; set; }

            [JsonProperty(PropertyName = "isPublish")]
            public bool IsPublish { get; set; }

            [JsonProperty(PropertyName = "apiSecretKey")]
            public string ApiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int StoreId { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int CustomerId { get; set; }
        }

        public class UpdateProductDescFromPluginModel
        {
            public int ProductId { get; set; }

            public string FullDesc { get; set; }

            public string ShortDesc { get; set; }

            public string ApiSecretKey { get; set; }

            public int CustomerId { get; set; }
        }

        public class UpdateProductDescModel
        {
            [JsonProperty(PropertyName = "productId")]
            public int ProductId { get; set; }

            [JsonProperty(PropertyName = "fullShortDescription")]
            public string FullDescription { get; set; }

            [JsonProperty(PropertyName = "newShortDescription")]
            public string ShortDescription { get; set; }

            [JsonProperty(PropertyName = "apiSecretKey")]
            public string ApiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int StoreId { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int CustomerId { get; set; }
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

        public class UpsertCustomerRequestModel
        {
            [JsonProperty(PropertyName = "apiSecretKey")]
            public string ApiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int StoreId { get; set; }

            [JsonProperty(PropertyName = "languageId")]
            public int LanguageId { get; set; }

            [JsonProperty(PropertyName = "emailAddress")]
            public string EmailAddress { get; set; }

            [JsonProperty(PropertyName = "firstname")]
            public string FirstName { get; set; }

            [JsonProperty(PropertyName = "lastname")]
            public string LastName { get; set; }

            [JsonProperty(PropertyName = "phonenumber")]
            public string PhoneNumber { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int CustomerId { get; set; }

            [JsonProperty(PropertyName = "NCId")]
            public int NCId { get; set; }
        }

        public class BlackoutCalendarFromPluginModel
        {
            public string ApiSecretKey { get; set; }

            public string RequestType { get; set; }

            public int Id { get; set; }

            public string Name { get; set; }

            public string Remarks { get; set; }

            public int CustomerId { get; set; }
        }

        public class BlackoutCalendarRequestModel
        {
            [JsonProperty(PropertyName = "apiSecretKey")]
            public string ApiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int StoreId { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int CustomerId { get; set; }

            [JsonProperty(PropertyName = "blackoutCalendarId")]
            public int BlackoutCalendarId { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "remarks")]
            public string Remarks { get; set; }
        }


        public class BlackoutCalendarDetailFromPluginModel
        {
            public string ApiSecretKey { get; set; }

            public string RequestType { get; set; }

            public int BlackoutCalendarId { get; set; }

            public int DetailId { get; set; }

            public string Name { get; set; }

            /// <summary>
            /// date in "yyyy-MM-dd" format
            /// </summary>
            public string DateFrom { get; set; }

            /// <summary>
            /// date in "yyyy-MM-dd" format
            /// </summary>
            public string DateTo { get; set; }

            public int CustomerId { get; set; }
        }

        public class BlackoutCalendarDetailModel
        {
            [JsonProperty(PropertyName = "apiSecretKey")]
            public string ApiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int StoreId { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int CustomerId { get; set; }

            [JsonProperty(PropertyName = "blackoutCalendarId")]
            public int BlackoutCalendarId { get; set; }

            [JsonProperty(PropertyName = "detailId")]
            public int DetailId { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            /// <summary>
            /// date in "yyyy-MM-dd" format
            /// </summary>
            [JsonProperty(PropertyName = "dateFrom")]
            public string DateFrom { get; set; }

            /// <summary>
            /// date in "yyyy-MM-dd" format
            /// </summary>
            [JsonProperty(PropertyName = "dateTo")]
            public string DateTo { get; set; }
        }

        public class BlackoutCalendarProductMappingFromPluginModel
        {
            public string ApiSecretKey { get; set; }

            public string RequestType { get; set; }

            public int BlackoutCalendarId { get; set; }

            public int ProductId { get; set; }

            public int BlackoutCalendarProductId { get; set; }

            public int CustomerId { get; set; }
        }

        public class BlackoutCalendarProductMappingModel
        {
            [JsonProperty(PropertyName = "apiSecretKey")]
            public string ApiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int StoreId { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int CustomerId { get; set; }

            [JsonProperty(PropertyName = "blackoutCalendarId")]
            public int BlackoutCalendarId { get; set; }

            [JsonProperty(PropertyName = "productId")]
            public int ProductId { get; set; }

            [JsonProperty(PropertyName = "blackoutCalendarProductId")]
            public int BlackoutCalendarProductId { get; set; }

        }

        public class NewsletterUpdateFromPluginModel
        {
            public string ApiSecretKey { get; set; }

            public string EmailAddress { get; set; }

            public bool Subscribe { get; set; }

            public int CustomerId { get; set; }
        }

        public class NewsletterUpdateModel
        {
            [JsonProperty(PropertyName = "apiSecretKey")]
            public string ApiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int StoreId { get; set; }

            [JsonProperty(PropertyName = "languageId")]
            public int LanguageId { get; set; }

            [JsonProperty(PropertyName = "email")]
            public string Email { get; set; }

            [JsonProperty(PropertyName = "subscribe")]
            public bool Subscribe { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int CustomerId { get; set; }
        }

        public class ProductUpdateURL
        {
            [JsonProperty(PropertyName = "productNCId")]
            public int ProductNCId { get; set; }

            [JsonProperty(PropertyName = "productURL")]
            public string ProductURL { get; set; }
        }

        public class ResourceManagementFromPluginModel
        {
            public string ApiSecretKey { get; set; }

            public string RequestType { get; set; }

            public int CustomerId { get; set; }

            public int StockId { get; set; }

            public int AdjustQuantity { get; set; }

            public bool Active { get; set; }
        }

        public class ResourceManagementModel
        {
            [JsonProperty(PropertyName = "apiSecretKey")]
            public string ApiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int StoreId { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int CustomerId { get; set; }

            [JsonProperty(PropertyName = "stockId")]
            public int StockId { get; set; }

            [JsonProperty(PropertyName = "adjustQuantity")]
            public int AdjustQuantity { get; set; }

            [JsonProperty(PropertyName = "active")]
            public bool Active { get; set; }
        }

        public class PriceCalendarFromPluginModel
        {
            public string ApiSecretKey { get; set; }

            public int CustomerId { get; set; }

            public int CalendarId { get; set; }

            public int NewPriceGroupId { get; set; }

            public int CalendarTypeId { get; set; }

            public DateTime? CalendarDate { get; set; }
        }

        public class PriceCalendarModel
        {
            [JsonProperty(PropertyName = "apiSecretKey")]
            public string apiSecretKey { get; set; }

            [JsonProperty(PropertyName = "storeId")]
            public int storeId { get; set; }

            [JsonProperty(PropertyName = "customerId")]
            public int customerId { get; set; }

            [JsonProperty(PropertyName = "calendarId")]
            public int calendarId { get; set; }

            [JsonProperty(PropertyName = "newPriceGroupId")]
            public int newPriceGroupId { get; set; }

            [JsonProperty(PropertyName = "calendarTypeId")]
            public int calendarTypeId { get; set; }

            [JsonProperty(PropertyName = "calendarDate")]
            public DateTime? calendarDate { get; set; }
        }

    }
}
