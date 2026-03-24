-- Migration: Add Subscriptions Table
-- This script creates the Subscriptions table for storing subscription descriptions
-- Run this script against your SQL Server database

-- Create Subscriptions table
CREATE TABLE [dbo].[Subscriptions] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SubscriptionId] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id])
);

-- Create unique index on SubscriptionId
CREATE UNIQUE INDEX [IX_Subscriptions_SubscriptionId] 
    ON [dbo].[Subscriptions] ([SubscriptionId]);

GO
