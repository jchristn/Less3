-- =============================================================================
-- Less3 Migration: v2.2.0 to v2.3.0
--
-- Description: Adds requestbody and responsebody columns to the requesthistory
--              table for storing captured request and response body content.
--              Bodies are truncated to 16 KB at capture time.
--
-- Database:    SQLite
-- Date:        2026-04-03
-- =============================================================================

ALTER TABLE requesthistory ADD COLUMN requestbody TEXT;
ALTER TABLE requesthistory ADD COLUMN responsebody TEXT;
