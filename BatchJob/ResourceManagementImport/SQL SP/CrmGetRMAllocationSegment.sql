USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetRMAllocationSegment]    Script Date: 03/26/2018 11:00:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[CrmGetRMAllocationSegment]
as begin
SELECT  A.[Id]
      ,[Name]
  FROM [OneStore].[dbo].[WRS_RMAllocationSegment] A

   inner join WRS_AuditLog W on W.EntityId = A.Id and  W.DateSync is null and w.EntityName = 'RMAllocationSegment' 
  and (W.RecordState = 'Added' or W.RecordState = 'Updated')
  and W.UpdatedById <> 0000000

  end
