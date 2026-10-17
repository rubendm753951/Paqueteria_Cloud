-- Grant access to the catalog and order-entry pages for one Identity user.
-- Replace the email and agency IDs. Existing grants are preserved except for
-- the permissions explicitly set below.

BEGIN;

DO $$
DECLARE
    target_user_id text;
    target_agency_id integer;
    requested_agencies integer[] := ARRAY[1432, 1433];
BEGIN
    SELECT "Id"
    INTO STRICT target_user_id
    FROM "AspNetUsers"
    WHERE lower("Email") = lower('rubendm@hotmail.com');

    INSERT INTO user_page_permissions
        (user_id, page_key, can_view, can_create, can_edit, can_delete)
    VALUES
        (target_user_id, 'Agencias', true, true, true, false)
    ON CONFLICT (user_id, page_key)
    DO UPDATE SET
        can_view = true,
        can_create = true,
        can_edit = true;

    INSERT INTO user_page_permissions
        (user_id, page_key, can_view, can_create, can_edit, can_delete)
    VALUES
        (target_user_id, 'Productos', true, true, true, true),
        (target_user_id, 'Ordenes', true, true, false, false)
    ON CONFLICT (user_id, page_key)
    DO UPDATE SET
        can_view = EXCLUDED.can_view,
        can_create = EXCLUDED.can_create,
        can_edit = EXCLUDED.can_edit,
        can_delete = EXCLUDED.can_delete;

    FOREACH target_agency_id IN ARRAY requested_agencies
    LOOP
        INSERT INTO user_agency_permissions
            (user_id, agency_id, can_view, can_edit, can_delete)
        VALUES
            (target_user_id, target_agency_id, true, true, false)
        ON CONFLICT (user_id, agency_id)
        DO UPDATE SET
            can_view = EXCLUDED.can_view,
            can_edit = EXCLUDED.can_edit,
            can_delete = EXCLUDED.can_delete;
    END LOOP;
END
$$;

COMMIT;


