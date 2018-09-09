USE [WRS_LogDBName]
GO

/****** Object:  Table [dbo].[WRS_SubscriptionLog]    Script Date: 15/08/2017 12:10:34 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[WRS_SubscriptionLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LogType] [nvarchar](50) NULL,
	[LogInfo] [nvarchar](max) NULL,
	[EmailAddress] [nvarchar](50) NULL,
	[CreateOn] [datetime] NULL,
 CONSTRAINT [PK_WRS_SubscriptionLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'type:exception and information' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'WRS_SubscriptionLog', @level2type=N'COLUMN',@level2name=N'LogType'
GO

