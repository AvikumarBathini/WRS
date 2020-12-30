USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetStateProvinces]    Script Date: 04/23/2018 17:57:04 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[CrmGetStateProvinces]
AS
BEGIN
SELECT S.[Id]
      ,[CountryId]
      ,[Name]
      ,[Abbreviation]     
  FROM [OneStore].[dbo].[StateProvince] S
  INNER JOIN WRS_AuditLog W ON S.Id = W.EntityId 
  AND W.EntityName = 'StateProvince' 
  AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
  AND W.DateSync IS NULL
  AND W.UpdatedById <> 0000000
END
