-- Lab 7: device capabilities.
-- Products.PhotoPath: path of the product photo RELATIVE to the app data folder ("photos/<barcode>_<yyyyMMdd_HHmmss>.jpg").
--   Only the path is stored (and synced); the file itself stays on the device.
-- Operations.Latitude / Longitude: where an In/Out was recorded, both set or both empty. Operations stay immutable.
-- SQLite accepts a CHECK on an added column (the rule is also tested against the existing rows, which all pass: NULL).
-- The cross-column rule sits on Longitude, the later column, because it refers to Latitude.

ALTER TABLE Products ADD COLUMN PhotoPath TEXT
    CHECK (PhotoPath IS NULL OR (length(trim(PhotoPath)) > 0 AND substr(PhotoPath, 1, 1) NOT IN ('/', '\') AND instr(PhotoPath, '..') = 0));

ALTER TABLE Operations ADD COLUMN Latitude REAL
    CHECK (Latitude IS NULL OR Latitude BETWEEN -90 AND 90);

ALTER TABLE Operations ADD COLUMN Longitude REAL
    CHECK ((Latitude IS NULL) = (Longitude IS NULL) AND (Longitude IS NULL OR Longitude BETWEEN -180 AND 180));
