USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetRMEventStock]    Script Date: 03/26/2018 11:01:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER procedure [dbo].[CrmGetRMEventStock]
as
begin
SELECT distinct ES. [Id] as RmEventStockId
      ,ES.[EventId]
   --   ,ES.[AllocationSegmentId]
      ,ES.[Quantity]
      ,ES.[UsedQty]
      ,ES.[ReservedQty]
      ,ES.[Active]
	  ,ET.Id as RMEventTypePkId
	  ,ET.EventTypeId as EventType
	  ,E.Id as RMEventPkId
	  ,E.EventId as RMEventId
	  ,E.StartDateTime
	  ,e.OffSaleDateTime
	  ,PC.GalaxyPLU
	--  ,PC.ProductId
	--  ,PC.Id as RMPluConfigurationPkId
	  ,R.Id as RMResourcePkId
	  ,R.ResourceId as RMResource
	  ,ES.AllocationSegmentId as segment
  FROM [OneStore].[dbo].[WRS_RMEventStock] ES
  inner join WRS_RMEvent E on ES.EventId = E.Id
  inner join WRS_RMEventType ET on E.EventTypeId =ET.EventTypeId
  inner join WRS_RMPluConfiguration PC on PC.EventTypeId = ET.EventTypeId
  inner join WRS_RMResource R on E.ResourceId = R.ResourceId
  --inner join WRS_RMAllocationSegment AL on PC.AllocationSegmentId = AL.Id
  
   inner join  WRS_AuditLog W on ES.Id = W.EntityId 
   and W.EntityName in( 'RMEventStock','RMEvent','RMEventType','RMPluConfiguration',
   'RMResource','RMAllocationSegment') 
   and (W.RecordState = 'Added' or W.RecordState = 'Updated')
  and W.DateSync is null 
  and W.UpdatedById <> 0000000
  and W.UpdatedById <> 933821

  order by RmEventStockId
  end


