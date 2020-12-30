USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetCategories]    Script Date: 03/26/2018 10:50:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER procedure [dbo].[CrmGetCategories]
as
begin
SELECT  P.[Id]
      ,[Name]
      
  FROM [OneStore].[dbo].[Category] P
  inner join WRS_AuditLog W on W.EntityId = P.Id and  W.DateSync is null and w.EntityName = 'Category' 
  and (W.RecordState = 'Added' or W.RecordState = 'Updated')
 and W.UpdatedById <> 0000000

  end

