USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetPriceCalendar]    Script Date: 03/26/2018 10:48:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER procedure [dbo].[CrmGetPriceCalendar]
as
begin
SELECT P.[Id]
      ,[CalendarTypeId]
      ,[CalendarDate]
      ,[PriceGroupId]
  FROM [OneStore].[dbo].[PriceCalendars] p 
  inner join WRS_AuditLog W on W.EntityId = P.Id and  W.DateSync is null and w.EntityName = 'PriceCalendars' 
  and (W.RecordState = 'Added' or W.RecordState = 'Updated')
  and W.UpdatedById <> 0000000

  end


