USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetProductProductTagMappings]    Script Date: 03/26/2018 10:58:31 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[CrmGetProductProductTagMappings]
as
begin


  SELECT
     [Product_Id], 
     STUFF(
         (SELECT DISTINCT ',' + cast([ProductTag_Id] as varchar(10))
          FROM [Product_ProductTag_Mapping] b
		  join ProductTag t on b.ProductTag_Id = t.Id
          WHERE [Product_Id] = a.[Product_Id] 
          FOR XML PATH (''))
          , 1, 1, '')  AS [ProductTag_Ids]
FROM [Product_ProductTag_Mapping] AS a
join Product P on a.Product_Id = P.Id
GROUP BY [Product_Id]

end
