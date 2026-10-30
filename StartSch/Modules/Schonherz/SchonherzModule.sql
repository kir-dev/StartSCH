-- Recreates the data initialized by SchonherzModuleInitializer.
-- PostgreSQL script. Safe to run more than once.
--
-- IDs of rows created by this script are recorded in the
-- "_SchonherzModuleRollback" table so that SchonherzModule.revert.sql
-- can undo exactly those inserts after they have been committed.

BEGIN;

CREATE TABLE IF NOT EXISTS "_SchonherzModuleRollback" (
    "Kind" text    NOT NULL,
    "A"    integer NOT NULL,
    "B"    integer NOT NULL DEFAULT 0,
    PRIMARY KEY ("Kind", "A", "B")
);

DO $$
DECLARE
    sch_page_id             integer;
    default_category_id     integer;
    tanfolyamok_id          integer;
    szakmai_id              integer;
    kajas_id                integer;
    hazszintu_id            integer;
    kir_page_id             integer;
    kir_default_category_id integer;
    kir_tanfolyam_id        integer;
BEGIN
    -- Page: Schönherz
    SELECT "Id"
    INTO sch_page_id
    FROM "Pages"
    WHERE "Name" = 'Schönherz'
    ORDER BY "Id"
    LIMIT 1;

    IF sch_page_id IS NULL THEN
        INSERT INTO "Pages" ("Created", "Updated", "Name")
        VALUES (now(), now(), 'Schönherz')
        RETURNING "Id" INTO sch_page_id;

        INSERT INTO "_SchonherzModuleRollback" ("Kind", "A")
        VALUES ('page', sch_page_id)
        ON CONFLICT DO NOTHING;
    END IF;

    -- Default category of the Schönherz page (Name is NULL in the initializer)
    SELECT "Id"
    INTO default_category_id
    FROM "Categories"
    WHERE "PageId" = sch_page_id
      AND "Name" IS NULL
    ORDER BY "Id"
    LIMIT 1;

    IF default_category_id IS NULL THEN
        INSERT INTO "Categories" ("PageId", "Name")
        VALUES (sch_page_id, NULL)
        RETURNING "Id" INTO default_category_id;

        INSERT INTO "_SchonherzModuleRollback" ("Kind", "A")
        VALUES ('category', default_category_id)
        ON CONFLICT DO NOTHING;
    END IF;

    -- Category: Tanfolyamok (child of the default category)
    SELECT "Id"
    INTO tanfolyamok_id
    FROM "Categories"
    WHERE "PageId" = sch_page_id
      AND "Name" = 'Tanfolyamok'
    ORDER BY "Id"
    LIMIT 1;

    IF tanfolyamok_id IS NULL THEN
        INSERT INTO "Categories" ("PageId", "Name")
        VALUES (sch_page_id, 'Tanfolyamok')
        RETURNING "Id" INTO tanfolyamok_id;

        INSERT INTO "_SchonherzModuleRollback" ("Kind", "A")
        VALUES ('category', tanfolyamok_id)
        ON CONFLICT DO NOTHING;
    END IF;

    -- Category: Szakmai tanfolyamok
    SELECT "Id"
    INTO szakmai_id
    FROM "Categories"
    WHERE "PageId" = sch_page_id
      AND "Name" = 'Szakmai tanfolyamok'
    ORDER BY "Id"
    LIMIT 1;

    IF szakmai_id IS NULL THEN
        INSERT INTO "Categories" ("PageId", "Name")
        VALUES (sch_page_id, 'Szakmai tanfolyamok')
        RETURNING "Id" INTO szakmai_id;

        INSERT INTO "_SchonherzModuleRollback" ("Kind", "A")
        VALUES ('category', szakmai_id)
        ON CONFLICT DO NOTHING;
    END IF;

    -- Category: Kajás tanfolyamok
    SELECT "Id"
    INTO kajas_id
    FROM "Categories"
    WHERE "PageId" = sch_page_id
      AND "Name" = 'Kajás tanfolyamok'
    ORDER BY "Id"
    LIMIT 1;

    IF kajas_id IS NULL THEN
        INSERT INTO "Categories" ("PageId", "Name")
        VALUES (sch_page_id, 'Kajás tanfolyamok')
        RETURNING "Id" INTO kajas_id;

        INSERT INTO "_SchonherzModuleRollback" ("Kind", "A")
        VALUES ('category', kajas_id)
        ON CONFLICT DO NOTHING;
    END IF;

    -- Category: Házszintű tanfolyamok
    SELECT "Id"
    INTO hazszintu_id
    FROM "Categories"
    WHERE "PageId" = sch_page_id
      AND "Name" = 'Házszintű tanfolyamok'
    ORDER BY "Id"
    LIMIT 1;

    IF hazszintu_id IS NULL THEN
        INSERT INTO "Categories" ("PageId", "Name")
        VALUES (sch_page_id, 'Házszintű tanfolyamok')
        RETURNING "Id" INTO hazszintu_id;

        INSERT INTO "_SchonherzModuleRollback" ("Kind", "A")
        VALUES ('category', hazszintu_id)
        ON CONFLICT DO NOTHING;
    END IF;

    -- The KIR developer page is expected to already exist.
    SELECT "Id"
    INTO kir_page_id
    FROM "Pages"
    WHERE "PekName" = 'KIR fejlesztők és üzemeltetők'
    ORDER BY "Id"
    LIMIT 1;

    IF kir_page_id IS NULL THEN
        RAISE EXCEPTION 'Page with PekName ''KIR fejlesztők és üzemeltetők'' was not found';
    END IF;

    -- Default category of the KIR developer page (Name is NULL in the initializer)
    SELECT "Id"
    INTO kir_default_category_id
    FROM "Categories"
    WHERE "PageId" = kir_page_id
      AND "Name" IS NULL
    ORDER BY "Id"
    LIMIT 1;

    IF kir_default_category_id IS NULL THEN
        INSERT INTO "Categories" ("PageId", "Name")
        VALUES (kir_page_id, NULL)
        RETURNING "Id" INTO kir_default_category_id;

        INSERT INTO "_SchonherzModuleRollback" ("Kind", "A")
        VALUES ('category', kir_default_category_id)
        ON CONFLICT DO NOTHING;
    END IF;

    -- Category: Tanfolyam (on the KIR developer page)
    SELECT "Id"
    INTO kir_tanfolyam_id
    FROM "Categories"
    WHERE "PageId" = kir_page_id
      AND "Name" = 'Tanfolyam'
    ORDER BY "Id"
    LIMIT 1;

    IF kir_tanfolyam_id IS NULL THEN
        INSERT INTO "Categories" ("PageId", "Name")
        VALUES (kir_page_id, 'Tanfolyam')
        RETURNING "Id" INTO kir_tanfolyam_id;

        INSERT INTO "_SchonherzModuleRollback" ("Kind", "A")
        VALUES ('category', kir_tanfolyam_id)
        ON CONFLICT DO NOTHING;
    END IF;

    -- Category includes (IncluderId -> IncludedId).
    -- Only includes actually inserted by this run are recorded for rollback.
    WITH inserted_includes AS (
        INSERT INTO "CategoryIncludes" ("IncluderId", "IncludedId")
        VALUES
            (default_category_id, tanfolyamok_id),
            (tanfolyamok_id, szakmai_id),
            (tanfolyamok_id, kajas_id),
            (tanfolyamok_id, hazszintu_id),
            (kir_default_category_id, kir_tanfolyam_id),
            (szakmai_id, kir_tanfolyam_id)
        ON CONFLICT ("IncludedId", "IncluderId") DO NOTHING
        RETURNING "IncluderId", "IncludedId"
    )
    INSERT INTO "_SchonherzModuleRollback" ("Kind", "A", "B")
    SELECT 'include', "IncluderId", "IncludedId"
    FROM inserted_includes
    ON CONFLICT DO NOTHING;
END $$;

COMMIT;
