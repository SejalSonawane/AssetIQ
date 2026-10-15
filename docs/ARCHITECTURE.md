# AssetIQ architecture notes (foundation)

## Layering

| Layer | Responsibility |
|-------|----------------|
| `AssetIQ.Web` | HTTP, Razor views, view models, UI assets |
| `AssetIQ.Business` | Business rules, dashboard summaries, future analytics |
| `AssetIQ.Data` | Entities, repositories, future EF Core configurations |

Controllers remain thin and call business services. Views do not query databases or embed business rules.

## Generic equipment model

Equipment uses `EquipmentTypeKey` and measurement keys rather than hardcoding rolling-mill-only tables. Rolling Mill is the initial PoC dataset, not the platform boundary.

## Future integration boundaries

- **PostgreSQL / EF Core**: repository implementations replace in-memory stubs.
- **Python ML**: predictions consumed via a dedicated service boundary; separate from CSV labels and rule-based scores.
- **Power Automate**: notification triggers after alerts are persisted and configured externally.
