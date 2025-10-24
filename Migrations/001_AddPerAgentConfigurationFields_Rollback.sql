-- Migration Rollback: AddPerAgentConfigurationFields
-- Date: 2024-10-24
-- Description: Rollback per-agent configuration fields migration

-- Remove the columns added in the forward migration
ALTER TABLE "Agents" DROP COLUMN IF EXISTS "ConfirmationPlugin";
ALTER TABLE "Agents" DROP COLUMN IF EXISTS "ConfirmationExpirationMinutes";
ALTER TABLE "Agents" DROP COLUMN IF EXISTS "NotificationTimeoutHours";
ALTER TABLE "Agents" DROP COLUMN IF EXISTS "LoggingPlugins";

-- Note: This rollback does NOT restore the ActivePlugins column
-- as that field is deprecated and should not be used going forward.
-- If you need to restore it, you'll need to manually add it:
-- ALTER TABLE "Agents" ADD COLUMN "ActivePlugins" VARCHAR(1000) NULL;
