-- Public tracking subscriptions. These contacts are not ASP.NET Identity users.
BEGIN;

CREATE TABLE IF NOT EXISTS tracking_subscriptions (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tracking_number varchar(40) NOT NULL,
    email varchar(254) NOT NULL,
    confirmation_token varchar(80) NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'pending',
    created_at timestamptz NOT NULL DEFAULT now(),
    confirmed_at timestamptz,
    unsubscribed_at timestamptz,
    CONSTRAINT uq_tracking_subscriptions_tracking_email UNIQUE (tracking_number, email),
    CONSTRAINT uq_tracking_subscriptions_token UNIQUE (confirmation_token),
    CONSTRAINT ck_tracking_subscriptions_status CHECK (status IN ('pending', 'confirmed', 'unsubscribed'))
);

CREATE INDEX IF NOT EXISTS ix_tracking_subscriptions_tracking
    ON tracking_subscriptions (tracking_number, status);

COMMIT;
ROLLBACK