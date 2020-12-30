USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetProducts]    Script Date: 03/26/2018 10:52:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[CrmGetProducts]
as
begin
SELECT distinct P.[Id]
      ,[ProductTypeId]
      ,[ParentGroupedProductId]
      ,[Name]
      ,[ShortDescription]
      ,[FullDescription]
      ,[AdminComment]
      ,[VendorId]
      ,[SubjectToAcl]
      ,[LimitedToStores]
      ,[Sku]
      ,[ManufacturerPartNumber]
      ,[IsGiftCard]
      ,[GiftCardTypeId]
      ,[IsShipEnabled]
      ,[IsFreeShipping]
      ,[WarehouseId]
      ,[StockQuantity]
      ,[OrderMinimumQuantity]
      ,[OrderMaximumQuantity]
      ,[AllowedQuantities]
      ,[Price]
      ,[OldPrice]
      ,[ProductCost]
      ,[HasDiscountsApplied]
      ,[Weight]
      ,[Length]
      ,[Width]
      ,[Height]
      ,[AvailableStartDateTimeUtc]
      ,[AvailableEndDateTimeUtc]
      ,[DisplayOrder]
      ,[Published]
      ,[CreatedOnUtc]
      ,[UpdatedOnUtc]
      ,[SalesPeriodFrom]
      ,[SalesPeriodTo]
	  --,w.DateSync
  FROM [OneStore].[dbo].[Product] P
   inner join
  WRS_AuditLog W on P.Id = W.EntityId and W.EntityName = 'Product' and (W.RecordState = 'Added' or W.RecordState = 'Updated')
  and W.UpdatedById <> 0000000
  and W.DateSync is null 
   where ProductTypeId = 5
  end


