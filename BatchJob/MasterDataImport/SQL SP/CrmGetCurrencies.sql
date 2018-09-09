USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetCurrencies]    Script Date: 04/23/2018 17:55:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[CrmGetCurrencies]
AS
BEGIN
SELECT C.[Id]
      ,[Name]
      ,[CurrencyCode]
      ,[Rate]
      ,[DisplayLocale]
      ,[CustomFormatting]
      ,[LimitedToStores]
      ,[Published]
      ,[DisplayOrder]
      ,[CreatedOnUtc]
      ,[UpdatedOnUtc]
      ,[RoundingTypeId]
  FROM [OneStore].[dbo].[Currency] C
  INNER JOIN WRS_AuditLog W ON C.Id = W.EntityId 
  AND W.EntityName = 'Currency' 
  AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
  AND W.DateSync IS NULL
  AND W.UpdatedById <> 0000000
END

