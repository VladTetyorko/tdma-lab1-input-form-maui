-- Redesign: batch receipts and issues.
-- StockDocuments: one receipt or issue; its lines are Operations rows pointing to it through DocumentId.
-- Counterparty holds AES-encrypted base64 text, like Operations.Counterparty. Documents are never changed.
-- SQLite accepts ADD COLUMN with REFERENCES because the default is NULL, so existing operations stay as they are.

CREATE TABLE StockDocuments (
    Id              TEXT NOT NULL PRIMARY KEY,
    Number          TEXT NOT NULL UNIQUE,
    Direction       TEXT NOT NULL CHECK (Direction IN ('In', 'Out')),
    Counterparty    TEXT NULL,
    Note            TEXT NULL,
    CreatedByUserId TEXT NOT NULL REFERENCES Users (Id),
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);

ALTER TABLE Operations ADD COLUMN DocumentId TEXT NULL REFERENCES StockDocuments (Id);

CREATE INDEX IX_Operations_DocumentId ON Operations (DocumentId);
