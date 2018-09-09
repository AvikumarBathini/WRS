USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetBlackoutCalendarProducts]    Script Date: 03/26/2018 10:31:46 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[CrmGetBlackoutCalendarProducts]
as
begin
SELECT distinct B.[Id]
      ,B.[BlackoutCalendarId]
      ,B.[ProductId]
      ,B.[CreatedById]
      ,B.[CreatedOnUtc]
  FROM [OneStore].[dbo].[WRS_BlackoutCalendarProductMapping] B
  join WRS_AuditLog W on B.Id = W.EntityId and EntityName = 'BlackoutCalendarProductMapping' 
  and W.DateSync is null and (W.RecordState = 'Added' or W.RecordState = 'Updated')
 and W.UpdatedById <> 0000000

  end

