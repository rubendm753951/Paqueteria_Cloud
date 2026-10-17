-- SiExPro product catalog, agency rates, and shipment order entry.
-- Run against the application's business database after taking a backup.
-- Existing c_agencias rows are referenced, never recreated or modified.

BEGIN;

CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE products (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sku_codigo varchar(50) NOT NULL,
    nombre varchar(160) NOT NULL,
    descripcion text,
    fecha_creacion timestamptz NOT NULL DEFAULT now(),
    fecha_baja timestamptz,
    estado varchar(12) NOT NULL DEFAULT 'activo',
    CONSTRAINT uq_products_sku UNIQUE (sku_codigo),
    CONSTRAINT ck_products_estado CHECK (estado IN ('activo', 'inactivo')),
    CONSTRAINT ck_products_baja CHECK (fecha_baja IS NULL OR fecha_baja >= fecha_creacion)
);

CREATE TABLE agency_products (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    id_agencia integer NOT NULL,
    id_producto uuid NOT NULL,
    moneda char(3) NOT NULL,
    incremento_peso_kg numeric(9,3) NOT NULL DEFAULT 1,
    cobertura_extendida_precio numeric(12,2) NOT NULL DEFAULT 0,
    cargo_dimensiones_extras_precio numeric(12,2) NOT NULL DEFAULT 0,
    lado_maximo_cm numeric(8,2) NOT NULL DEFAULT 120,
    suma_lados_maxima_cm numeric(8,2),
    activo boolean NOT NULL DEFAULT true,
    fecha_creacion timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_agency_products_agency FOREIGN KEY (id_agencia)
        REFERENCES c_agencias (id_agencia) ON DELETE RESTRICT,
    CONSTRAINT fk_agency_products_product FOREIGN KEY (id_producto)
        REFERENCES products (id) ON DELETE RESTRICT,
    CONSTRAINT uq_agency_products_pair UNIQUE (id_agencia, id_producto),
    CONSTRAINT uq_agency_products_identity UNIQUE (id, id_agencia, id_producto),
    CONSTRAINT ck_agency_products_increment CHECK (incremento_peso_kg > 0),
    CONSTRAINT ck_agency_products_charges CHECK (
        cobertura_extendida_precio >= 0 AND cargo_dimensiones_extras_precio >= 0
    ),
    CONSTRAINT ck_agency_products_dimensions CHECK (
        lado_maximo_cm > 0 AND (suma_lados_maxima_cm IS NULL OR suma_lados_maxima_cm > 0)
    )
);

CREATE TABLE agency_product_rate_ranges (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_agencia_producto uuid NOT NULL,
    peso_inicial_kg numeric(9,3) NOT NULL,
    peso_final_kg numeric(9,3),
    precio_base_rango numeric(12,2) NOT NULL,
    precio_kg_adicional numeric(12,2) NOT NULL DEFAULT 0,
    CONSTRAINT fk_rate_ranges_agency_product FOREIGN KEY (id_agencia_producto)
        REFERENCES agency_products (id) ON DELETE CASCADE,
    CONSTRAINT ck_rate_ranges_bounds CHECK (
        peso_inicial_kg >= 0 AND (peso_final_kg IS NULL OR peso_final_kg > peso_inicial_kg)
    ),
    CONSTRAINT ck_rate_ranges_price CHECK (
        precio_base_rango >= 0 AND precio_kg_adicional >= 0
    ),
    CONSTRAINT ex_rate_ranges_no_overlap EXCLUDE USING gist (
        id_agencia_producto WITH =,
        numrange(peso_inicial_kg, COALESCE(peso_final_kg, 'Infinity'::numeric), '[)') WITH &&
    )
);

CREATE INDEX ix_agency_products_product ON agency_products (id_producto);
CREATE INDEX ix_rate_ranges_lookup ON agency_product_rate_ranges (id_agencia_producto, peso_inicial_kg);

CREATE TABLE shipment_orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tracking_number varchar(40) NOT NULL,
    id_agencia integer NOT NULL,
    id_producto uuid NOT NULL,
    id_agencia_producto uuid NOT NULL,
    fecha_creacion timestamptz NOT NULL DEFAULT now(),
    estado_orden varchar(16) NOT NULL DEFAULT 'cotizada',
    cobertura_extendida boolean NOT NULL DEFAULT false,
    cobertura_extendida_precio_snapshot numeric(12,2) NOT NULL DEFAULT 0,
    total_envio numeric(12,2) NOT NULL DEFAULT 0,
    moneda char(3) NOT NULL,
    creada_por_user_id text,
    CONSTRAINT uq_shipment_orders_tracking UNIQUE (tracking_number),
    CONSTRAINT fk_orders_agency_product FOREIGN KEY (id_agencia_producto, id_agencia, id_producto)
        REFERENCES agency_products (id, id_agencia, id_producto) ON DELETE RESTRICT,
    CONSTRAINT fk_orders_created_by FOREIGN KEY (creada_por_user_id)
        REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL,
    CONSTRAINT ck_shipment_orders_status CHECK (
        estado_orden IN ('cotizada', 'confirmada', 'en_transito', 'entregada', 'cancelada')
    ),
    CONSTRAINT ck_shipment_orders_amounts CHECK (
        cobertura_extendida_precio_snapshot >= 0 AND total_envio >= 0
    )
);

CREATE INDEX ix_shipment_orders_agency_date ON shipment_orders (id_agencia, fecha_creacion DESC);
CREATE INDEX ix_shipment_orders_product ON shipment_orders (id_producto);
CREATE INDEX ix_shipment_orders_creator ON shipment_orders (creada_por_user_id);

CREATE TABLE shipment_addresses (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_orden uuid NOT NULL,
    tipo_contacto varchar(12) NOT NULL,
    pais char(2) NOT NULL DEFAULT 'MX',
    nombre_contacto varchar(160) NOT NULL,
    empresa varchar(160),
    direccion_linea1 varchar(200) NOT NULL,
    direccion_linea2 varchar(200),
    codigo_postal varchar(12) NOT NULL,
    colonia varchar(120) NOT NULL,
    municipio varchar(120) NOT NULL,
    estado varchar(120) NOT NULL,
    ciudad varchar(120),
    telefono varchar(30) NOT NULL,
    email varchar(254),
    tax_id_rfc varchar(20),
    CONSTRAINT fk_shipment_addresses_order FOREIGN KEY (id_orden)
        REFERENCES shipment_orders (id) ON DELETE CASCADE,
    CONSTRAINT uq_shipment_addresses_order_type UNIQUE (id_orden, tipo_contacto),
    CONSTRAINT ck_shipment_addresses_type CHECK (tipo_contacto IN ('remitente', 'destinatario'))
);

CREATE TABLE shipment_packages (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_orden uuid NOT NULL,
    secuencia_paquete smallint NOT NULL,
    peso_bascula_kg numeric(9,3) NOT NULL,
    peso_volumetrico_kg numeric(9,3) NOT NULL,
    peso_cobrable_kg numeric(9,3) NOT NULL,
    largo_cm numeric(8,2) NOT NULL,
    ancho_cm numeric(8,2) NOT NULL,
    alto_cm numeric(8,2) NOT NULL,
    descripcion_contenido varchar(240) NOT NULL,
    es_dimension_extra boolean NOT NULL DEFAULT false,
    tarifa_base_snapshot numeric(12,2) NOT NULL,
    cargo_kg_adicional_snapshot numeric(12,2) NOT NULL DEFAULT 0,
    cargo_dimension_extra_snapshot numeric(12,2) NOT NULL DEFAULT 0,
    total_paquete_snapshot numeric(12,2) NOT NULL,
    CONSTRAINT fk_shipment_packages_order FOREIGN KEY (id_orden)
        REFERENCES shipment_orders (id) ON DELETE CASCADE,
    CONSTRAINT uq_shipment_packages_sequence UNIQUE (id_orden, secuencia_paquete),
    CONSTRAINT ck_shipment_packages_sequence CHECK (secuencia_paquete BETWEEN 1 AND 10),
    CONSTRAINT ck_shipment_packages_weights CHECK (
        peso_bascula_kg > 0 AND peso_volumetrico_kg >= 0 AND peso_cobrable_kg > 0
    ),
    CONSTRAINT ck_shipment_packages_dimensions CHECK (
        largo_cm > 0 AND ancho_cm > 0 AND alto_cm > 0
    ),
    CONSTRAINT ck_shipment_packages_amounts CHECK (
        tarifa_base_snapshot >= 0 AND cargo_kg_adicional_snapshot >= 0 AND
        cargo_dimension_extra_snapshot >= 0 AND total_paquete_snapshot >= 0
    )
);

CREATE INDEX ix_shipment_packages_order ON shipment_packages (id_orden);

COMMIT;


SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_name IN (
  'products', 'agency_products', 'agency_product_rate_ranges',
  'shipment_orders', 'shipment_addresses', 'shipment_packages'
)
ORDER BY table_schema, table_name;


UPDATE user_page_permissions p
SET can_view = true,
    can_create = true,
    can_edit = true
FROM "AspNetUsers" u
WHERE p.user_id = u."Id"
  AND lower(u."Email") = lower('rubendm@hotmail.com')
  AND p.page_key = 'Agencias'
RETURNING p.page_key, p.can_view, p.can_create, p.can_edit, p.can_delete;


SELECT ap.id_agencia, ap.id_producto, p.sku_codigo, ap.activo
FROM agency_products AS ap
JOIN products AS p ON p.id = ap.id_producto
ORDER BY ap.id_agencia, p.sku_codigo;