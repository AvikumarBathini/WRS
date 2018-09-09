USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetProductsToDraft]    Script Date: 03/26/2018 10:56:34 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[CrmGetProductsToDraft]
as
begin
SELECT distinct P.[Id]
      
  FROM [OneStore].[dbo].[Product] P

   inner join WRS_AuditLog W on W.EntityId = P.Id and  W.DateSync is null and w.EntityName = 'Product' 
  and W.UpdatedById <> 0000000
  and ( W.RecordState = 'Updated') 

  end
