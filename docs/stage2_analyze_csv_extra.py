import csv
from collections import Counter, defaultdict
from pathlib import Path

path = Path(__file__).resolve().parents[1] / "data" / "rolling-mill" / "AssetIQ_RollingMill_500Rows(AssetIQ_RollingMill).csv"

rows = []
with path.open(newline="", encoding="utf-8-sig") as f:
    reader = csv.DictReader(f)
    for r in reader:
        rows.append(r)


def f(x):
    return float(x)


# Energy intensity check
mismatch = 0
for r in rows:
    calc = f(r["Energy_kWh"]) / f(r["Production_Tons"])
    actual = f(r["Energy_Intensity"])
    if abs(calc - actual) > 0.01:
        mismatch += 1
print("Energy_Intensity mismatches vs Energy_kWh/Production_Tons:", mismatch)

# Risk tier vs cost/downtime
for r in rows[:3]:
    pass
combo = Counter((r["Risk_Level"], r["Maintenance_Flag"], r["Estimated_Downtime_Hours"], r["Estimated_Cost_Impact_INR"]) for r in rows)
print("Risk/Maintenance/Downtime/Cost combinations:", len(combo))
for k, v in combo.most_common():
    print(" ", k, v)

# Health score zero share
zero_health = sum(1 for r in rows if f(r["Health_Score"]) == 0)
print("Health_Score == 0:", zero_health, f"({100*zero_health/len(rows):.1f}%)")

# Medium/High health with Critical risk?
crit_nonzero_health = sum(1 for r in rows if r["Risk_Level"] == "Critical" and f(r["Health_Score"]) > 0)
print("Critical risk but Health_Score > 0:", crit_nonzero_health)

# Measurement extremes for Medium vs Critical (small samples)
groups = defaultdict(list)
for r in rows:
    groups[r["Risk_Level"]].append(r)

for level in ["Medium", "High", "Critical"]:
    g = groups[level]
    if not g:
        continue
    vib = [f(x["Vibration_mm_s"]) for x in g]
    mt = [f(x["Motor_Temp_C"]) for x in g]
    print(f"\n{level} n={len(g)} vibration mean={sum(vib)/len(vib):.2f} motor_temp mean={sum(mt)/len(mt):.2f}")

# Timestamp parsing - hourly increments?
from datetime import datetime

fmt = "%Y-%m-%d %H:%M"
dts = sorted(datetime.strptime(r["Timestamp"], fmt) for r in rows)
gaps = [(dts[i + 1] - dts[i]).total_seconds() / 3600 for i in range(len(dts) - 1)]
print("\nTimestamp gap hours: min", min(gaps), "max", max(gaps), "unique gaps", len(set(gaps)))

# Equipment types with roll metrics - all have values (not null) - process relevance
for et in sorted(set(r["Equipment_Type"] for r in rows)):
    subset = [r for r in rows if r["Equipment_Type"] == et]
    rs = [f(r["Roll_Speed_mpm"]) for r in subset]
    print(f"{et}: Roll_Speed mean={sum(rs)/len(rs):.1f}")
