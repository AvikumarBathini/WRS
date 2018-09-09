USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetCustomerDetails]    Script Date: 04/23/2018 17:41:34 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[CrmGetCustomerDetails]
AS
BEGIN
SELECT *
INTO #GenericAttributeVirtual
FROM 
(
  SELECT EntityId, [Key], [Value]
  FROM [GenericAttribute]
  -- where EntityId in (select EntityId from WRS_AuditLog where DateSync is null)
) src
PIVOT
(
  MAX([Value])
  FOR [Key] IN ([FirstName],[LastName],[Gender],[DateOfBirth])
) piv;

SELECT DISTINCT
C.Id
,C.CustomerGuid
,C.Username
,C.Email
,C.EmailToRevalidate
,C.AdminComment
,C.IsTaxExempt
,C.AffiliateId
,C.VendorId
,C.HasShoppingCartItems
,C.RequireReLogin
,C.FailedLoginAttempts
,C.CannotLoginUntilDateUtc
,C.Active
,C.Deleted
,C.IsSystemAccount
,C.SystemName
,C.LastIpAddress
,C.CreatedOnUtc
,C.LastLoginDateUtc
,C.LastActivityDateUtc
,C.RegisteredInStoreId
,C.BillingAddress_Id
,C.ShippingAddress_Id
,C.[AccountNumber]
,V.FirstName
,V.LastName
,v.DateOfBirth
,v.Gender
--,W.RecordState
,A.Address1 AS Address1Bill
,A.Address2 AS Address2Bill
,A.City AS CityBill
,A.ZipPostalCode AS ZipPostalCodeBill
,A.FaxNumber AS FaxNumberBill
,A.PhoneNumber AS PhoneNumberBill
,Co.Name AS CountryNameBill
,A.CountryId AS CountryIdBill
,A.StateProvinceId AS StateProvinceIdBill
,S.Name AS StateNameBill

,A1.Address1 AS Address1Ship
,A1.Address2 AS Address2Ship
,A1.City AS CityShip
,A1.ZipPostalCode AS ZipPostalCodeShip
,A1.FaxNumber AS FaxNumberShip
,A1.PhoneNumber AS PhoneNumberShip
,CS.Name AS CountryNameShip
,A1.CountryId AS CountryIdShip
,A1.StateProvinceId AS StateProvinceIdShip
,SS.Name AS StateNameShip

FROM Customer C  
JOIN WRS_AuditLog W ON C.Id = W.EntityId 
AND W.DateSync IS NULL 
AND w.EntityName = 'Customer'
AND (W.RecordState = 'Added' OR W.RecordState = 'Updated') 
AND W.UpdatedById <> 0000000
LEFT JOIN
#GenericAttributeVirtual V ON C.Id = V.EntityId  

--Billing Address
LEFT JOIN [Address] A ON A.Id = C.BillingAddress_Id
LEFT JOIN Country Co ON A.CountryId = Co.Id
LEFT JOIN StateProvince S ON A.StateProvinceId = S.Id AND A.CountryId = S.CountryId

--Shipping Address
LEFT JOIN [Address] A1 ON A1.Id = C.ShippingAddress_Id
LEFT JOIN Country CS ON A1.CountryId = CS.Id
LEFT JOIN StateProvince SS ON A.StateProvinceId = SS.Id AND A1.CountryId = SS.CountryId
WHERE  C.Email IS NOT NULL AND C.AccountNumber IS NOT NULL
DROP  TABLE #GenericAttributeVirtual
END