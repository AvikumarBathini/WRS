USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetStores]    Script Date: 04/23/2018 17:43:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[CrmGetStores]
AS
BEGIN
SELECT S.[Id]
      ,[Name]
      ,[Url]
      ,[SslEnabled]
      ,[SecureUrl]
      ,[Hosts]
      ,[DefaultLanguageId]
      ,[DisplayOrder]
      ,[CompanyName]
      ,[CompanyAddress]
      ,[CompanyPhoneNumber]
      ,[CompanyVat]
  FROM [OneStore].[dbo].[Store] S
  INNER JOIN
  WRS_AuditLog W ON S.Id = W.EntityId AND W.EntityName = 'Store' 
  AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
  AND W.DateSync IS NULL
  AND W.UpdatedById <> 0000000
END