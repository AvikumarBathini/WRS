USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetRMEventType]    Script Date: 03/26/2018 11:00:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[CrmGetRMEventType]
as
begin

SELECT distinct [Name]
      ,[EventTypeId]
      ,E.Id
  FROM [OneStore].[dbo].[WRS_RMEventType] E

    inner join WRS_AuditLog W on W.EntityId = E.Id and  W.DateSync is null and w.EntityName = 'RMEventType' 
  and (W.RecordState = 'Added' or W.RecordState = 'Updated')
  and W.UpdatedById <> 0000000

  end
