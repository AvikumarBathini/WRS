USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetPaymentGateway]    Script Date: 04/23/2018 18:03:24 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[CrmGetPaymentGateway]
AS
BEGIN
SELECT DISTINCT C.[Id]
      ,[Name]
      ,[SystemName]
      ,[Logo]
      ,[Timeout]
      ,[APIEndpoint]
      ,[SuccessURL]
      ,[PaymentUrl]
      ,[FailedUrl]
      ,[IsActive]
      ,[CreatedById]
      ,[CreatedOnUtc]
      ,C.[UpdatedById]
      ,[UpdatedOnUtc]
      ,[PaymentTransactionLimit]
  FROM [OneStore].[dbo].WRS_PaymentGateway C
  JOIN WRS_AuditLog W ON C.Id = W.EntityId 
  AND W.DateSync IS NULL 
  AND w.EntityName = 'PaymentGatewayItem'
  AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
  AND W.UpdatedById <> 0000000
END
