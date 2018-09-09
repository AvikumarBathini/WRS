USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetNewsLetterSubscriptions]    Script Date: 06/08/2018 10:34:19 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/****** Script for SelectTopNRows command from SSMS  ******/
ALTER PROCEDURE [dbo].[CrmGetNewsLetterSubscriptions]
AS
BEGIN
 SELECT  N.[Id]
      ,N.[Firstname]
      ,N.[Lastname]
      ,N.[Email]
      ,N.[Active]
      ,N.[StoreId]
      ,N.[CreatedOnUtc]
      ,N.[Source]
      ,N.[Category]
      ,N.[Campaign]
FROM [OneStore].[dbo].[NewsLetterSubscription] N
INNER JOIN WRS_AuditLog W ON N.Id = W.EntityId 
AND W.EntityName = 'NewsLetterSubscription' 
AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
AND W.DateSync IS NULL
AND W.UpdatedById <> 0000000
ORDER BY CreatedOnUtc DESC

END

