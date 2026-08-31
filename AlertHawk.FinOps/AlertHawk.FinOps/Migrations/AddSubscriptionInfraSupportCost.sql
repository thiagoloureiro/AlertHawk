-- Migration: Add InfraSupportCost column to Subscriptions
-- Monthly infra support overlay for historical charts (USD). Default $400/mo.

IF COL_LENGTH('dbo.Subscriptions', 'InfraSupportCost') IS NULL
BEGIN
    ALTER TABLE [dbo].[Subscriptions]
    ADD [InfraSupportCost] DECIMAL(18, 2) NOT NULL
        CONSTRAINT [DF_Subscriptions_InfraSupportCost] DEFAULT (400);
END
GO
