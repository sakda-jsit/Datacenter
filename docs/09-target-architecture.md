# Target Architecture

## High-Level Flow

```text
Express Accounting / Excel / CSV
        ↓
Import Layer
        ↓
Staging Tables
        ↓
Validation
        ↓
Accounting Data Warehouse
        ↓
Modules / Reports / Dashboard
```

## Backend Layers

### Domain
Entities, value objects, enums, core business concepts.

### Application
Use cases, DTOs, validation, interfaces, business workflows.

### Infrastructure
EF Core, SQL Server, file import, DBF reader adapter, external integrations.

### API
REST endpoints, authentication, authorization, Swagger.

## Frontend
React feature modules:

- Client Management
- Import Data
- VAT
- AR
- AP
- Payroll
- Bank Reconciliation
- Reports
- Compliance Calendar
- Dashboard

Frontend architecture principles:

- Use Component-Based Architecture.
- Build a shared component library in `src/frontend/src/shared/components`.
- Reuse shared components across feature modules instead of creating page-specific copies.
- Keep pages thin: pages coordinate route state, API hooks, and workflow; reusable UI lives in shared or feature components.
- Put cross-module components such as `DataTable`, form controls, filter bars, pagination, badges, tabs, modals, cards, and layout primitives in `shared/components`.
- Put module-specific compositions in `src/frontend/src/features/<feature>/components`.
