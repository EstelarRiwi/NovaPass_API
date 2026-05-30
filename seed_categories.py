import psycopg2
import uuid
from datetime import datetime, timezone

conn = psycopg2.connect(
    host="5.189.174.154",
    port=5432,
    dbname="postgres",
    user="postgres",
    password="sjo9&ja%9JWQgtF#8hxL4XgruoDS#",
    connect_timeout=15
)
cur = conn.cursor()
schema = "Novapass"
now = datetime.now(timezone.utc).replace(tzinfo=None)

# Event IDs
FESTIVAL   = "e1111111-1111-1111-1111-111111111111"
CONCIERTO  = "e2222222-2222-2222-2222-222222222222"
TECH       = "e3333333-3333-3333-3333-333333333333"

new_categories = [
    # Festival Electronico — agregar Economico
    (str(uuid.uuid4()), FESTIVAL,  "Economico",       80000.00,  1000, 1000, now, now),
    # Concierto Rock Colombia — agregar General y Pista
    (str(uuid.uuid4()), CONCIERTO, "General",         120000.00,  600,  600, now, now),
    (str(uuid.uuid4()), CONCIERTO, "Pista",            80000.00,  800,  800, now, now),
    # Tech Summit 2026 — agregar Acceso General y Startup Bundle
    (str(uuid.uuid4()), TECH,      "Acceso General",  450000.00,  300,  300, now, now),
    (str(uuid.uuid4()), TECH,      "Startup Bundle",  1200000.00,  30,   30, now, now),
]

insert_sql = f"""
    INSERT INTO "{schema}".ticket_categories
        (id, event_id, name, price, total_capacity, available_capacity, created_at, updated_at)
    VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
"""

for cat in new_categories:
    cur.execute(insert_sql, cat)
    print(f"  Inserted: {cat[2]} for event {cat[1][:8]}...")

conn.commit()
print(f"\nDone — {len(new_categories)} categories added.")

# Summary
cur.execute(f"""
    SELECT e.name, COUNT(tc.id) as cat_count,
           STRING_AGG(tc.name || ' ($' || tc.price::int || ', cap ' || tc.total_capacity || ')', ' | ' ORDER BY tc.price DESC) as cats
    FROM "{schema}".events e
    LEFT JOIN "{schema}".ticket_categories tc ON tc.event_id = e.id
    GROUP BY e.id, e.name
    ORDER BY e.name
""")
print("\n=== FINAL STATE ===")
for row in cur.fetchall():
    print(f"\n{row[0]} ({row[1]} categorías):")
    print(f"  {row[2]}")

cur.close()
conn.close()
