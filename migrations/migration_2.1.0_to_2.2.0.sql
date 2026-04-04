-- =============================================================================
-- Less3 Migration: v2.1.0 to v2.2.0
--
-- Description: Creates the requesthistory table for tracking API request
--              history and metrics. Adds indexes on commonly queried columns.
--
-- Database:    SQLite
-- Date:        2026-04-03
-- =============================================================================

CREATE TABLE IF NOT EXISTS requesthistory (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    guid                NVARCHAR(64)   NOT NULL,
    httpmethod          NVARCHAR(16)   NOT NULL,
    requesturl          NVARCHAR(2048) NOT NULL,
    sourceip            NVARCHAR(64)       NULL,
    statuscode          INTEGER        NOT NULL DEFAULT 0,
    success             BOOLEAN        NOT NULL DEFAULT 1,
    durationms          BIGINT         NOT NULL DEFAULT 0,
    requesttype         NVARCHAR(64)       NULL,
    userguid            NVARCHAR(64)       NULL,
    accesskey           NVARCHAR(256)      NULL,
    requestcontenttype  NVARCHAR(128)      NULL,
    requestbodylength   BIGINT         NOT NULL DEFAULT 0,
    responsecontenttype NVARCHAR(128)      NULL,
    responsebodylength  BIGINT         NOT NULL DEFAULT 0,
    createdutc          DATETIME       NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_requesthistory_createdutc
    ON requesthistory (createdutc);

CREATE INDEX IF NOT EXISTS idx_requesthistory_httpmethod
    ON requesthistory (httpmethod);

CREATE INDEX IF NOT EXISTS idx_requesthistory_statuscode
    ON requesthistory (statuscode);

CREATE INDEX IF NOT EXISTS idx_requesthistory_success
    ON requesthistory (success);
