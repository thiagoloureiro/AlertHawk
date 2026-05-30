IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AzureAppSecret')
BEGIN
    CREATE TABLE [dbo].[AzureAppSecret](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ApplicationObjectId] [nvarchar](100) NOT NULL,
        [ApplicationDisplayName] [nvarchar](255) NULL,
        [AppId] [nvarchar](100) NULL,
        [KeyId] [uniqueidentifier] NOT NULL,
        [SecretDisplayName] [nvarchar](255) NULL,
        [EndDateTime] [datetimeoffset] NOT NULL,
        [DaysUntilExpiry] [int] NOT NULL,
        [IsExpiring] [bit] NOT NULL,
        [LastChecked] [datetime] NOT NULL,
        CONSTRAINT [PK_AzureAppSecret] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_AzureAppSecret_App_Key] UNIQUE ([ApplicationObjectId], [KeyId])
    );
END
GO
