-- Lab 6: local accounts. Users get a login name and a password HASH (never a password).
-- Hash format: pbkdf2-sha256$iterations$saltBase64$hashBase64 (see Services/Pbkdf2PasswordHasher.cs).
-- SQLite cannot add a UNIQUE column, so uniqueness is a unique index; NOCASE makes "Olena" and "olena" the same login.
-- The columns are nullable in the table only because ALTER TABLE cannot add NOT NULL without a default;
-- the model (User.Login, User.PasswordHash) is not nullable and the repository writes both on every insert.

ALTER TABLE Users ADD COLUMN Login TEXT;

ALTER TABLE Users ADD COLUMN PasswordHash TEXT;

CREATE UNIQUE INDEX UX_Users_Login ON Users (Login COLLATE NOCASE);

-- The three demo users of 0002_seed.sql. The demo passwords are listed in README.md ("Demo accounts");
-- only their hashes are stored here.
UPDATE Users SET Login = 'olena', PasswordHash = 'pbkdf2-sha256$210000$spPyzIBlCGraCSGwk1sqUQ==$fFCZF2mEzJp6pdOC5BIo3kwQyVLNV5Cn8/aYlOWyXck='
    WHERE Id = '0196a000-0000-7000-8000-000000000001';

UPDATE Users SET Login = 'petro', PasswordHash = 'pbkdf2-sha256$210000$tW4l5CDg/+3B2QICbRzU3A==$b+1umb/jknps/7DzJV4ZZ+2YyfrZBH2LQuGec4XiN4g='
    WHERE Id = '0196a000-0000-7000-8000-000000000002';

UPDATE Users SET Login = 'ivan', PasswordHash = 'pbkdf2-sha256$210000$IeC7gfyQ3jKb/dDvgR4u3Q==$3TEhxx4iffLRbnOGLtWAM3oqWp29XCGVsx0gppkGBCI='
    WHERE Id = '0196a000-0000-7000-8000-000000000003';
