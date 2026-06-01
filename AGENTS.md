# AGENTS.md

## Project
Datacenter

## Current Situation
A legacy prototype already exists. It was built with Python Flask and Jinja templates.

The new target system must NOT continue the old Flask architecture.

The new system must be built as:

- React + TypeScript frontend
- ASP.NET Core Web API backend
- SQL Server database
- Clean Architecture
- Multi-client accounting office platform

## Important Rule
Use files under `/reference` as business and technical reference only.

Do not copy Flask routes, Jinja templates, Python web structure, CSS, cache files, users.json, secrets, or legacy runtime files into the new system.

## Reference Assets
The `/reference` folder contains useful legacy knowledge:

- Express DBF extraction logic
- Financial statement calculation logic
- Statement mapping SQL
- Sample chart of accounts
- Sample trial balance data
- Original requirement document

These assets must be analyzed and converted into the new architecture.

## Target Architecture
Backend projects should follow Clean Architecture:

- Datacenter.Domain
- Datacenter.Application
- Datacenter.Infrastructure
- Datacenter.Api

Frontend should be:

- React
- TypeScript
- Vite
- TailwindCSS
- Component-Based Architecture
- Shared Component / Component Library for reusable UI patterns

Database:

- SQL Server
- EF Core
- Migration-based schema management

## Phase 1 Focus
Build an Accounting Office Platform with:

1. Client Management
2. Import Data
3. VAT Management
4. AR Management
5. AP Management
6. Payroll
7. Bank Reconciliation
8. Trial Balance
9. General Ledger
10. Financial Statement
11. Tax Report
12. Compliance Calendar
13. Closing Period
14. Audit Log
15. Dashboard & KPI

## Development Rules
- Business logic must not be placed in Controllers.
- Use DTOs between API and Application layers.
- Use EF Core in Infrastructure only.
- Domain entities must not depend on EF Core attributes unless necessary.
- All critical actions must be audited.
- Every import batch must be traceable.
- All modules must support multi-company isolation.
- Frontend pages must be built from reusable components, not large page-only JSX blocks.
- Common frontend UI such as tables, forms, filters, buttons, status badges, tabs, modals, cards, pagination, and layout primitives must live under `src/frontend/src/shared/components`.
- Feature-specific components may live under `src/frontend/src/features/<feature>/components`, but they should compose shared components whenever possible.
- Do not duplicate table, form, filter, pagination, or status UI logic across pages; create or extend a shared component instead.
- Prefer clear, maintainable code over over-engineered abstractions.

## Claude Code Workflow
Before writing code:

1. Read AGENTS.md.
2. Read all files in `/docs`.
3. Inspect `/reference`.
4. Produce a migration/design plan.
5. Ask for confirmation only if architecture direction is unclear.
6. Generate skeleton first.
7. Implement one module at a time.

# Communication Rules
- ตอบกลับผู้ใช้เป็นภาษาไทยเสมอ
- คำอธิบายเชิงธุรกิจ, architecture, requirement, workflow ให้ใช้ภาษาไทย
- ชื่อไฟล์, ชื่อตัวแปร, ชื่อ class, method, API endpoint และ technical keyword สามารถใช้ภาษาอังกฤษได้
- ถ้าอธิบายโค้ด ให้เขียนคำอธิบายเป็นภาษาไทย แต่คง syntax/code เป็นภาษาอังกฤษ
- ถ้าต้องสร้างเอกสาร Markdown ให้เขียนเนื้อหาหลักเป็นภาษาไทย
- ถ้าพบ requirement ไม่ชัดเจน ให้ถามกลับเป็นภาษาไทยก่อนแก้โค้ด
