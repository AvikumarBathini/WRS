USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetProductTags]    Script Date: 03/26/2018 10:57:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER procedure [dbo].[CrmGetProductTags]
as
begin

SELECT P.[Id]
      ,[Name]
  FROM [OneStore].[dbo].[ProductTag] P
   inner join WRS_AuditLog W on W.EntityId = P.Id and  W.DateSync is null and w.EntityName = 'ProductTag' 
  and (W.RecordState = 'Added' or W.RecordState = 'Updated')
  and W.UpdatedById <> 0000000


  end

