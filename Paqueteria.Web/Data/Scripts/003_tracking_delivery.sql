-- SiExPro tracking, drivers, delivery attempts and proof of delivery.
-- Execute after 001_products_orders.sql against the business database.
BEGIN;

ALTER TABLE shipment_orders
    ALTER COLUMN estado_orden TYPE varchar(24);

ALTER TABLE shipment_orders
    DROP CONSTRAINT IF EXISTS ck_shipment_orders_status;

ALTER TABLE shipment_orders
    ADD CONSTRAINT ck_shipment_orders_status CHECK (
        estado_orden IN (
            'cotizada', 'confirmada', 'listo_despacho', 'asignada',
            'en_transito', 'intento_fallido', 'en_retencion',
            'entregada', 'cancelada', 'devuelta'
        )
    );

ALTER TABLE shipment_orders
    ADD COLUMN IF NOT EXISTS assigned_driver_id bigint,
    ADD COLUMN IF NOT EXISTS delivered_at timestamptz;

CREATE TABLE IF NOT EXISTS delivery_drivers (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    agency_id integer NOT NULL,
    employee_code varchar(50) NOT NULL,
    first_name varchar(100) NOT NULL,
    last_name varchar(100) NOT NULL,
    phone varchar(30) NOT NULL,
    license_number varchar(80),
    status varchar(20) NOT NULL DEFAULT 'activo',
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_delivery_drivers_agency FOREIGN KEY (agency_id)
        REFERENCES c_agencias(id_agencia) ON DELETE RESTRICT,
    CONSTRAINT uq_delivery_drivers_agency_code UNIQUE (agency_id, employee_code),
    CONSTRAINT uq_delivery_drivers_agency_id UNIQUE (agency_id, id),
    CONSTRAINT ck_delivery_drivers_status CHECK (status IN ('activo', 'inactivo', 'suspendido'))
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_shipment_orders_agency_id
    ON shipment_orders (id_agencia, id);

ALTER TABLE shipment_orders
    DROP CONSTRAINT IF EXISTS fk_shipment_orders_driver;

ALTER TABLE shipment_orders
    ADD CONSTRAINT fk_shipment_orders_driver
    FOREIGN KEY (id_agencia, assigned_driver_id)
    REFERENCES delivery_drivers (agency_id, id) ON DELETE RESTRICT;

CREATE TABLE IF NOT EXISTS shipment_tracking_events (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    agency_id integer NOT NULL,
    shipment_order_id uuid NOT NULL,
    event_code varchar(40) NOT NULL,
    previous_status varchar(24),
    new_status varchar(24) NOT NULL,
    event_at timestamptz NOT NULL DEFAULT now(),
    location_text varchar(240),
    description varchar(500),
    created_by_user_id text,
    CONSTRAINT fk_tracking_events_order FOREIGN KEY (agency_id, shipment_order_id)
        REFERENCES shipment_orders (id_agencia, id) ON DELETE CASCADE,
    CONSTRAINT fk_tracking_events_user FOREIGN KEY (created_by_user_id)
        REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS ix_tracking_events_lookup
    ON shipment_tracking_events (agency_id, shipment_order_id, event_at DESC);

CREATE TABLE IF NOT EXISTS delivery_attempts (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    agency_id integer NOT NULL,
    shipment_order_id uuid NOT NULL,
    driver_id bigint,
    attempt_number integer NOT NULL,
    attempted_at timestamptz NOT NULL DEFAULT now(),
    result_code varchar(40) NOT NULL,
    notes varchar(1000),
    next_action varchar(30) NOT NULL DEFAULT 'reintentar',
    CONSTRAINT fk_delivery_attempts_order FOREIGN KEY (agency_id, shipment_order_id)
        REFERENCES shipment_orders (id_agencia, id) ON DELETE CASCADE,
    CONSTRAINT fk_delivery_attempts_driver FOREIGN KEY (agency_id, driver_id)
        REFERENCES delivery_drivers (agency_id, id) ON DELETE RESTRICT,
    CONSTRAINT ck_delivery_attempts_number CHECK (attempt_number > 0),
    CONSTRAINT ck_delivery_attempts_result CHECK (result_code IN (
        'cliente_ausente', 'direccion_incorrecta', 'negocio_cerrado',
        'cliente_rechazo', 'sin_acceso', 'paquete_danado', 'otro'
    )),
    CONSTRAINT ck_delivery_attempts_next_action CHECK (next_action IN (
        'reintentar', 'retener', 'devolver'
    ))
);

CREATE INDEX IF NOT EXISTS ix_delivery_attempts_lookup
    ON delivery_attempts (agency_id, shipment_order_id, attempted_at DESC);

CREATE TABLE IF NOT EXISTS proofs_of_delivery (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    agency_id integer NOT NULL,
    shipment_order_id uuid NOT NULL,
    driver_id bigint,
    received_by_name varchar(160) NOT NULL,
    relationship varchar(80),
    delivered_at timestamptz NOT NULL DEFAULT now(),
    delivery_notes varchar(1000),
    CONSTRAINT fk_pod_order FOREIGN KEY (agency_id, shipment_order_id)
        REFERENCES shipment_orders (id_agencia, id) ON DELETE CASCADE,
    CONSTRAINT fk_pod_driver FOREIGN KEY (agency_id, driver_id)
        REFERENCES delivery_drivers (agency_id, id) ON DELETE RESTRICT,
    CONSTRAINT uq_pod_order UNIQUE (agency_id, shipment_order_id)
);

INSERT INTO shipment_tracking_events
    (agency_id, shipment_order_id, event_code, previous_status, new_status,
     event_at, description, created_by_user_id)
SELECT o.id_agencia, o.id, 'SHIPMENT_CREATED', NULL, o.estado_orden,
       o.fecha_creacion, 'Orden importada al historial de seguimiento', o.creada_por_user_id
FROM shipment_orders o
WHERE NOT EXISTS (
    SELECT 1 FROM shipment_tracking_events e
    WHERE e.agency_id = o.id_agencia
      AND e.shipment_order_id = o.id
);

-- Permiso inicial para el usuario de desarrollo ya utilizado por SiExPro.
-- Ajustar el correo si la instalación utiliza otro usuario administrador.
COMMIT;

-- Después de ejecutar el script, otorgar la página "Envios" y los permisos
-- de agencia can_view/can_edit al usuario correspondiente.

-- ROLLBACK


SELECT table_name
FROM information_schema.tables
WHERE table_name IN (
    'delivery_drivers',
    'shipment_tracking_events',
    'delivery_attempts',
    'proofs_of_delivery'
);