USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetRMResource]    Script Date: 03/26/2018 11:01:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/****** Script for SelectTopNRows command from SSMS  ******/
ALTER procedure [dbo].[CrmGetRMResource]
as
begin
SELECT [Name]
      ,[ResourceId]
      ,R.[Id]
  FROM [OneStore].[dbo].[WRS_RMResource] R
   inner join
  WRS_AuditLog W on R.Id = W.EntityId and W.EntityName = 'RMResource' and (W.RecordState = 'Added' or W.RecordState = 'Updated')
  and W.DateSync is null
  and W.UpdatedById <> 0000000

  end
