-- Migration: AddPerAgentConfigurationFields
-- Date: 2024-10-24
-- Description: Add per-agent configuration fields and remove deprecated ActivePlugins field

-- Add new columns for per-agent configuration
ALTER TABLE "Agents"
ADD COLUMN "ConfirmationPlugin" VARCHAR(200) NULL;

ALTER TABLE "Agents"
ADD COLUMN "ConfirmationExpirationMinutes" INTEGER NOT NULL DEFAULT 30;

ALTER TABLE "Agents"
ADD COLUMN "NotificationTimeoutHours" INTEGER NOT NULL DEFAULT 24;

ALTER TABLE "Agents"
ADD COLUMN "LoggingPlugins" VARCHAR(1000) NULL;

-- Drop deprecated ActivePlugins column (if it exists)
-- This column has been replaced by the PluginConfigurations table
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'Agents'
        AND column_name = 'ActivePlugins'
    ) THEN
        ALTER TABLE "Agents" DROP COLUMN "ActivePlugins";
    END IF;
END $$;

-- Update any existing agents to have default values
UPDATE "Agents"
SET
    "ConfirmationExpirationMinutes" = 30,
    "NotificationTimeoutHours" = 24
WHERE
    "ConfirmationExpirationMinutes" IS NULL
    OR "NotificationTimeoutHours" IS NULL;

-- Add comments to document the new columns
COMMENT ON COLUMN "Agents"."ConfirmationPlugin" IS 'Plugin name to use for confirmation requests (e.g., Discord, Email)';
COMMENT ON COLUMN "Agents"."ConfirmationExpirationMinutes" IS 'How long confirmation requests are valid (in minutes, default 30)';
COMMENT ON COLUMN "Agents"."NotificationTimeoutHours" IS 'Notification timeout in hours (default 24)';
COMMENT ON COLUMN "Agents"."LoggingPlugins" IS 'Comma-separated list of logging plugin names for this agent';
