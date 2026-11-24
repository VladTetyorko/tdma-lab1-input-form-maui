-- StockRoom schema, version 1. Matches docs/domain/domain-model.md (third normal form).
-- Conventions: UUIDs and enums are TEXT, timestamps are ISO-8601 UTC TEXT ("2026-01-31T10:15:00.0000000Z").
-- Rules of this file: plain statements separated by a semicolon, no triggers (MigrationRunner runs them one by one).

CREATE TABLE Users (
    Id        TEXT NOT NULL PRIMARY KEY,
    FullName  TEXT NOT NULL CHECK (length(trim(FullName)) > 0),
    Role      TEXT NOT NULL CHECK (Role IN ('Storekeeper', 'Driver', 'Manager')),
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE Products (
    Id              TEXT NOT NULL PRIMARY KEY,
    Name            TEXT NOT NULL CHECK (length(trim(Name)) > 0),
    Barcode         TEXT NOT NULL UNIQUE,
    CreatedByUserId TEXT NOT NULL REFERENCES Users (Id),
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);

CREATE TABLE Characteristics (
    Id              TEXT NOT NULL PRIMARY KEY,
    Name            TEXT NOT NULL UNIQUE COLLATE NOCASE CHECK (length(trim(Name)) > 0),
    ValueType       TEXT NOT NULL CHECK (ValueType IN ('Text', 'Number')),
    CreatedByUserId TEXT NOT NULL REFERENCES Users (Id),
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);

CREATE TABLE ProductCharacteristics (
    Id               TEXT NOT NULL PRIMARY KEY,
    ProductId        TEXT NOT NULL REFERENCES Products (Id),
    CharacteristicId TEXT NOT NULL REFERENCES Characteristics (Id),
    Value            TEXT NOT NULL,
    CreatedByUserId  TEXT NOT NULL REFERENCES Users (Id),
    CreatedAt        TEXT NOT NULL,
    UpdatedAt        TEXT NOT NULL,
    UNIQUE (ProductId, CharacteristicId)
);

-- The only way quantity changes. Quantity on hand is never stored: it is SUM(In) - SUM(Out).
-- Counterparty is personal data and holds AES-encrypted base64 text (see AesFieldEncryptor).
CREATE TABLE Operations (
    Id              TEXT NOT NULL PRIMARY KEY,
    ProductId       TEXT NOT NULL REFERENCES Products (Id),
    Direction       TEXT NOT NULL CHECK (Direction IN ('In', 'Out')),
    Quantity        INTEGER NOT NULL CHECK (Quantity > 0),
    Counterparty    TEXT NULL,
    Reason          TEXT NULL,
    CreatedByUserId TEXT NOT NULL REFERENCES Users (Id),
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);

CREATE INDEX IX_Operations_ProductId ON Operations (ProductId);
CREATE INDEX IX_ProductCharacteristics_CharacteristicId ON ProductCharacteristics (CharacteristicId);

-- Infrastructure, not domain: the offline-first outbox. One row per local write that the server has not
-- received yet. PayloadJson is a deliberate snapshot of the written row (see docs/steps/04-sqlite.md).
CREATE TABLE SyncQueue (
    Id          TEXT NOT NULL PRIMARY KEY,
    EntityType  TEXT NOT NULL,
    EntityId    TEXT NOT NULL,
    Operation   TEXT NOT NULL CHECK (Operation IN ('Insert', 'Update', 'Delete')),
    PayloadJson TEXT NOT NULL,
    CreatedAt   TEXT NOT NULL,
    UpdatedAt   TEXT NOT NULL
);

CREATE INDEX IX_SyncQueue_CreatedAt ON SyncQueue (CreatedAt);
