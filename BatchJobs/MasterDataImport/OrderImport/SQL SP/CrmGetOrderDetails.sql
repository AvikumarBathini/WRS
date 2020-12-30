USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetOrderDetails]    Script Date: 04/23/2018 18:00:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
 ALTER procedure [dbo].[CrmGetOrderDetails]
 AS
 BEGIN
SELECT DISTINCT O.[Id]
      ,O.[OrderGuid]
      ,O.[StoreId]
      ,O.[CustomerId]
      ,O.[BillingAddressId]
      ,O.[ShippingAddressId]
      ,O.[PickupAddressId]
      ,O.[PickUpInStore]
      ,O.[OrderStatusId]
      ,O.[ShippingStatusId]
      ,O.[PaymentStatusId]
      ,O.[PaymentMethodSystemName]
      ,O.[CustomerCurrencyCode]
      ,O.[CurrencyRate]
      ,O.[CustomerTaxDisplayTypeId]
      ,O.[OrderSubtotalInclTax]
      ,O.[OrderSubtotalExclTax]
      ,O.[OrderSubTotalDiscountInclTax]
      ,O.[OrderSubTotalDiscountExclTax]
      ,O.[OrderShippingInclTax]
      ,O.[OrderShippingExclTax]
      ,O.[PaymentMethodAdditionalFeeInclTax]
      ,O.[PaymentMethodAdditionalFeeExclTax]
      ,O.[TaxRates]
      ,O.[OrderTax]
      ,O.[OrderDiscount]
      ,O.[OrderTotal]
      ,O.[RefundedAmount]
      ,O.[RewardPointsHistoryEntryId]
      ,O.[CheckoutAttributeDescription]
      ,O.[CheckoutAttributesXml]
      ,O.[CustomerIp]
      ,O.[AuthorizationTransactionId]
      ,O.[AuthorizationTransactionCode]
      ,O.[AuthorizationTransactionResult]
      ,O.[CaptureTransactionId]
      ,O.[CaptureTransactionResult]
      ,O.[SubscriptionTransactionId]
      ,O.[PaidDateUtc]
      ,O.[ShippingMethod]
      ,O.[ShippingRateComputationMethodSystemName]
      ,O.[Deleted]
      ,O.[CreatedOnUtc]
      ,O.[CustomOrderNumber]
      ,O.[GalaxyOrderNumber]
      ,O.[WCFOrderConfirmationId]
      
,A.Address1 as Address1Bill
,A.Address2 as Address2Bill
,A.City as CityBill
,A.ZipPostalCode as ZipPostalCodeBill
,A.FaxNumber as FaxNumberBill
,A.PhoneNumber as PhoneNumberBill
,Co.Name as CountryNameBill
,A.CountryId as CountryIdBill
,A.StateProvinceId as StateProvinceIdBill
,S.Name as StateNameBill

,A1.Address1 as Address1Ship
,A1.Address2 as Address2Ship
,A1.City as CityShip
,A1.ZipPostalCode as ZipPostalCodeShip
,A1.FaxNumber as FaxNumberShip
,A1.PhoneNumber as PhoneNumberShip
,CS.Name as CountryNameShip
,A1.CountryId as CountryIdShip
,A1.StateProvinceId as StateProvinceIdShip
,SS.Name as StateNameShip
,O.[PriceGroupId]
,W.Id AS AuditLogID
	  
  FROM [OneStore].[dbo].[Order] O 
  JOIN
  WRS_AuditLog W on O.Id = W.EntityId AND W.EntityName = 'Order'  
  AND (W.RecordState = 'Added' OR W.RecordState = 'Updated') AND W.DateSync IS NULL
  AND W.UpdatedById <> 0000000
  JOIN Customer C on O.CustomerId = C.Id

  --billing address
  LEFT JOIN [Address] A on O.BillingAddressId = A.Id
  LEFT JOIN Country Co on A.CountryId = Co.Id
  LEFT JOIN StateProvince S on A.StateProvinceId = S.Id and A.CountryId = S.CountryId

  --shipping address
  LEFT JOIN [Address] A1 on O.ShippingAddressId = A.Id
  LEFT JOIN Country CS on A1.CountryId = CS.Id
  LEFT JOIN StateProvince SS on A.StateProvinceId = SS.Id AND A1.CountryId = SS.CountryId

  --pickup address
  LEFT JOIN [Address] A2 on O.PickupAddressId = A.Id
  WHERE C.Email IS NOT NULL
  ORDER BY W.Id ASC
  END