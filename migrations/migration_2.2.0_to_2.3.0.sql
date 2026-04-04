-- =============================================================================
-- Less3 Migration: v2.2.0 to v2.3.0
--
-- Description: Migrates WatsonORM schema to custom database driver schema.
--              Renames columns that changed between WatsonORM and the new
--              driver layer, adds missing columns, and adds request/response
--              body capture columns to requesthistory.
--
-- Database:    SQLite
-- Date:        2026-04-03
-- =============================================================================

-- objects: add expirationutc column
ALTER TABLE objects ADD COLUMN expirationutc VARCHAR(64);

-- bucketacls: rename permitfullcontrol -> fullcontrol
ALTER TABLE bucketacls RENAME COLUMN permitfullcontrol TO fullcontrol;

-- objectacls: rename permitfullcontrol -> fullcontrol
ALTER TABLE objectacls RENAME COLUMN permitfullcontrol TO fullcontrol;

-- buckettags: rename tagkey -> key, tagvalue -> value
ALTER TABLE buckettags RENAME COLUMN tagkey TO key;
ALTER TABLE buckettags RENAME COLUMN tagvalue TO value;

-- objecttags: rename tagkey -> key, tagvalue -> value
ALTER TABLE objecttags RENAME COLUMN tagkey TO key;
ALTER TABLE objecttags RENAME COLUMN tagvalue TO value;

-- uploadparts: rename md5 -> md5hash, sha1 -> sha1hash, sha256 -> sha256hash
ALTER TABLE uploadparts RENAME COLUMN md5 TO md5hash;
ALTER TABLE uploadparts RENAME COLUMN sha1 TO sha1hash;
ALTER TABLE uploadparts RENAME COLUMN sha256 TO sha256hash;

-- requesthistory: add request/response body columns
ALTER TABLE requesthistory ADD COLUMN requestbody TEXT;
ALTER TABLE requesthistory ADD COLUMN responsebody TEXT;
