USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetOrderItemDetails]    Script Date: 12/21/2020 18:08:22 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[CrmGetOrderItemDetails]
@List AS dbo.OrderRelatedNotes READONLY

AS
BEGIN
SELECT DISTINCT OI.[Id]
      ,[OrderItemGuid]
      ,[OrderId]
      ,[ProductId]
      ,[Quantity]
      ,[UnitPriceInclTax]
      ,[UnitPriceExclTax]
      ,[PriceInclTax]
      ,[PriceExclTax]	
      ,[DiscountAmountInclTax]
      ,[DiscountAmountExclTax]
      ,[OriginalProductCost]
      ,[AttributeDescription]
      ,[AttributesXml]
      ,[DownloadCount]
      ,[IsDownloadActivated]
      ,[ProductAttributeCombinationSku]
      ,C.Id AS CustId
  FROM [OneStore].[dbo].[OrderItem] OI 
  INNER JOIN [Order] O ON O.Id = OI.OrderId
  INNER JOIN Customer C ON C.Id = O.CustomerId
  WHERE OI.OrderId IN (SELECT Id FROM @List)
  END
