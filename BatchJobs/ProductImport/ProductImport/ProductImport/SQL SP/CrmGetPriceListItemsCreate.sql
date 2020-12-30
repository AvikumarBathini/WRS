USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetPriceListItemsCreate]    Script Date: 03/26/2018 10:54:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER procedure [dbo].[CrmGetPriceListItemsCreate]
as
begin
SELECT  PG.[Id]
      ,[ProductId]
      ,[PriceGroupId]
      ,[Price]
  FROM [OneStore].[dbo].[PriceGroups] PG
   inner join
  WRS_AuditLog W on PG.Id = W.EntityId and W.EntityName = 'PriceGroups' and (W.RecordState = 'Added' )
  and W.DateSync is null
  and W.UpdatedById <> 0000000

  end

