USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetBlackoutCalendar]    Script Date: 03/26/2018 10:25:49 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER procedure [dbo].[CrmGetBlackoutCalendar]

as
begin
SELECT distinct C.[Id]
      ,C.[Name]
      ,C.[Remarks]
      ,C.[CreatedById]
      ,C.[CreatedOnUtc]
      ,C.[UpdatedById]
      ,C.[UpdatedOnUtc]
  FROM [OneStore].[dbo].[WRS_BlackoutCalendar] C
    join WRS_AuditLog W on C.Id = W.EntityId and W.DateSync is null and w.EntityName = 'BlackoutCalendarItem'
 and (W.RecordState = 'Added' or W.RecordState = 'Updated')
 and W.UpdatedById <> 0000000
  end

