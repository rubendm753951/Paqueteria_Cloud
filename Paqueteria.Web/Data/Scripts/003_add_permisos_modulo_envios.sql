BEGIN;

DO $$
DECLARE
    target_user_id text;
    v_agency_id integer;
BEGIN
    SELECT "Id"
    INTO STRICT target_user_id
    FROM "AspNetUsers"
    WHERE lower("Email") = lower('rubendm@hotmail.com');

    -- Permiso para visualizar y operar el módulo
    INSERT INTO user_page_permissions
        (user_id, page_key, can_view, can_create, can_edit, can_delete)
    VALUES
        (target_user_id, 'Envios', true, true, true, false)
    ON CONFLICT (user_id, page_key)
    DO UPDATE SET
        can_view = true,
        can_create = true,
        can_edit = true;

    -- Reemplaza estos IDs por las agencias que administras
    FOREACH v_agency_id IN ARRAY ARRAY[1432, 1433]
    LOOP
        INSERT INTO user_agency_permissions
            (user_id, agency_id, can_view, can_edit, can_delete)
        VALUES
            (target_user_id, v_agency_id, true, true, false)
        ON CONFLICT (user_id, agency_id)
        DO UPDATE SET
            can_view = true,
            can_edit = true;
    END LOOP;
END
$$;

COMMIT;
-- ROLLBACK
