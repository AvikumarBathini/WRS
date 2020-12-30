USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetDiscounts]    Script Date: 03/26/2018 10:53:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/****** Script for SelectTopNRows command from SSMS  ******/
ALTER procedure [dbo].[CrmGetDiscounts]
as
begin
SELECT distinct D.[Id]
      ,[Name]
      ,[DiscountTypeId]
      ,[UsePercentage]
      ,[DiscountPercentage]
      ,[DiscountAmount]
      ,[MaximumDiscountAmount]
      ,[StartDateUtc]
      ,[EndDateUtc]
      ,[RequiresCouponCode]
      ,[CouponCode]
      ,[IsCumulative]
      ,[DiscountLimitationId]
      ,[LimitationTimes]
      ,[MaximumDiscountedQuantity]
      ,[AppliedToSubCategories]
  FROM [OneStore].[dbo].[Discount] D
  inner join WRS_AuditLog W on W.EntityId = D.Id and  W.DateSync is null and w.EntityName = 'Discount' 
  and (W.RecordState = 'Added' or W.RecordState = 'Updated')
 and W.UpdatedById <> 0000000

  end
