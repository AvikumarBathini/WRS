USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetProductFamilies]    Script Date: 03/26/2018 10:56:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[CrmGetProductFamilies]
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
  FROM [OneStore].[dbo].[Product] P

   inner join WRS_AuditLog W on W.EntityId = P.Id and  W.DateSync is null and w.EntityName = 'Product' 
  and (W.RecordState = 'Added' or W.RecordState = 'Updated')

  and W.UpdatedById <> 0000000

  WHERE P.[ProductTypeId] = 10
  end
