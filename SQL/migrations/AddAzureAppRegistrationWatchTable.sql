IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AzureAppRegistrationWatch')
BEGIN
    CREATE TABLE [dbo].[AzureAppRegistrationWatch](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ApplicationObjectId] [nvarchar](100) NOT NULL,
        [ApplicationDisplayName] [nvarchar](255) NOT NULL,
        [AppId] [nvarchar](100) NOT NULL,
        [IsEnabled] [bit] NOT NULL CONSTRAINT [DF_AzureAppRegistrationWatch_IsEnabled] DEFAULT (1),
        [CreatedAt] [datetime] NOT NULL,
        CONSTRAINT [PK_AzureAppRegistrationWatch] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_AzureAppRegistrationWatch_ObjectId] UNIQUE ([ApplicationObjectId])
    );
END
GO
