USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetPaymentTransaction]    Script Date: 04/23/2018 18:04:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[CrmGetPaymentTransaction]
AS
BEGIN
SELECT DISTINCT C.[Id]
      ,[OrderId]
      ,[PaymentStatus]
      ,[PaymentGuid]
      ,[MerchantRef]
      ,[TranRef]
      ,[TranStatus]
      ,[BankAuthId]
      ,[TranMessage]
      ,[PaymentMID]
      ,[TranResponseCode]
      ,[TID]
      ,[TranDate]
      ,[TranAmount]
      ,[PaymentMode]
      ,[CardNo]
      ,[CardExpiry]
      ,[CreatedOnUtc]
  FROM [OneStore].[dbo].WRS_PaymentTransactions C
JOIN WRS_AuditLog W ON C.Id = W.EntityId 
AND W.DateSync IS NULL 
AND w.EntityName = 'PaymentTransaction'
AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
AND W.UpdatedById <> 0000000
END
