USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetDiscountUsageDetails]    Script Date: 04/23/2018 17:59:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/****** Script for SelectTopNRows command from SSMS  ******/

ALTER procedure [dbo].[CrmGetDiscountUsageDetails]
AS
BEGIN
SELECT  D.[Id]
      ,[DiscountId]
      ,[OrderId]
      ,[CreatedOnUtc]
  FROM [OneStore].[dbo].[DiscountUsageHistory] D
  INNER JOIN WRS_AuditLog W ON D.Id = W.EntityId 
  AND W.EntityName = 'DiscountUsageHistory' 
  AND (W.RecordState = 'Added' OR W.RecordState = 'Updated')
  AND W.DateSync IS NULL
  AND W.UpdatedById <> 0000000
END
