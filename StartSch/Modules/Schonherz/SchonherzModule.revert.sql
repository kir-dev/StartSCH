-- Reverts the rows created by SchonherzModule.sql.
--
-- This only removes rows that the apply script inserted. Pre-existing pages,
-- categories, and category includes are left untouched.
--
-- Run this after committing SchonherzModule.sql, once you have finished
-- previewing the changes in the UI.

BEGIN;

DO $$
BEGIN
    IF to_regclass('"_SchonherzModuleRollback"') IS NULL THEN
        RAISE NOTICE 'No "_SchonherzModuleRollback" table found; nothing to revert';
        RETURN;
    END IF;

    -- Remove category includes inserted by the apply script first.
    DELETE FROM "CategoryIncludes" ci
    USING "_SchonherzModuleRollback" l
    WHERE l."Kind" = 'include'
      AND ci."IncluderId" = l."A"
      AND ci."IncludedId" = l."B";

    -- Remove categories inserted by the apply script.
    -- Any remaining related rows (interests, event/post links, etc.) are
    -- removed by the existing ON DELETE CASCADE constraints.
    DELETE FROM "Categories" c
    USING "_SchonherzModuleRollback" l
    WHERE l."Kind" = 'category'
      AND c."Id" = l."A";

    -- Remove the page inserted by the apply script.
    DELETE FROM "Pages" p
    USING "_SchonherzModuleRollback" l
    WHERE l."Kind" = 'page'
      AND p."Id" = l."A";
END $$;

DROP TABLE IF EXISTS "_SchonherzModuleRollback";

COMMIT;
