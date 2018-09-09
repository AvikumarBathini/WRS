using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using WRS.Xrm;

namespace OrderImport
{
    class Program
    {
        static int batchSize = int.Parse(ConfigurationManager.AppSettings["BatchSize"]);
        private string NCId;
        const string OrderNote = "OrderNote";
        const string OrderStoreNotesProcName = "CrmGetOrderNotes";
        const string OrderItemStoreProcName = "CrmGetOrderItemDetails";
        const string OrderItem = "OrderItem";
        static void Main(string[] args)
        {
            string alternateKeyLogicalName = "wrs_id";
            const string DiscountUsageStoreProcName = "CrmGetDiscountUsageDetails";
            const string OrderStoreProcName = "CrmGetOrderDetails";
            const string Order = "Order";
            const string DiscountUsageHistory = "DiscountUsageHistory";
            Program p = new Program();

            p.UpsertOrders(Order, p, OrderStoreProcName, alternateKeyLogicalName);

            //p.CreateOrderNotes(OrderNote, p, OrderStoreNotesProcName, alternateKeyLogicalName);

            p.UpsertDiscountUsageHistory(DiscountUsageHistory, p, DiscountUsageStoreProcName, alternateKeyLogicalName);

            //p.UpsertOrderItems(OrderItem, p, OrderItemStoreProcName, alternateKeyLogicalName);
        }

        private void UpsertDiscountUsageHistory(string discountUsage, Program p, string storeProc, string alternateKeyLogicalName)
        {
            try
            {
                DataTable DiscountUsageTable = RetrieveRecordsFromDB(storeProc);
                if (DiscountUsageTable != null && DiscountUsageTable.Rows.Count > 0)
                {
                    string primaryKeyLogicalName = "wrs_discountusagehistoryid";
                    List<EntityCollection> _lisEntityCollection = GetEntityCollection(DiscountUsageTable, PrepareDiscountUsageObject);
                    if (_lisEntityCollection != null)
                        foreach (EntityCollection ec in _lisEntityCollection)
                        {
                            p.CrmExecuteMultiple(ec, p, discountUsage, wrs_discountusagehistory.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                        }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private List<EntityCollection> GetEntityCollection(DataTable table, Func<DataRow, Entity> myMethod)
        {
            int totalCount = table.Rows.Count;
            if (totalCount == 0)
                return null;
            int requests = (totalCount / batchSize) + 1; //Maximum parallel requests
            List<EntityCollection> _lisEntityCollection = new List<EntityCollection>();
            for (int i = 0; i < requests; i++)
            {
                //Add (requests) set of EntityCollection in _lisEntityCollection
                _lisEntityCollection.Add(new EntityCollection());
            }

            //Add maximum of (batchCount) number of records in each _lisEntityCollection -> EntityCollection object
            for (int j = 0; j < totalCount; j++)
            {
                try
                {
                    DataRow dr = table.Rows[j];
                    Entity _record = myMethod(dr);
                    if (_record != null)
                        _lisEntityCollection[j % requests].Entities.Add(_record);
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
            return _lisEntityCollection;
        }

        private void UpsertOrders(string Order, Program p, string storeProc, string alternateKeyLogicalName)
        {
            DataTable OrderTable = RetrieveRecordsFromDB(storeProc);
            if (OrderTable != null && OrderTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "salesorderid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(OrderTable, PrepareOrderObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Order, SalesOrder.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName, false, false, false, true);
                    }
            }
        }

        private void CreateOrderNotes(string Order, Program p, string storeProc, string alternateKeyLogicalName, List<string> orderColl)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Id");
            foreach (string e in orderColl)
            {
                dt.Rows.Add(int.Parse(e));
            }
            DataTable OrderNotesTable = RetrieveRecordsFromDB(storeProc, dt);
            if (OrderNotesTable != null && OrderNotesTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "wrs_ordernoteid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(OrderNotesTable, PrepareOrderNoteObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, Order, wrs_ordernote.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName, true);
                    }
            }
        }

        private void UpsertOrderItems(string OrderItem, Program p, string storeProc, string alternateKeyLogicalName, List<string> orderColl)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Id");
            foreach (string e in orderColl)
            {
                dt.Rows.Add(int.Parse(e));
            }
            DataTable OrderItemTable = RetrieveRecordsFromDB(storeProc, dt);

            if (OrderItemTable != null && OrderItemTable.Rows.Count > 0)
            {
                string primaryKeyLogicalName = "salesorderdetailid";
                List<EntityCollection> _lisEntityCollection = GetEntityCollection(OrderItemTable, PrepareOrderItemObject);
                if (_lisEntityCollection != null)
                    foreach (EntityCollection ec in _lisEntityCollection)
                    {
                        p.CrmExecuteMultiple(ec, p, OrderItem, SalesOrderDetail.EntityLogicalName, alternateKeyLogicalName, primaryKeyLogicalName);
                    }
            }
        }

        public static void ErrorLog(string sErrMsg)
        {
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            string sErrorTime = sYear + sMonth + sDay;

            string sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";
            StreamWriter sw = new StreamWriter("C:/" + sErrorTime + ".txt", true);
            sw.WriteLine(sLogFormat + sErrMsg);
            sw.Flush();
            sw.Close();
        }

        public void UpdateSyncStatus(string entityName, string l)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var update_item = new SqlCommand("CrmUpdateAuditLogSyncStatus", conn))
            {
                update_item.CommandType = CommandType.StoredProcedure;
                update_item.Parameters.Add("@EntityId", SqlDbType.NVarChar).Value = l.ToString();
                update_item.Parameters.Add("@EntityName", SqlDbType.NVarChar).SqlValue = entityName;
                conn.Open();
                update_item.ExecuteNonQuery();
                conn.Close();
            }
        }

        public string RetrieveAlternateKey(Guid accountID, string entityLogicalName, string alternate, string primary)
        {
            QueryExpression accountbyID = new QueryExpression(entityLogicalName);
            accountbyID.Criteria.AddCondition(new ConditionExpression(primary, ConditionOperator.Equal, accountID));
            accountbyID.ColumnSet.Columns.Add(alternate);
            Entity e = serviceClient().Retrieve(entityLogicalName, accountID, accountbyID.ColumnSet);
            if (e != null && e.Attributes[alternate] != null && entityLogicalName != TransactionCurrency.EntityLogicalName)
                return e.GetAttributeValue<int>(alternate).ToString();
            else if (e != null && e.Attributes[alternate] != null && entityLogicalName.Equals(TransactionCurrency.EntityLogicalName))
                return e.GetAttributeValue<string>(alternate);
            return "";
        }

        public static IOrganizationService serviceClient()
        {
            CrmServiceClient crmConnD = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMD"].ConnectionString);
            crmConnD.OrganizationServiceProxy.Timeout = new TimeSpan(0, 20, 0);
            IOrganizationService crmServiceD = crmConnD.OrganizationServiceProxy;
            return crmServiceD;
        }

        public DataTable RetrieveRecordsFromDB(string storeProc, DataTable dt = null)
        {
            DataTable DBTable = new DataTable();
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQL"].ConnectionString))
            using (var command = new SqlCommand(storeProc, conn))
            using (var da = new SqlDataAdapter(command))
            {
                try
                {

                    command.CommandType = CommandType.StoredProcedure;
                    if (dt != null)
                    {
                        SqlParameter tvparam = command.Parameters.AddWithValue("@List", dt);
                        tvparam.SqlDbType = SqlDbType.Structured;
                    }
                    da.Fill(DBTable);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }

            return DBTable;
        }

        private Entity PrepareDiscountUsageObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value)
            {

                Entity DiscountUsageToBeCreated = new Entity(wrs_discountusagehistory.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_discountusagehistory DiscountUsageToBeCreatedFromDB = DiscountUsageToBeCreated.ToEntity<wrs_discountusagehistory>();
                if (dr["DiscountId"] != DBNull.Value && int.Parse(dr["DiscountId"].ToString()) > 0)
                    DiscountUsageToBeCreatedFromDB.wrs_discountid = new EntityReference(wrs_discount.EntityLogicalName, "wrs_id", int.Parse(dr["DiscountId"].ToString()));
                DiscountUsageToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                if (dr["OrderId"] != DBNull.Value && int.Parse(dr["OrderId"].ToString()) > 0)
                    DiscountUsageToBeCreatedFromDB.wrs_orderid = new EntityReference(SalesOrder.EntityLogicalName, "wrs_id", int.Parse(dr["OrderId"].ToString()));

                if (dr["CreatedOnUtc"] != DBNull.Value)
                    DiscountUsageToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)(dr["CreatedOnUtc"]);
                DiscountUsageToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                return DiscountUsageToBeCreatedFromDB;
            }
            return null;
        }

        public Entity PrepareOrderObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity orderToBeCreated = new Entity(SalesOrder.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                SalesOrder orderToBeCreatedFromDB = orderToBeCreated.ToEntity<SalesOrder>();
                if (dr["StoreId"] != DBNull.Value && int.Parse(dr["StoreId"].ToString()) != 0)
                    orderToBeCreatedFromDB.wrs_storeid = new EntityReference(wrs_store.EntityLogicalName, "wrs_id", int.Parse(dr["StoreId"].ToString()));
                orderToBeCreatedFromDB.Name = "Order# " + dr["Id"].ToString();
                orderToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                if (dr["PriceGroupId"] != DBNull.Value && int.Parse(dr["PriceGroupId"].ToString()) > 0)
                    orderToBeCreatedFromDB.PriceLevelId = new EntityReference(PriceLevel.EntityLogicalName, "wrs_id", int.Parse(dr["PriceGroupId"].ToString()));
                else if (dr["PriceGroupId"] != DBNull.Value && int.Parse(dr["PriceGroupId"].ToString()).Equals(0))
                    orderToBeCreatedFromDB.PriceLevelId = new EntityReference(PriceLevel.EntityLogicalName, "wrs_id", 40);

                orderToBeCreatedFromDB.BillTo_City = dr["CityBill"] != DBNull.Value ? dr["CityBill"].ToString() : "";
                orderToBeCreatedFromDB.BillTo_PostalCode = dr["ZipPostalCodeBill"] != DBNull.Value ? dr["ZipPostalCodeBill"].ToString() : "";
                orderToBeCreatedFromDB.BillTo_Fax = dr["FaxNumberBill"] != DBNull.Value ? dr["FaxNumberBill"].ToString() : "";
                orderToBeCreatedFromDB.BillTo_Telephone = dr["PhoneNumberBill"] != DBNull.Value ? dr["PhoneNumberBill"].ToString() : "";
                orderToBeCreatedFromDB.BillTo_Line1 = dr["Address1Bill"] != DBNull.Value ? dr["Address1Bill"].ToString() : "";
                orderToBeCreatedFromDB.BillTo_Line2 = dr["Address2Bill"] != DBNull.Value ? dr["Address2Bill"].ToString() : "";
                if (dr["CountryIdBill"] != DBNull.Value && dr["CountryIdBill"].ToString() != string.Empty)
                    orderToBeCreatedFromDB.BillTo_Country = dr["CountryNameBill"] != DBNull.Value ? dr["CountryNameBill"].ToString() : "";
                if (dr["StateProvinceIdBill"] != DBNull.Value && dr["StateProvinceIdBill"].ToString() != string.Empty)
                    orderToBeCreatedFromDB.BillTo_StateOrProvince = dr["StateNameBill"] != DBNull.Value ? dr["StateNameBill"].ToString() : "";

                orderToBeCreatedFromDB.ShipTo_City = dr["CityShip"] != DBNull.Value ? dr["CityShip"].ToString() : "";
                orderToBeCreatedFromDB.ShipTo_PostalCode = dr["ZipPostalCodeShip"] != DBNull.Value ? dr["ZipPostalCodeShip"].ToString() : "";
                orderToBeCreatedFromDB.ShipTo_Fax = dr["FaxNumberShip"] != DBNull.Value ? dr["FaxNumberShip"].ToString() : "";
                orderToBeCreatedFromDB.ShipTo_Telephone = dr["PhoneNumberShip"] != DBNull.Value ? dr["PhoneNumberShip"].ToString() : "";
                orderToBeCreatedFromDB.ShipTo_Line1 = dr["Address1Ship"] != DBNull.Value ? dr["Address1Ship"].ToString() : "";
                orderToBeCreatedFromDB.ShipTo_Line2 = dr["Address2Ship"] != DBNull.Value ? dr["Address2Ship"].ToString() : "";
                if (dr["CountryIdShip"] != DBNull.Value && dr["CountryIdShip"].ToString() != string.Empty)
                    orderToBeCreatedFromDB.ShipTo_Country = dr["CountryNameShip"] != DBNull.Value ? dr["CountryNameShip"].ToString() : "";
                if (dr["StateProvinceIdShip"] != DBNull.Value && dr["StateProvinceIdShip"].ToString() != string.Empty)
                    orderToBeCreatedFromDB.ShipTo_StateOrProvince = dr["StateNameShip"] != DBNull.Value ? dr["StateNameShip"].ToString() : "";

                if (dr["PickupAddressId"] != DBNull.Value && int.Parse(dr["PickupAddressId"].ToString()) != 0)
                    orderToBeCreatedFromDB.wrs_pickupaddressid = int.Parse(dr["PickupAddressId"].ToString());

                if (dr["CustomerId"] != DBNull.Value && int.Parse(dr["CustomerId"].ToString()) != 0)
                    orderToBeCreatedFromDB.CustomerId = new EntityReference(Contact.EntityLogicalName, "wrs_id", int.Parse(dr["CustomerId"].ToString()));

                orderToBeCreatedFromDB.wrs_paymentmethodsystemname = dr["PaymentMethodSystemName"] != DBNull.Value ? dr["PaymentMethodSystemName"].ToString() : "";
                orderToBeCreatedFromDB.wrs_customercurrencycode = dr["CustomerCurrencyCode"] != DBNull.Value ? dr["CustomerCurrencyCode"].ToString() : "";
                if (dr["CurrencyRate"] != DBNull.Value && decimal.Parse(dr["CurrencyRate"].ToString()) != 0)
                    orderToBeCreatedFromDB.wrs_currencyrate = decimal.Parse(dr["CurrencyRate"].ToString());

                if (dr["CustomerTaxDisplayTypeId"] != DBNull.Value && int.Parse(dr["CustomerTaxDisplayTypeId"].ToString()) != 0)
                    orderToBeCreatedFromDB.wrs_customertaxdisplaytypeid = int.Parse(dr["CustomerTaxDisplayTypeId"].ToString());

                if (dr["OrderSubtotalInclTax"] != DBNull.Value && decimal.Parse(dr["OrderSubtotalInclTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_ordersubtotalincltax = new Money(decimal.Parse(dr["OrderSubtotalInclTax"].ToString()));

                if (dr["OrderSubtotalExclTax"] != DBNull.Value && decimal.Parse(dr["OrderSubtotalExclTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_ordersubtotalexcltax = new Money(decimal.Parse(dr["OrderSubtotalExclTax"].ToString()));

                if (dr["OrderSubTotalDiscountInclTax"] != DBNull.Value && decimal.Parse(dr["OrderSubTotalDiscountInclTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_ordersubtotaldiscountincltax = new Money(decimal.Parse(dr["OrderSubTotalDiscountInclTax"].ToString()));

                if (dr["OrderSubTotalDiscountExclTax"] != DBNull.Value && decimal.Parse(dr["OrderSubTotalDiscountExclTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_ordersubtotaldiscountexcltax = new Money(decimal.Parse(dr["OrderSubTotalDiscountExclTax"].ToString()));

                if (dr["OrderShippingInclTax"] != DBNull.Value && decimal.Parse(dr["OrderShippingInclTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_ordershippingincltax = new Money(decimal.Parse(dr["OrderShippingInclTax"].ToString()));

                if (dr["OrderShippingExclTax"] != DBNull.Value && decimal.Parse(dr["OrderShippingExclTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_ordershippingexcltax = new Money(decimal.Parse(dr["OrderShippingExclTax"].ToString()));

                if (dr["PaymentMethodAdditionalFeeInclTax"] != DBNull.Value && decimal.Parse(dr["PaymentMethodAdditionalFeeInclTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_paymentmethodadditionalfeeincltax = new Money(decimal.Parse(dr["PaymentMethodAdditionalFeeInclTax"].ToString()));

                if (dr["PaymentMethodAdditionalFeeExclTax"] != DBNull.Value && decimal.Parse(dr["PaymentMethodAdditionalFeeExclTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_paymentmethodadditionalfeeexcltax = new Money(decimal.Parse(dr["PaymentMethodAdditionalFeeExclTax"].ToString()));

                orderToBeCreatedFromDB.wrs_taxrates = dr["TaxRates"] != DBNull.Value ? dr["TaxRates"].ToString() : "";

                if (dr["OrderTax"] != DBNull.Value && decimal.Parse(dr["OrderTax"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_TotalTax = new Money(decimal.Parse(dr["OrderTax"].ToString()));

                if (dr["OrderDiscount"] != DBNull.Value && decimal.Parse(dr["OrderDiscount"].ToString()) > 0)
                    orderToBeCreatedFromDB.DiscountAmount = new Money(decimal.Parse(dr["OrderDiscount"].ToString()));

                if (dr["OrderTotal"] != DBNull.Value && decimal.Parse(dr["OrderTotal"].ToString()) > 0)
                    orderToBeCreatedFromDB.TotalAmount = new Money(decimal.Parse(dr["OrderTotal"].ToString()));

                if (dr["RefundedAmount"] != DBNull.Value && decimal.Parse(dr["RefundedAmount"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_refundedamount = new Money(decimal.Parse(dr["RefundedAmount"].ToString()));

                if (dr["RewardPointsHistoryEntryId"] != DBNull.Value && int.Parse(dr["RewardPointsHistoryEntryId"].ToString()) != 0)
                    orderToBeCreatedFromDB.wrs_rewardpointshistoryentryid = int.Parse(dr["RewardPointsHistoryEntryId"].ToString());

                orderToBeCreatedFromDB.wrs_checkoutattributedescription = dr["CheckoutAttributeDescription"] != DBNull.Value ? dr["CheckoutAttributeDescription"].ToString() : "";
                orderToBeCreatedFromDB.wrs_checkoutattributexml = dr["CheckoutAttributesXml"] != DBNull.Value ? dr["CheckoutAttributesXml"].ToString() : "";

                orderToBeCreatedFromDB.wrs_customerip = dr["CustomerIp"] != DBNull.Value ? dr["CustomerIp"].ToString() : "";
                orderToBeCreatedFromDB.wrs_authorizationtransactionid = dr["AuthorizationTransactionId"] != DBNull.Value ? dr["AuthorizationTransactionId"].ToString() : "";
                orderToBeCreatedFromDB.wrs_authorizationtransactioncode = dr["AuthorizationTransactionCode"] != DBNull.Value ? dr["AuthorizationTransactionCode"].ToString() : "";
                orderToBeCreatedFromDB.wrs_authorizationtransactionresult = dr["AuthorizationTransactionResult"] != DBNull.Value ? dr["AuthorizationTransactionResult"].ToString() : "";
                orderToBeCreatedFromDB.wrs_capturetransactionid = dr["CaptureTransactionId"] != DBNull.Value ? dr["CaptureTransactionId"].ToString() : "";
                orderToBeCreatedFromDB.wrs_capturetransactionresult = dr["CaptureTransactionResult"] != DBNull.Value ? dr["CaptureTransactionResult"].ToString() : "";
                orderToBeCreatedFromDB.wrs_subscriptiontransactionid = dr["SubscriptionTransactionId"] != DBNull.Value ? dr["SubscriptionTransactionId"].ToString() : "";
                if (dr["PaidDateUtc"] != DBNull.Value)
                    orderToBeCreatedFromDB.wrs_paiddate = (DateTime?)dr["PaidDateUtc"];
                if (dr["ShippingMethod"] != DBNull.Value)
                    orderToBeCreatedFromDB.ShippingMethodCode = new OptionSetValue(int.Parse(dr["ShippingMethod"].ToString()));
                orderToBeCreatedFromDB.wrs_shippingratecomputationmethodsystemname = dr["ShippingRateComputationMethodSystemName"] != DBNull.Value ? dr["ShippingRateComputationMethodSystemName"].ToString() : "";
                orderToBeCreatedFromDB.wrs_deleted = dr["Deleted"] != DBNull.Value ? (bool)dr["Deleted"] : false;
                if (dr["CreatedOnUtc"] != DBNull.Value)
                    orderToBeCreatedFromDB.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];
                orderToBeCreatedFromDB.OrderNumber = dr["CustomOrderNumber"] != DBNull.Value ? dr["CustomOrderNumber"].ToString() : "";
                orderToBeCreatedFromDB.wrs_galaxyordernumber = dr["GalaxyOrderNumber"] != DBNull.Value ? dr["GalaxyOrderNumber"].ToString() : "";
                orderToBeCreatedFromDB.wrs_wcforderconfirmationid = dr["WCFOrderConfirmationId"] != DBNull.Value ? dr["WCFOrderConfirmationId"].ToString() : "";

                switch (int.Parse((dr["OrderStatusId"]).ToString()))
                {
                    case 10:
                        orderToBeCreatedFromDB.StatusCode = new OptionSetValue(2);
                        break;

                    case 20:
                        orderToBeCreatedFromDB.StatusCode = new OptionSetValue(1);
                        break;

                    case 30:
                        orderToBeCreatedFromDB.StatusCode = new OptionSetValue(690970000);
                        break;

                    case 35:
                        orderToBeCreatedFromDB.StatusCode = new OptionSetValue(690970001);
                        break;

                    case 40:
                        orderToBeCreatedFromDB.StatusCode = new OptionSetValue(690970002);
                        break;

                }
                if (dr["PaymentStatusId"] != DBNull.Value && int.Parse(dr["PaymentStatusId"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_PaymentStatus = new OptionSetValue(int.Parse(dr["PaymentStatusId"].ToString()));

                if (dr["ShippingStatusId"] != DBNull.Value && int.Parse(dr["ShippingStatusId"].ToString()) > 0)
                    orderToBeCreatedFromDB.wrs_ShippingStatus = new OptionSetValue(int.Parse(dr["ShippingStatusId"].ToString()));

                if (ConfigurationManager.AppSettings["SGCurrency"] != null)
                {
                    string varSGCurrentyGuid = ConfigurationManager.AppSettings["SGCurrency"].ToString();
                    orderToBeCreatedFromDB.TransactionCurrencyId = new EntityReference(TransactionCurrency.EntityLogicalName, new Guid(varSGCurrentyGuid));
                }

                return orderToBeCreatedFromDB;
            }
            return null;
        }

        private Entity PrepareOrderNoteObject(DataRow dr)
        {
            if (dr["Id"] != DBNull.Value && int.Parse(dr["Id"].ToString()) > 0)
            {
                Entity ordernote = new Entity(wrs_ordernote.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                wrs_ordernote ordernoteObj = ordernote.ToEntity<wrs_ordernote>();
                ordernoteObj.wrs_id = int.Parse(dr["Id"].ToString());
                ordernoteObj.wrs_name = dr["Note"] != DBNull.Value ? dr["Note"].ToString() : "";
                if (dr["OrderId"] != DBNull.Value && int.Parse(dr["OrderId"].ToString()) != 0)
                    ordernoteObj.wrs_orderid = new EntityReference(SalesOrder.EntityLogicalName, "wrs_id", int.Parse(dr["OrderId"].ToString()));

                ordernoteObj.wrs_displaytocustomer = dr["DisplayToCustomer"] != DBNull.Value ? (bool)dr["DisplayToCustomer"] : false;
                if (dr["CreatedOnUtc"] != DBNull.Value)
                    ordernoteObj.OverriddenCreatedOn = (DateTime?)dr["CreatedOnUtc"];

                return ordernoteObj;
            }
            return null;
        }

        public Entity PrepareOrderItemObject(DataRow dr)
        {

            if (dr["Id"] != DBNull.Value)
            {
                decimal DiscountAmountExclTax = 0;
                decimal DiscountAmountInclTax = 0;
                int quantity = 0;
                Entity orderitemToBeCreated = new Entity(SalesOrderDetail.EntityLogicalName, "wrs_id", int.Parse(dr["Id"].ToString()));
                SalesOrderDetail orderitemToBeCreatedFromDB = orderitemToBeCreated.ToEntity<SalesOrderDetail>();
                orderitemToBeCreatedFromDB.wrs_id = int.Parse(dr["Id"].ToString());
                orderitemToBeCreatedFromDB.IsPriceOverridden = true;

                if (dr["DiscountAmountInclTax"] != DBNull.Value && decimal.Parse(dr["DiscountAmountInclTax"].ToString()) > 0)
                {
                    DiscountAmountInclTax = decimal.Parse(dr["DiscountAmountInclTax"].ToString());
                    orderitemToBeCreatedFromDB.wrs_discountamountincltax = new Money(decimal.Parse(dr["DiscountAmountInclTax"].ToString()));
                    orderitemToBeCreatedFromDB.ManualDiscountAmount = new Money(decimal.Parse(dr["DiscountAmountInclTax"].ToString()));
                }
                if (dr["DiscountAmountExclTax"] != DBNull.Value && decimal.Parse(dr["DiscountAmountExclTax"].ToString()) > 0)
                {
                    DiscountAmountExclTax = decimal.Parse(dr["DiscountAmountExclTax"].ToString());
                    orderitemToBeCreatedFromDB.wrs_discountamountexcltax = new Money(decimal.Parse(dr["DiscountAmountExclTax"].ToString()));
                }

                if (dr["Quantity"] != DBNull.Value && int.Parse(dr["Quantity"].ToString()) != 0)
                {
                    quantity = int.Parse(dr["Quantity"].ToString());
                    orderitemToBeCreatedFromDB.Quantity = int.Parse(dr["Quantity"].ToString());
                }

                if (dr["OrderId"] != DBNull.Value && int.Parse(dr["OrderId"].ToString()) != 0)
                    orderitemToBeCreatedFromDB.SalesOrderId = new EntityReference(SalesOrder.EntityLogicalName, "wrs_id", int.Parse(dr["OrderId"].ToString()));

                if (dr["ProductId"] != DBNull.Value && int.Parse(dr["ProductId"].ToString()) != 0)
                    orderitemToBeCreatedFromDB.ProductId = new EntityReference(Product.EntityLogicalName, "wrs_id", int.Parse(dr["ProductId"].ToString()));

                if (dr["UnitPriceInclTax"] != DBNull.Value && decimal.Parse(dr["UnitPriceInclTax"].ToString()) > 0)
                {
                    orderitemToBeCreatedFromDB.wrs_unitpriceincltax = new Money((DiscountAmountInclTax != 0 ? (DiscountAmountInclTax / quantity) : 0) + decimal.Parse(dr["UnitPriceInclTax"].ToString()));
                    orderitemToBeCreatedFromDB.PricePerUnit = new Money((DiscountAmountInclTax != 0 ? (DiscountAmountInclTax / quantity) : 0) + decimal.Parse(dr["UnitPriceInclTax"].ToString()));
                }

                if (dr["UnitPriceExclTax"] != DBNull.Value && decimal.Parse(dr["UnitPriceExclTax"].ToString()) > 0)
                {
                    orderitemToBeCreatedFromDB.wrs_unitpriceexcltax = new Money((DiscountAmountExclTax != 0 ? (DiscountAmountExclTax / quantity) : 0) + decimal.Parse(dr["UnitPriceExclTax"].ToString()));
                }

                if (dr["PriceInclTax"] != DBNull.Value && decimal.Parse(dr["PriceInclTax"].ToString()) > 0)
                {
                    orderitemToBeCreatedFromDB.wrs_priceincltax = new Money((DiscountAmountExclTax != 0 ? (DiscountAmountExclTax / quantity) : 0) + decimal.Parse(dr["PriceInclTax"].ToString()));
                }

                if (dr["PriceExclTax"] != DBNull.Value && decimal.Parse(dr["PriceExclTax"].ToString()) > 0)
                {
                    orderitemToBeCreatedFromDB.wrs_priceexcltax = new Money((DiscountAmountInclTax != 0 ? (DiscountAmountInclTax / quantity) : 0) + decimal.Parse(dr["PriceExclTax"].ToString()));
                }

                if (dr["OriginalProductCost"] != DBNull.Value && decimal.Parse(dr["OriginalProductCost"].ToString()) > 0)
                    orderitemToBeCreatedFromDB.wrs_originalproductcost = new Money(decimal.Parse(dr["OriginalProductCost"].ToString()));


                orderitemToBeCreatedFromDB.wrs_attributedescription = dr["AttributeDescription"] != DBNull.Value ? dr["AttributeDescription"].ToString() : "";
                orderitemToBeCreatedFromDB.wrs_attributesxml = dr["AttributesXml"] != DBNull.Value ? dr["AttributesXml"].ToString() : "";
                orderitemToBeCreatedFromDB.UoMId = new EntityReference("uom", new Guid(ConfigurationManager.AppSettings["DefaultUoMId"]));
                if (dr["DownloadCount"] != DBNull.Value && int.Parse(dr["DownloadCount"].ToString()) != 0)
                    orderitemToBeCreatedFromDB.wrs_downloadcount = int.Parse(dr["DownloadCount"].ToString());

                if (dr["CustId"] != DBNull.Value && int.Parse(dr["CustId"].ToString()) != 0)
                    orderitemToBeCreatedFromDB.wrs_Customer = new EntityReference(Contact.EntityLogicalName, "wrs_id", int.Parse(dr["CustId"].ToString()));

                return orderitemToBeCreatedFromDB;
            }
            return null;

        }

        public UpsertRequest CreateUpsertRequest(Entity target)
        {
            return new UpsertRequest()
            {
                Target = target
            };
        }

        public void CrmExecuteMultiple(EntityCollection ecoll, Program p, string table, string entityLogicalName, string alternate, string primary, bool isOrderNote = false, bool isPriceListItemCreate = false, bool isPriceListItemUpdate = false, bool isOrder = false)
        {
            string mainSuccessLog = string.Empty;
            try
            {
                ExecuteMultipleRequest multipleReq = new ExecuteMultipleRequest()
                {
                    Settings = new ExecuteMultipleSettings()
                    {
                        ContinueOnError = true,
                        ReturnResponses = true
                    },
                    Requests = new OrganizationRequestCollection()
                };
                foreach (Entity e in ecoll.Entities)
                {
                    multipleReq.Requests.Add(CreateUpsertRequest(e));
                }
                ExecuteMultipleResponse emrResponse = (ExecuteMultipleResponse)Program.serviceClient().Execute(multipleReq);
                List<string> existingRecords = new List<string>();

                foreach (ExecuteMultipleResponseItem e in emrResponse.Responses)
                {
                    if (e.Fault == null)
                    {
                        string alternateKey = "";
                        alternateKey = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
                        existingRecords.Add(alternateKey);
                        if (mainSuccessLog != string.Empty)
                        {
                            mainSuccessLog = mainSuccessLog + ",";
                        }
                        mainSuccessLog = mainSuccessLog + "('" + DateTime.Now + "','Success','" + table + "','" + alternateKey + "')";
                    }

                    else if (e.Fault != null)
                    {
                        NCId = ((UpsertRequest)(multipleReq.Requests[e.RequestIndex])).Target.Attributes[alternate].ToString();
                        string errorLog = "('" + DateTime.Now + "','" + e.Fault.Message.Replace("'", "''") + "','" + (e.Fault.TraceText != null ? e.Fault.TraceText.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                        WriteLogsToDB(errorLog);
                    }
                }

                if (isOrder && existingRecords.Count > 0)
                {
                    UpsertOrderItems(OrderItem, p, OrderItemStoreProcName, "wrs_id", existingRecords);
                    CreateOrderNotes(OrderNote, p, OrderStoreNotesProcName, "wrs_id", existingRecords);
                }

                foreach (string l in existingRecords)
                {
                    if (!isOrderNote && table.ToLower() != "orderitem")
                    {
                        p.UpdateSyncStatus(table, l);
                    }
                    //ErrorLog(string.Format("Sync Status updated in NC DB for {0} with ID - {1}", table, l));
                }

                //if (!isOrder)
                //{
                //    foreach (string l in existingRecords)
                //    {
                //        if (isOrderNote)
                //            p.UpdateSyncStatus("Order", l);
                //        else
                //            p.UpdateSyncStatus(table, l);
                //        //ErrorLog(string.Format("Sync Status updated in NC DB for {0} with ID - {1}", table, l));
                //    }
                //}

                WriteMigrateStatusLogs(mainSuccessLog);

            }
            catch (Exception e)
            {
                string errorLog = "('" + DateTime.Now + "','" + e.Message.Replace("'", "''") + "','" + (e.StackTrace != null ? e.StackTrace.Replace("'", "''") : " ") + "','" + table + "','" + NCId + "'";
                WriteLogsToDB(errorLog);
            }
        }

        private OrganizationRequest CreateCreateRequest(Entity e)
        {
            CreateRequest cr = new CreateRequest();
            cr.Target = e;
            return cr;
        }

        private OrganizationRequest CreateUpdateRequest(Entity e)
        {
            UpdateRequest cr = new UpdateRequest();
            cr.Target = e;
            return cr;
        }

        public SetStateRequest CreateSetStateRequest(string entityName, int alternateKey, string alternateKeyName, int stateCode, int statusCode)
        {

            EntityReference target = new EntityReference(entityName, alternateKeyName, alternateKey);
            SetStateRequest request = new SetStateRequest();
            request.EntityMoniker = target;
            request.State = new OptionSetValue(stateCode);
            request.Status = new OptionSetValue(statusCode);

            return request;
        }

        public int RetreveAlternateKey(Guid accountID, string entityLogicalName, string alternate, string primary)
        {
            QueryExpression accountbyID = new QueryExpression(entityLogicalName);
            accountbyID.Criteria.AddCondition(new ConditionExpression(primary, ConditionOperator.Equal, accountID));
            if (entityLogicalName.Equals(TransactionCurrency.EntityLogicalName))
                accountbyID.ColumnSet.Columns.Add(alternate);
            Entity e = serviceClient().Retrieve(entityLogicalName, accountID, accountbyID.ColumnSet);
            if (e != null && e.Attributes[alternate] != null)
                return e.GetAttributeValue<int>(alternate);
            return 0;
        }

        private static void WriteLogsToDB(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = string.Empty;
                sql = "INSERT INTO [dbo].[CrmImportErrorLog] ([CreatedOnUtc],[Message],[StackTrace],[Table],[EntityId]) " +
                       "VALUES " + insertQuery + ")";

                SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQLErrorLog"].ToString());
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }

        private static void WriteMigrateStatusLogs(string insertQuery)
        {
            if (insertQuery != string.Empty)
            {
                string sql = string.Empty;
                sql = "INSERT INTO [dbo].[CrmImportStatusLog] ([CreatedOnUtc],[Message],[Table],[EntityId]) " +
                       "VALUES " + insertQuery;

                SqlConnection cnn = new SqlConnection(ConfigurationManager.ConnectionStrings["SQLErrorLog"].ToString());
                SqlCommand command;
                cnn.Open();
                command = new SqlCommand(sql, cnn);
                SqlDataReader dr = command.ExecuteReader();
                command.Dispose();
                cnn.Close();
            }
        }
    }
}