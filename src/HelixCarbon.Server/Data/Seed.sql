-- Demo tenant for local development (slug: demo). Password: Admin123!
INSERT OR IGNORE INTO Tenants (Id, Slug, Name, Plan, CreatedAt)
VALUES ('11111111-1111-1111-1111-111111111111', 'demo', 'Demo Workspace', 1, datetime('now'));

INSERT OR IGNORE INTO Users (Id, TenantId, Email, PasswordHash, Role, CreatedAt)
VALUES (
    '22222222-2222-2222-2222-222222222222',
    '11111111-1111-1111-1111-111111111111',
    'admin@demo.local',
    'AQAAAAIAAYagAAAAEExampleHashReplaceOnFirstLogin',
    1,
    datetime('now')
);

INSERT OR IGNORE INTO Products (Id, TenantId, Name, Description, Price, CreatedAt)
VALUES
    ('33333333-3333-3333-3333-333333333331', '11111111-1111-1111-1111-111111111111', 'Starter Kit', 'Tenant-scoped sample product', 49.99, datetime('now')),
    ('33333333-3333-3333-3333-333333333332', '11111111-1111-1111-1111-111111111111', 'Pro Subscription', 'Monthly plan add-on', 199.00, datetime('now'));
