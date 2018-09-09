using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.Http;
using WRS.CRMInterface.WebAPI.Helper;
using WRSDataMigrationInt.Infrastructure;
using WRSDataMigrationInt.Infrastructure.Logger;
using WRSDataMigrationInt.Infrastructure.LoggerExceptionHandling;

namespace WRS.CRMInterface.WebAPI.Controllers
{
    public class CRMInterfaceController : ApiController
    {
        // POST api/CRMInterface/UpdateProductPublish
        // POST api/CRMInterface/UpsertCustomer
        [HttpPost]
        public ResponseModel.UpdateProductPublishResult UpdateProductPublish()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var ProductResponseEntity = new ResponseModel.UpdateProductPublishResult();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.UpdateProductFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var publishPRDUrl = ConfigHelper.GetSysParam<string>("UpdateProductURL");
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    var requestEntity = new RequestModel.UpdateProductModel();
                    requestEntity.ProductId = pluginEntity.ProductId;
                    requestEntity.ApiSecretKey = NCAPIKey;
                    requestEntity.IsPublish = pluginEntity.IsPublish;
                    requestEntity.StoreId = storeId;
                    requestEntity.CustomerId = pluginEntity.CustomerId;
                    var requestData = JsonConvert.SerializeObject(requestEntity);
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var response = HttpRequestUrl.PostRequest(publishPRDUrl, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {
                            ProductResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).UpdateProductPublishResult;

                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "UpdateProductPublish").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                               .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                               .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            if (ProductResponseEntity.IsSuccess)
                            {
                                //DBDataAccess.InsertOrUpdateLog("information", "UpdateProductPublish", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "UpdateProductPublish", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), ProductResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", ProductResponseEntity.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        ProductResponseEntity.IsSuccess = false;
                        ProductResponseEntity.Message = "CRM API Key Is not correct!";
                    }
                }
                else
                {
                    ProductResponseEntity.IsSuccess = false;
                    ProductResponseEntity.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    ProductResponseEntity.IsSuccess = false;
                    ProductResponseEntity.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "UpdateProductPublish", requestDataStr.Replace("'", ""), requestDataStr.Replace("'", ""),
                    //        "", ProductResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "UpdateProductPublish").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", ProductResponseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);

                }
                catch (Exception ex2)
                {
                    ProductResponseEntity.IsSuccess = false;
                    ProductResponseEntity.Message = "API Error:" + ex2.Message;
                }
            }
            return ProductResponseEntity;
        }
        [HttpGet]
        public string getmethodtest(string param)
        {
            return param;
        }
        [HttpPost]
        public ResponseModel.UpdateProductDescriptionResult UpdateProductDescription()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var ProductResponseEntity = new ResponseModel.UpdateProductDescriptionResult();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.UpdateProductDescFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var publishPRDUrl = ConfigHelper.GetSysParam<string>("UpdateProductDesc");
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    var requestEntity = new RequestModel.UpdateProductDescModel();
                    requestEntity.ProductId = pluginEntity.ProductId;
                    requestEntity.ApiSecretKey = NCAPIKey;
                    requestEntity.FullDescription = pluginEntity.FullDesc;
                    requestEntity.ShortDescription = pluginEntity.ShortDesc;
                    requestEntity.StoreId = storeId;
                    requestEntity.CustomerId = pluginEntity.CustomerId;
                    var requestData = JsonConvert.SerializeObject(requestEntity);
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var response = HttpRequestUrl.PostRequest(publishPRDUrl, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {
                            ProductResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).UpdateProductDescriptionResult;

                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "UpdateProductDescription").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                               .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                               .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (ProductResponseEntity.IsSuccess)
                            {
                                //DBDataAccess.InsertOrUpdateLog("information", "UpdateProductDescription", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "UpdateProductDescription", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), ProductResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", ProductResponseEntity.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        ProductResponseEntity.IsSuccess = false;
                        ProductResponseEntity.Message = "CRM API Key Is not correct!";
                    }
                }
                else
                {
                    ProductResponseEntity.IsSuccess = false;
                    ProductResponseEntity.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    ProductResponseEntity.IsSuccess = false;
                    ProductResponseEntity.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "UpdateProductDescription", requestDataStr.Replace("'", ""), requestDataStr.Replace("'", ""),
                    //        "", ProductResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "UpdateProductDescription").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", ProductResponseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);

                }
                catch (Exception ex2)
                {
                    ProductResponseEntity.IsSuccess = false;
                    ProductResponseEntity.Message = "API Error:" + ex2.Message;
                }
            }
            return ProductResponseEntity;
        }

        [HttpPost]
        public ResponseModel.UpsertCustomerResult UpsertCustomer()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var upsertCustomerResponseEntity = new ResponseModel.UpsertCustomerResult();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.UpsertCustomerFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var publishPRDUrl = ConfigHelper.GetSysParam<string>("UpSertCustomerURL");
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    var requestEntity = new RequestModel.UpsertCustomerRequestModel();
                    requestEntity.ApiSecretKey = NCAPIKey;
                    requestEntity.StoreId = storeId;
                    requestEntity.LanguageId = 1;
                    requestEntity.EmailAddress = pluginEntity.EmailAddress;
                    requestEntity.FirstName = pluginEntity.FirstName;
                    requestEntity.LastName = pluginEntity.LastName;
                    requestEntity.PhoneNumber = pluginEntity.PhoneNumber;
                    requestEntity.CustomerId = pluginEntity.CustomerId;
                    requestEntity.NCId = pluginEntity.NCId;
                    var requestData = JsonConvert.SerializeObject(requestEntity);
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var response = HttpRequestUrl.PostRequest(publishPRDUrl, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {
                            upsertCustomerResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).UpsertCustomerResult;

                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "UpsertCustomer").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                               .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                               .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (upsertCustomerResponseEntity.IsSuccess)
                            {
                                //DBDataAccess.InsertOrUpdateLog("information", "UpsertCustomer", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "UpsertCustomer", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), upsertCustomerResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", upsertCustomerResponseEntity.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        upsertCustomerResponseEntity.IsSuccess = false;
                        upsertCustomerResponseEntity.Message = "CRM API Key is not correct!";
                    }
                }
                else
                {
                    upsertCustomerResponseEntity.IsSuccess = false;
                    upsertCustomerResponseEntity.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    upsertCustomerResponseEntity.IsSuccess = false;
                    upsertCustomerResponseEntity.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "UpsertCustomer", requestDataStr.Replace("'", ""), requestDataStr.Replace("'", ""),
                    //        "", upsertCustomerResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "UpsertCustomer").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", upsertCustomerResponseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);
                }
                catch (Exception ex2)
                {
                    upsertCustomerResponseEntity.IsSuccess = false;
                    upsertCustomerResponseEntity.Message = "API Error:" + ex2.Message;
                }
            }
            return upsertCustomerResponseEntity;
        }

        /// <summary>
        /// To create and upadte Blackout calendar
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ResponseModel.BlackoutCalendar CreateOrUpdateBlackoutCalendar()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var blackoutCalendarResponseEntity = new ResponseModel.BlackoutCalendar();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.BlackoutCalendarFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var publishPRDUrl = "";
                    if (pluginEntity.RequestType == "update")
                    {
                        publishPRDUrl = ConfigHelper.GetSysParam<string>("UpdateBlackoutCalendarURL");
                    }
                    else
                    {
                        publishPRDUrl = ConfigHelper.GetSysParam<string>("CreateBlackoutCalendarURL");
                    }
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var requestEntity = new RequestModel.BlackoutCalendarRequestModel();
                        requestEntity.ApiSecretKey = NCAPIKey;
                        requestEntity.StoreId = storeId;
                        requestEntity.Name = pluginEntity.Name;
                        requestEntity.Remarks = pluginEntity.Remarks;
                        requestEntity.CustomerId = pluginEntity.CustomerId;
                        if (pluginEntity.RequestType == "update")
                        {
                            requestEntity.BlackoutCalendarId = pluginEntity.Id;
                        }
                        var requestData = JsonConvert.SerializeObject(requestEntity);
                        var response = HttpRequestUrl.PostRequest(publishPRDUrl, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {
                            if (pluginEntity.RequestType == "update")
                            {
                                blackoutCalendarResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).UpdateBlackoutCalendarResult;
                            }
                            else
                            {
                                blackoutCalendarResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).CreateBlackoutCalendarResult;
                            }

                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "CreateOrUpdateBlackoutCalendar").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                               .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                               .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (blackoutCalendarResponseEntity.IsSuccess)
                            {
                                //DBDataAccess.InsertOrUpdateLog("information", "CreateOrUpdateBlackoutCalendar", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "CreateOrUpdateBlackoutCalendar", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), blackoutCalendarResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", blackoutCalendarResponseEntity.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        blackoutCalendarResponseEntity.IsSuccess = false;
                        blackoutCalendarResponseEntity.Message = "CRM API Key is not correct!";
                    }
                }
                else
                {
                    blackoutCalendarResponseEntity.IsSuccess = false;
                    blackoutCalendarResponseEntity.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    blackoutCalendarResponseEntity.IsSuccess = false;
                    blackoutCalendarResponseEntity.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "CreateOrUpdateBlackoutCalendar", requestDataStr.Replace("'", ""), requestDataStr.Replace("'", ""),
                    //        "", blackoutCalendarResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "CreateOrUpdateBlackoutCalendar").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", blackoutCalendarResponseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);
                }
                catch (Exception ex2)
                {
                    blackoutCalendarResponseEntity.IsSuccess = false;
                    blackoutCalendarResponseEntity.Message = "API Error:" + ex2.Message;
                }
            }
            return blackoutCalendarResponseEntity;
        }

        /// <summary>
        /// To call NC api to create or update or delete
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ResponseModel.BlackoutCalendarDetailResult CreateOrUpdateOrDeleteBlackoutCalendarDetail()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var blackoutCalendarDetailResponseEntity = new ResponseModel.BlackoutCalendarDetailResult();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.BlackoutCalendarDetailFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var publishPRDUrl = "";
                    if (pluginEntity.RequestType == "create")
                    {
                        publishPRDUrl = ConfigHelper.GetSysParam<string>("CreateBlackoutCalendarDetailURL");
                    }
                    else if (pluginEntity.RequestType == "update")
                    {
                        publishPRDUrl = ConfigHelper.GetSysParam<string>("UpdateBlackoutCalendarDetailURL");
                    }
                    else if (pluginEntity.RequestType == "delete")
                    {
                        publishPRDUrl = ConfigHelper.GetSysParam<string>("DeleteBlackoutCalendarDetailURL");
                    }
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var requestEntity = new RequestModel.BlackoutCalendarDetailModel();
                        requestEntity.ApiSecretKey = NCAPIKey;
                        requestEntity.StoreId = storeId;
                        requestEntity.Name = pluginEntity.Name;
                        requestEntity.DateFrom = pluginEntity.DateFrom;
                        requestEntity.DateTo = pluginEntity.DateTo;
                        requestEntity.BlackoutCalendarId = pluginEntity.BlackoutCalendarId;
                        requestEntity.CustomerId = pluginEntity.CustomerId;
                        if (pluginEntity.RequestType != "create")
                        {
                            requestEntity.DetailId = pluginEntity.DetailId;
                        }
                        var requestData = JsonConvert.SerializeObject(requestEntity);
                        var response = HttpRequestUrl.PostRequest(publishPRDUrl, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {
                            if (pluginEntity.RequestType == "create")
                            {
                                blackoutCalendarDetailResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).CreateBlackoutCalendarDetailResult;
                            }
                            else if (pluginEntity.RequestType == "update")
                            {
                                blackoutCalendarDetailResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).UpdateBlackoutCalendarDetailResult;
                            }
                            else if (pluginEntity.RequestType == "delete")
                            {
                                blackoutCalendarDetailResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).DeleteBlackoutCalendarDetailResult;
                            }

                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "CreateOrUpdateOrDeleteBlackoutCalendarDetail").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                               .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                               .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (blackoutCalendarDetailResponseEntity.IsSuccess)
                            {
                                //DBDataAccess.InsertOrUpdateLog("information", "CreateOrUpdateOrDeleteBlackoutCalendarDetail", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "CreateOrUpdateOrDeleteBlackoutCalendarDetail", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), blackoutCalendarDetailResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", blackoutCalendarDetailResponseEntity.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        blackoutCalendarDetailResponseEntity.IsSuccess = false;
                        blackoutCalendarDetailResponseEntity.Message = "CRM API Key is not correct!";
                    }
                }
                else
                {
                    blackoutCalendarDetailResponseEntity.IsSuccess = false;
                    blackoutCalendarDetailResponseEntity.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    blackoutCalendarDetailResponseEntity.IsSuccess = false;
                    blackoutCalendarDetailResponseEntity.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "CreateOrUpdateOrDeleteBlackoutCalendarDetail", requestDataStr.Replace("'", ""), requestDataStr.Replace("'", ""),
                    //        "", blackoutCalendarDetailResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "CreateOrUpdateOrDeleteBlackoutCalendarDetail").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", blackoutCalendarDetailResponseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);
                }
                catch (Exception ex2)
                {
                    blackoutCalendarDetailResponseEntity.IsSuccess = false;
                    blackoutCalendarDetailResponseEntity.Message = "API Error:" + ex2.Message;
                }

            }
            return blackoutCalendarDetailResponseEntity;
        }

        /// <summary>
        /// To Call NC api to create and delete BlackoutCalendarProductMapping
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ResponseModel.BlackoutCalendarProductMappingResult CreateOrDeleteBlackoutCalendarProductMapping()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var blackoutCalendarProductMappingResponseEntity = new ResponseModel.BlackoutCalendarProductMappingResult();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.BlackoutCalendarProductMappingFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var publishPRDUrl = "";
                    if (pluginEntity.RequestType == "create")
                    {
                        publishPRDUrl = ConfigHelper.GetSysParam<string>("CreateBlackoutCalendarProductMappingURL");
                    }
                    else
                    {
                        publishPRDUrl = ConfigHelper.GetSysParam<string>("DeleteBlackoutCalendarProductMappingURL");
                    }
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var requestEntity = new RequestModel.BlackoutCalendarProductMappingModel();
                        requestEntity.ApiSecretKey = NCAPIKey;
                        requestEntity.StoreId = storeId;
                        requestEntity.BlackoutCalendarId = pluginEntity.BlackoutCalendarId;
                        requestEntity.ProductId = pluginEntity.ProductId;
                        requestEntity.CustomerId = pluginEntity.CustomerId;
                        if (pluginEntity.RequestType == "delete")
                        {
                            requestEntity.BlackoutCalendarProductId = pluginEntity.BlackoutCalendarProductId;
                        }
                        var requestData = JsonConvert.SerializeObject(requestEntity);
                        var response = HttpRequestUrl.PostRequest(publishPRDUrl, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {
                            if (pluginEntity.RequestType == "create")
                            {
                                blackoutCalendarProductMappingResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).CreateBlackoutCalendarProductMappingResult;
                            }
                            else
                            {
                                blackoutCalendarProductMappingResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).DeleteBlackoutCalendarProductMappingResult;
                            }

                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "CreateOrDeleteBlackoutCalendarProductMapping").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                               .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                               .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (blackoutCalendarProductMappingResponseEntity.IsSuccess)
                            {
                                //DBDataAccess.InsertOrUpdateLog("information", "CreateOrDeleteBlackoutCalendarProductMapping", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "CreateOrDeleteBlackoutCalendarProductMapping", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), blackoutCalendarProductMappingResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", blackoutCalendarProductMappingResponseEntity.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        blackoutCalendarProductMappingResponseEntity.IsSuccess = false;
                        blackoutCalendarProductMappingResponseEntity.Message = "CRM api key is not correct!";
                    }
                }
                else
                {
                    blackoutCalendarProductMappingResponseEntity.IsSuccess = false;
                    blackoutCalendarProductMappingResponseEntity.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    blackoutCalendarProductMappingResponseEntity.IsSuccess = false;
                    blackoutCalendarProductMappingResponseEntity.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "CreateOrDeleteBlackoutCalendarProductMapping", requestDataStr.Replace("'", ""), requestDataStr.Replace("'", ""),
                    //        "", blackoutCalendarProductMappingResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "CreateOrDeleteBlackoutCalendarProductMapping").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", blackoutCalendarProductMappingResponseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);
                }
                catch (Exception ex2)
                {
                    blackoutCalendarProductMappingResponseEntity.IsSuccess = false;
                    blackoutCalendarProductMappingResponseEntity.Message = "API Error:" + ex2.Message;
                }
            }
            return blackoutCalendarProductMappingResponseEntity;
        }

        /// <summary>
        /// this api will not use.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ResponseModel.SubscribeNewsletterResponse UpdateSubscribeNewsletter()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var subscribeNewsletterResponse = new ResponseModel.SubscribeNewsletterResponse();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.NewsletterUpdateFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var updateNewletterUrl = ConfigHelper.GetSysParam<string>("AddNewsletterSubscriptionURL");
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var requestEntity = new RequestModel.NewsletterUpdateModel();
                        requestEntity.ApiSecretKey = NCAPIKey;
                        requestEntity.StoreId = storeId;
                        requestEntity.LanguageId = 1;
                        requestEntity.Email = pluginEntity.EmailAddress;
                        requestEntity.Subscribe = pluginEntity.Subscribe;
                        requestEntity.CustomerId = pluginEntity.CustomerId;
                        var requestData = JsonConvert.SerializeObject(requestEntity);
                        var response = HttpRequestUrl.PostRequest(updateNewletterUrl, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {
                            subscribeNewsletterResponse = JsonConvert.DeserializeObject<ResponseModel.SubscribeNewsletterResponse>(response);
                            subscribeNewsletterResponse.IsSuccess = true;

                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "UpdateSubscribeNewsletter").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                               .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                               .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (subscribeNewsletterResponse.IsSuccess)
                            {
                                //DBDataAccess.InsertOrUpdateLog("information", "UpdateSubscribeNewsletter", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "UpdateSubscribeNewsletter", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), subscribeNewsletterResponse.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", subscribeNewsletterResponse.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        subscribeNewsletterResponse.IsSuccess = false;
                        subscribeNewsletterResponse.Message = "crmAPiKey is not correct!";
                    }
                }
                else
                {
                    subscribeNewsletterResponse.IsSuccess = false;
                    subscribeNewsletterResponse.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    subscribeNewsletterResponse.IsSuccess = false;
                    subscribeNewsletterResponse.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "UpdateSubscribeNewsletter", requestDataStr.Replace("'", ""), requestDataStr.Replace("'", ""),
                    //        "", subscribeNewsletterResponse.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "UpdateSubscribeNewsletter").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", subscribeNewsletterResponse.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);
                }
                catch (Exception ex2)
                {
                    subscribeNewsletterResponse.IsSuccess = false;
                    subscribeNewsletterResponse.Message = "API Error:" + ex2.Message;
                }
            }
            return subscribeNewsletterResponse;
        }

        /// <summary>
        /// Update EMP product URL by Product id. 
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ResponseModel.CommonResonseModel UpdateProductURL()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var responseEntity = new ResponseModel.CommonResonseModel();
            responseEntity.IsSuccess = false;
            var requestDataStr = HttpContext.Current.Request.Form[0];

            // Request received for update product URL log
            //DBDataAccess.InsertOrUpdateLog("information", "UpdateProductURL", requestDataStr.Replace("'", ""), "",
            //                "Request received and method initiated.", "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                var connectionStr = ConfigurationManager.ConnectionStrings["Xrm"].ToString();
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var requestEntity = JsonConvert.DeserializeObject<RequestModel.ProductUpdateURL>(requestDataStr);
                    using (var service = new CrmServiceClient(connectionStr))
                    {
                        var product = new Entity(Constants.WRSEntityName.Entity_Product, "wrs_id", requestEntity.ProductNCId);
                        product["wrs_empproducturl"] = requestEntity.ProductURL;
                        UpdateRequest request = new UpdateRequest
                        {
                            Target = product
                        };
                        service.Execute(request);
                        responseEntity.IsSuccess = true;
                        responseEntity.Message = "Update success, product url:" + requestEntity.ProductURL;
                        var commonSql = ApiDBSql.insertLog.Replace("@APIName", "UpdateProductURL").Replace("@CRMRequestInfo", "")
                          .Replace("@NCRequestInfo", requestDataStr.Replace("'", "")).Replace("@ResponseInfo", responseEntity.Message.Replace("'", ""))
                          .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        if (responseEntity.IsSuccess)
                        {
                            //DBDataAccess.InsertOrUpdateLog("information", "UpdateProductURL", requestDataStr.Replace("'", ""), requestDataStr.Replace("'", ""),
                            //responseEntity.Message.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                            DBDataAccess.InsertOrUpdateLog(commonSql);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    responseEntity.Message = ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "UpdateProductURL", requestDataStr.Replace("'", ""), "",
                    //        "", responseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "UpdateProductURL").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", responseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);
                }
                catch (Exception ex2)
                {
                    responseEntity.Message = ex2.Message;
                }
            }
            return responseEntity;
        }

        [HttpPost]
        public ResponseModel.UpdateResourceManagement UpdateResourceManager()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var updateResourceManagerResponseEntity = new ResponseModel.UpdateResourceManagement();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.ResourceManagementFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var requestUrlStr = "";
                    if (pluginEntity.RequestType == "AdjustRMEventStockQuantity")
                    {
                        requestUrlStr = ConfigHelper.GetSysParam<string>("AdjustRMEventStockQuantityURL");
                    }
                    else if (pluginEntity.RequestType == "UpdateRMEventStockActive")
                    {
                        requestUrlStr = ConfigHelper.GetSysParam<string>("UpdateRMEventStockActiveURL");
                    }
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var requestEntity = new RequestModel.ResourceManagementModel();
                        requestEntity.ApiSecretKey = NCAPIKey;
                        requestEntity.StoreId = storeId;
                        requestEntity.StockId = pluginEntity.StockId;
                        requestEntity.AdjustQuantity = pluginEntity.AdjustQuantity;
                        requestEntity.Active = pluginEntity.Active;
                        requestEntity.CustomerId = pluginEntity.CustomerId;
                        var requestData = JsonConvert.SerializeObject(requestEntity);
                        var response = HttpRequestUrl.PostRequest(requestUrlStr, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {
                            if (pluginEntity.RequestType == "AdjustRMEventStockQuantity")
                            {
                                updateResourceManagerResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).AdjustRMEventStockQuantityResult;
                            }
                            else if (pluginEntity.RequestType == "UpdateRMEventStockActive")
                            {
                                updateResourceManagerResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).UpdateRMEventStockActiveResult;
                            }
                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "UpdateResourceManager").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                            .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                            .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (updateResourceManagerResponseEntity.IsSuccess)
                            {

                                //DBDataAccess.InsertOrUpdateLog("information", "UpdateResourceManager", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "UpdateResourceManager", requestDataStr.Replace("'", ""), "",
                                //    "", updateResourceManagerResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", updateResourceManagerResponseEntity.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        updateResourceManagerResponseEntity.IsSuccess = false;
                        updateResourceManagerResponseEntity.Message = "CRM API Key is not correct!";
                    }
                }
                else
                {
                    updateResourceManagerResponseEntity.IsSuccess = false;
                    updateResourceManagerResponseEntity.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    updateResourceManagerResponseEntity.IsSuccess = false;
                    updateResourceManagerResponseEntity.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "UpdateResourceManager", requestDataStr.Replace("'", ""), "",
                    //        "", updateResourceManagerResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "UpdateResourceManager").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", updateResourceManagerResponseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);
                }
                catch (Exception ex2)
                {
                    updateResourceManagerResponseEntity.IsSuccess = false;
                    updateResourceManagerResponseEntity.Message = "API Error:" + ex2.Message;
                }

            }
            return updateResourceManagerResponseEntity;
        }

        [HttpPost]
        public ResponseModel.PriceCalendarResponse PriceCalendarEdit()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var requestDataStr = HttpContext.Current.Request.Form[0];
            var priceCalendarEditResponseEntity = new ResponseModel.PriceCalendarResponse();
            try
            {
                if (!string.IsNullOrEmpty(requestDataStr))
                {
                    var pluginEntity = JsonConvert.DeserializeObject<RequestModel.PriceCalendarFromPluginModel>(requestDataStr);
                    var crmAPiKey = ConfigHelper.GetSysParam<string>("CRMApiSecretKey");
                    var NCAPIKey = ConfigHelper.GetSysParam<string>("NCApiSecretKey");
                    var storeId = ConfigHelper.GetSysParam<int>("StoreId");
                    var requestUrlStr = ConfigHelper.GetSysParam<string>("PriceCalendarEditURL");
                    if (crmAPiKey == pluginEntity.ApiSecretKey)
                    {
                        var requestEntity = new RequestModel.PriceCalendarModel();
                        requestEntity.apiSecretKey = NCAPIKey;
                        requestEntity.storeId = storeId;
                        requestEntity.calendarId = pluginEntity.CalendarId;
                        requestEntity.newPriceGroupId = pluginEntity.NewPriceGroupId;
                        requestEntity.customerId = pluginEntity.CustomerId;
                        requestEntity.calendarTypeId = pluginEntity.CalendarTypeId;
                        if (pluginEntity.CalendarDate != null)
                        {
                            requestEntity.calendarDate = pluginEntity.CalendarDate.Value.ToLocalTime();
                        }
                        var requestData = EntityToJson(requestEntity);
                        var response = HttpRequestUrl.PostRequest(requestUrlStr, requestData, 60000, "UTF-8", "application/json");
                        if (!string.IsNullOrEmpty(response))
                        {

                            priceCalendarEditResponseEntity = JsonConvert.DeserializeObject<ResponseModel.Root>(response).PriceCalendarEditResult;

                            var commonSql = ApiDBSql.insertLog.Replace("@APIName", "PriceCalendarEdit").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                               .Replace("@NCRequestInfo", requestData.Replace("'", "")).Replace("@ResponseInfo", response.Replace("'", ""))
                               .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            if (priceCalendarEditResponseEntity.IsSuccess)
                            {
                                //DBDataAccess.InsertOrUpdateLog("information", "PriceCalendarEdit", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), "", startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "information").Replace("@ExceptionLog", "");
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                            else
                            {
                                //DBDataAccess.InsertOrUpdateLog("exception", "PriceCalendarEdit", requestDataStr.Replace("'", ""), requestData.Replace("'", ""),
                                //    response.Replace("'", ""), priceCalendarEditResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                                commonSql = commonSql.Replace("@LogType", "exception").Replace("@ExceptionLog", priceCalendarEditResponseEntity.Message.Replace("'", ""));
                                DBDataAccess.InsertOrUpdateLog(commonSql);
                            }
                        }
                    }
                    else
                    {
                        priceCalendarEditResponseEntity.IsSuccess = false;
                        priceCalendarEditResponseEntity.Message = "CRM API Key is not correct!";
                    }
                }
                else
                {
                    priceCalendarEditResponseEntity.IsSuccess = false;
                    priceCalendarEditResponseEntity.Message = "Request Data can not as null";
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionPolicyExtension.HandleExceptionForLogOnly(ex);
                    priceCalendarEditResponseEntity.IsSuccess = false;
                    priceCalendarEditResponseEntity.Message = "API Error:" + ex.Message;

                    //DBDataAccess.InsertOrUpdateLog("exception", "PriceCalendarEdit", requestDataStr.Replace("'", ""), "",
                    //        "", priceCalendarEditResponseEntity.Message.Replace("'", ""), startTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    var commonSql = ApiDBSql.insertExceptionLog.Replace("@APIName", "PriceCalendarEdit").Replace("@CRMRequestInfo", requestDataStr.Replace("'", ""))
                   .Replace("@startTime", startTime).Replace("@EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                   .Replace("@ExceptionLog", priceCalendarEditResponseEntity.Message.Replace("'", ""));
                    DBDataAccess.InsertOrUpdateLog(commonSql);
                }
                catch (Exception ex2)
                {
                    priceCalendarEditResponseEntity.IsSuccess = false;
                    priceCalendarEditResponseEntity.Message = "API Error:" + ex2.Message;
                }
            }
            return priceCalendarEditResponseEntity;
        }

        public static string EntityToJson(object requestEntity)
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
    }
}