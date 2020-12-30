USE [OneStore]
GO
/****** Object:  StoredProcedure [dbo].[CrmGetPassCoMember]    Script Date: 12/28/2020 11:26:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[CrmGetPassCoMember]
@Passes as dbo.CoMemberForPassList READONLY
AS
BEGIN
SELECT DISTINCT PCM.[Id]
      ,PassesId
      ,FirstName
      ,LastName
      ,DateOfBirth
      ,AgeGroup
      ,CustContactId
      ,CreatedOnUtc
      ,IdentificationNo	
      ,Gender
      ,Email
      ,RelationshipTypeId
      ,CustomerId
  FROM [OneStore].[dbo].[WRS_PassesCoMember] PCM   
  INNER JOIN WRS_AuditLog W ON W.EntityId = PCM.Id 
  AND W.EntityName = 'PassesCoMember'
  AND PassesId in (SELECT PassId from @Passes)
  END
 

