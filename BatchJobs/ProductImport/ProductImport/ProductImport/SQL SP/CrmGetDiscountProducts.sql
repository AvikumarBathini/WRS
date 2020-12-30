USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetDiscountProducts]    Script Date: 03/26/2018 10:55:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[CrmGetDiscountProducts]
as
begin

SELECT [Discount_Id]
      ,[Product_Id]
  FROM [OneStore].[dbo].[Discount_AppliedToProducts]

  end
