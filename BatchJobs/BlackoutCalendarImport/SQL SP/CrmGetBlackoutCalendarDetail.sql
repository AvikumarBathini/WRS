USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetBlackoutCalendarDetail]    Script Date: 03/26/2018 10:26:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[CrmGetBlackoutCalendarDetail]
as
begin
SELECT distinct C.[Id]
      ,C.[BlackoutCalendarId]
      ,C.[Name]
      ,C.[DateFrom]
      ,C.[DateTo]
      ,C.[CreatedById]
      ,C.[CreatedOnUtc]
      ,C.[UpdatedById]
      ,C.[UpdatedOnUtc]
  FROM [OneStore].[dbo].[WRS_BlackoutCalendarDetail]   C
    join WRS_AuditLog W on C.Id = W.EntityId and W.DateSync is null and w.EntityName = 'BlackoutCalendarDetail'
 and (W.RecordState = 'Added' or W.RecordState = 'Updated')
 and W.UpdatedById <> 0000000

  end
