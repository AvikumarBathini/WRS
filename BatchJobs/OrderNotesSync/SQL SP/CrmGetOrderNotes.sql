USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetOrderNotes]    Script Date: 04/23/2018 18:01:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[CrmGetOrderNotes]
@List AS dbo.OrderRelatedNotes READONLY
AS
BEGIN
SELECT N.[Id]
      ,[OrderId]
      ,[Note]
      ,N.[CreatedOnUtc]
      ,N.DisplayToCustomer
  FROM [OneStore].[dbo].[OrderNote] N
  JOIN [Order]  O ON N.OrderId = O.Id
  JOIN  Customer C ON O.CustomerId = C.Id
  INNER JOIN WRS_AuditLog W ON N.OrderId = W.EntityId 
  AND W.EntityName = 'Order' 
  AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
  AND W.UpdatedById <> 0000000 
  AND N.OrderId IN (SELECT Id FROM @List)
  AND W.DateSync IS NULL 
  WHERE C.Email IS NOT NULL 
END 


