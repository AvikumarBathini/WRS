USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetOrderItemDetails]    Script Date: 04/23/2018 18:00:59 ******/
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
  INNER JOIN
  WRS_AuditLog W ON OI.OrderId = W.EntityId 
  AND W.EntityName = 'Order'
  AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
  AND W.UpdatedById <> 0000000 
  AND OI.OrderId IN (SELECT Id FROM @List)

  INNER JOIN [Order] O ON O.Id = OI.OrderId
  INNER JOIN Customer C ON C.Id = O.CustomerId
  WHERE C.Email IS NOT NULL
  END
