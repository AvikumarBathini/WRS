USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetCountries]    Script Date: 04/23/2018 17:49:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[CrmGetCountries]
AS
BEGIN
SELECT C.[Id]
      ,[Name]
      ,[AllowsBilling]
      ,[AllowsShipping]
      ,[TwoLetterIsoCode]
      ,[ThreeLetterIsoCode]
      ,[NumericIsoCode]
      ,[SubjectToVat]
      ,[Published]
      ,[DisplayOrder]
      ,[LimitedToStores]
FROM [OneStore].[dbo].[Country] C
JOIN WRS_AuditLog W ON C.Id = W.EntityId 
AND W.DateSync IS NULL 
AND w.EntityName = 'Country'
AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
AND W.UpdatedById <> 0000000
END