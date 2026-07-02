-- 1. Infrastructure: Roles and Extensions
CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD 'replicator_pass';
CREATE EXTENSION IF NOT EXISTS pgcrypto;
-- 2. Schema: Products table
CREATE TABLE products (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(200) NOT NULL,
    sku         VARCHAR(50) NOT NULL UNIQUE,
    category    VARCHAR(100),
    quantity    INTEGER NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    min_alert   INTEGER NOT NULL DEFAULT 10,
    price       DECIMAL(18,2) NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ DEFAULT NOW(),
    updated_at  TIMESTAMPTZ DEFAULT NOW()
);

-- 3. Schema: Transactions table
CREATE TABLE transactions (
    id          SERIAL PRIMARY KEY,
    product_id  INTEGER NOT NULL REFERENCES products(id),
    type        VARCHAR(20) NOT NULL CHECK (type IN ('sale', 'purchase', 'adjustment')),
    quantity    INTEGER NOT NULL,
    actor       VARCHAR(100),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- 4. Schema: Reconciliation log
CREATE TABLE reconciliation_log (
    id          SERIAL PRIMARY KEY,
    product_id  INTEGER NOT NULL REFERENCES products(id),
    old_qty     INTEGER NOT NULL,
    new_qty     INTEGER NOT NULL,
    reason      TEXT,
    worker_id   VARCHAR(100),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- 5. Optimization: Indexes
CREATE INDEX idx_transactions_product_id ON transactions(product_id);
CREATE INDEX idx_transactions_created_at ON transactions(created_at);
CREATE INDEX idx_products_category ON products(category);

-- 6. Seed data
INSERT INTO products (name, sku, category, quantity, min_alert, price) VALUES
    ('Laptop Dell XPS 13', 'DELL-XPS-13', 'Electronics', 50, 5, 1200.00),
    ('Wireless Mouse', 'LOGI-M123', 'Peripherals', 200, 20, 25.00),
    ('USB-C Hub', 'ANKER-HUB', 'Peripherals', 30, 10, 45.00),
    ('Monitor 27"', 'LG-27-4K', 'Electronics', 15, 3, 350.00),
    ('Mechanical Keyboard', 'KEY-RGB', 'Peripherals', 100, 15, 80.00);