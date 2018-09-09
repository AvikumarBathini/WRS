USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetCustomerGenericAttribute]    Script Date: 04/23/2018 17:41:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[CrmGetCustomerGenericAttribute]
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
,C.Username
,C.Email
,C.[AccountNumber]
,V.FirstName
,V.LastName
,v.DateOfBirth
,v.Gender
--,W.RecordState

FROM Customer C  
	JOIN GenericAttribute G ON G.EntityId=C.Id AND G.KeyGroup='Customer' AND G.[Key] IN ('FirstName','LastName','Gender','DateOfBirth')
	JOIN WRS_AuditLog W ON G.Id = W.EntityId AND W.DateSync IS NULL AND w.EntityName = 'GenericAttribute'
	AND (W.RecordState = 'Added' OR W.RecordState = 'Updated') AND W.UpdatedById <> 0000000
	LEFT JOIN
	#GenericAttributeVirtual V ON C.Id = V.EntityId  

WHERE C.Email IS NOT NULL AND C.AccountNumber IS NOT NULL
DROP TABLE #GenericAttributeVirtual
END


