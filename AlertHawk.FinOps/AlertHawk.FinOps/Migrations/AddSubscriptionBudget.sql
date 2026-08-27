-- Migration: Add Budget column to Subscriptions
-- Monthly budget in USD (nullable = no budget set)

IF COL_LENGTH('dbo.Subscriptions', 'Budget') IS NULL
BEGIN
    ALTER TABLE [dbo].[Subscriptions]
    ADD [Budget] DECIMAL(18, 2) NULL;
END
GO
