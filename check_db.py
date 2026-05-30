import psycopg2

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

# Check seats columns
cur.execute(f"""
    SELECT column_name, data_type
    FROM information_schema.columns
    WHERE table_schema = '{schema}' AND table_name = 'seats'
    ORDER BY ordinal_position
""")
print("=== SEATS COLUMNS ===")
for row in cur.fetchall():
    print(f"  {row[0]} ({row[1]})")

print("\n=== SEATS DATA ===")
cur.execute(f'SELECT * FROM "{schema}".seats LIMIT 15')
seats = cur.fetchall()
for s in seats:
    print(f"  {s}")

# Categories with categories count per event
print("\n=== CATEGORIES PER EVENT ===")
cur.execute(f"""
    SELECT e.name, COUNT(tc.id) as cat_count,
           SUM(tc.total_capacity) as total_cap,
           SUM(tc.available_capacity) as avail_cap
    FROM "{schema}".events e
    LEFT JOIN "{schema}".ticket_categories tc ON tc.event_id = e.id
    GROUP BY e.id, e.name
    ORDER BY e.name
""")
for row in cur.fetchall():
    print(f"  {row[0]}: {row[1]} categories, {row[2]} total capacity, {row[3]} available")

cur.close()
conn.close()
