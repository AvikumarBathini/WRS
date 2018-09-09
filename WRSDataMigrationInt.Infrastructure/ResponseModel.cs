using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WRSDataMigrationInt.Infrastructure
{
    public class ResponseModel
    {
        public class UpdateProductPublishResult
        {
            public int Id { get; set; }

            public bool IsSuccess { get; set; }

            public string Message { get; set; }
        }

        public class UpdateProductDescriptionResult
        {
            public int Id { get; set; }

            public bool IsSuccess { get; set; }

            public string Message { get; set; }
        }

        public class UpsertCustomerResult
        {
            public int Id { get; set; }

            public bool IsSuccess { get; set; }

            public string Message { get; set; }

            public string CustomerId { get; set; }

            public string AccountNumber { get; set; }
        }

        public class DetailsItem
        {
            /// <summary>
            /// 
            /// </summary>
            public int CreatedById { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string CreatedOnUtc { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string DateFrom { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string DateTo { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int Id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int UpdatedById { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string UpdatedOnUtc { get; set; }
        }

        public class ProductsItem
        {
            /// <summary>
            /// 
            /// </summary>
            public int CreatedById { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string CreatedOnUtc { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int Id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int ProductId { get; set; }
        }

        public class BlackoutCalendar
        {
            /// <summary>
            /// 
            /// </summary>
            public int Id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public bool IsSuccess { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Message { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int CreatedById { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string CreatedOnUtc { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public List<DetailsItem> Details { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public List<ProductsItem> Products { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Remarks { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int UpdatedById { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string UpdatedOnUtc { get; set; }
        }

        public class BlackoutCalendarDetailResult
        {
            /// <summary>
            /// 
            /// </summary>
            public int Id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public bool IsSuccess { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Message { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int CreatedById { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string CreatedOnUtc { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string DateFrom { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string DateTo { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int UpdatedById { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string UpdatedOnUtc { get; set; }
        }

        public class BlackoutCalendarProductMappingResult
        {
            /// <summary>
            /// 
            /// </summary>
            public int Id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public bool IsSuccess { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Message { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int CreatedById { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string CreatedOnUtc { get; set; }

            public int ProductId { get; set; }
        }

        public class CommonResonseModel
        {
            public bool IsSuccess { get; set; }

            public string Message { get; set; }
        }

        public class Root
        {
            /// <summary>
            /// 
            /// </summary>
            public UpdateProductPublishResult UpdateProductPublishResult { get; set; }
            public UpdateProductDescriptionResult UpdateProductDescriptionResult { get; set; }

            public UpsertCustomerResult UpsertCustomerResult { get; set; }

            public BlackoutCalendar CreateBlackoutCalendarResult { get; set; }

            public BlackoutCalendar UpdateBlackoutCalendarResult { get; set; }

            public BlackoutCalendarDetailResult CreateBlackoutCalendarDetailResult { get; set; }

            public BlackoutCalendarDetailResult UpdateBlackoutCalendarDetailResult { get; set; }

            public BlackoutCalendarDetailResult DeleteBlackoutCalendarDetailResult { get; set; }

            public BlackoutCalendarProductMappingResult CreateBlackoutCalendarProductMappingResult { get; set; }

            public BlackoutCalendarProductMappingResult DeleteBlackoutCalendarProductMappingResult { get; set; }

            public UpdateResourceManagement AdjustRMEventStockQuantityResult { get; set; }

            public UpdateResourceManagement UpdateRMEventStockActiveResult { get; set; }

            public PriceCalendarResponse PriceCalendarEditResult { get; set; }
        }

        public class SubscribeNewsletterResponse
        {
            public string SubscribeNewsletterResult { get; set; }

            public bool IsSuccess { get; set; }

            public string Message { get; set; }
        }

        public class UpdateResourceManagement
        {
            public int Id { get; set; }

            public bool IsSuccess { get; set; }

            public string Message { get; set; }
        }

        public class PriceCalendarResponse
        {
            public int Id { get; set; }

            public bool IsSuccess { get; set; }

            public string Message { get; set; }
        }
    }
}
