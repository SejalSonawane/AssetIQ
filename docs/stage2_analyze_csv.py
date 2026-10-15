import csv
from collections import Counter
from pathlib import Path

path = Path(__file__).resolve().parents[1] / "data" / "rolling-mill" / "AssetIQ_RollingMill_500Rows(AssetIQ_RollingMill).csv"
expected = Path(__file__).resolve().parents[1] / "data" / "rolling-mill" / "AssetIQ_RollingMill_500Rows.csv"

rows = []
with path.open(newline="", encoding="utf-8-sig") as f:
    reader = csv.DictReader(f)
    cols = reader.fieldnames or []
    for r in reader:
        rows.append(r)

n = len(rows)


def to_float(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return None


print("EXPECTED_PATH_EXISTS", expected.exists(), expected)
print("ACTUAL_PATH_EXISTS", path.exists(), path)
print("ROWS", n)
print("COL_COUNT", len(cols))
print("COLUMNS")
for i, c in enumerate(cols, 1):
    print(f"  {i:2}. {c}")

row_tuples = [tuple(r[c] for c in cols) for r in rows]
print("DUPLICATE_FULL_ROWS", n - len(set(row_tuples)))
keys = [(r["Equipment_ID"], r["Timestamp"]) for r in rows]
print("DUPLICATE_EQUIP_ID_TIMESTAMP", n - len(set(keys)))

missing = {c: sum(1 for r in rows if not str(r.get(c, "")).strip()) for c in cols}
print("MISSING_VALUES", missing)

numeric_cols = [
    c
    for c in cols
    if c
    not in (
        "Timestamp",
        "Equipment_ID",
        "Equipment_Type",
        "Section",
        "Risk_Level",
        "Maintenance_Flag",
    )
]

print("\nNUMERIC_SUMMARY")
for c in cols:
    vals = [to_float(r[c]) for r in rows]
    nums = [v for v in vals if v is not None]
    if len(nums) < n * 0.5:
        continue
    print(
        f"{c}: min={min(nums):.6g} max={max(nums):.6g} mean={sum(nums)/len(nums):.6g} "
        f"non_numeric={n-len(nums)}"
    )

for label, field in [
    ("Equipment_Type", "Equipment_Type"),
    ("Section", "Section"),
    ("Risk_Level", "Risk_Level"),
    ("Maintenance_Flag", "Maintenance_Flag"),
]:
    ctr = Counter(r[field] for r in rows)
    print(f"\n{label} (unique={len(ctr)})")
    for k, v in ctr.most_common():
        print(f"  {k!r}: {v}")

print("\nHealth_Score unique values", sorted(set(r["Health_Score"] for r in rows)))
print("Failure_Count_Last_Year unique", sorted(set(r["Failure_Count_Last_Year"] for r in rows)))

print("\nHealth_Score x Risk_Level")
for pair, cnt in Counter((r["Health_Score"], r["Risk_Level"]) for r in rows).most_common():
    print(f"  {pair}: {cnt}")

print("\nUnique Equipment_ID", len(set(r["Equipment_ID"] for r in rows)))
ts = sorted(set(r["Timestamp"] for r in rows))
print("Unique Timestamp", len(ts), "range", ts[0], "->", ts[-1])

print("\nEstimated_Cost_Impact_INR", Counter(r["Estimated_Cost_Impact_INR"] for r in rows))
print("Estimated_Downtime_Hours", Counter(r["Estimated_Downtime_Hours"] for r in rows))

prod_zero = sum(1 for r in rows if to_float(r["Production_Tons"]) == 0)
print("Production_Tons == 0 rows", prod_zero)

# Per-equipment row counts
eq_ctr = Counter(r["Equipment_ID"] for r in rows)
print("Rows per equipment: min", min(eq_ctr.values()), "max", max(eq_ctr.values()), "median-ish unique counts", len(eq_ctr))
