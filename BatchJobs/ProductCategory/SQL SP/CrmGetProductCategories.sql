USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetProductCategories]    Script Date: 03/26/2018 10:51:11 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER procedure [dbo].[CrmGetProductCategories]
as
begin


  SELECT
     ProductId, 
     STUFF(
         (SELECT DISTINCT ',' + cast(CategoryId as varchar(10))
          FROM Product_Category_Mapping b
		  join Category t on b.CategoryId = t.Id
          WHERE ProductId = a.ProductId 
          FOR XML PATH (''))
          , 1, 1, '')  AS [ProductCategory_Ids]
FROM Product_Category_Mapping AS a
join Product P on a.ProductId = P.Id

 join WRS_AuditLog W on a.Id = W.EntityId and EntityName = 'Product_Category_Mapping' 
  and W.DateSync is not null and (W.RecordState = 'Added' or W.RecordState = 'Updated')
  and W.UpdatedById <> 0000000

GROUP BY ProductId

end


