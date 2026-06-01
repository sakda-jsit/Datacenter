# Migration Plan

## Goal
Create a new React + ASP.NET Core + SQL Server system while preserving business knowledge from the Flask prototype.

## Step 1 - Read Reference
Analyze files in:

- `/reference/express`
- `/reference/financial`
- `/reference/requirements`

## Step 2 - Convert Concepts
Convert Python reference logic into C# application services.

Examples:

- Express DBF reader → Infrastructure Import Adapter
- Financial statement builder → Application FinancialStatementService
- SQL mapping → SQL Server seed data / EF migration
- Trial balance CSV → Test fixtures

## Step 3 - Build Skeleton
Create Clean Architecture backend and React frontend.

Frontend skeleton must include Component-Based Architecture:

- `src/frontend/src/shared/components` for the shared component library.
- `src/frontend/src/features/<feature>/components` for feature-specific composed components.
- Shared primitives for tables, forms, filters, buttons, badges, cards, tabs, modals, pagination, loading states, empty states, and layout.
- Feature pages should call shared components instead of duplicating UI implementation.

## Step 4 - Implement Modules
Start with:

1. Client Management
2. Import Data
3. Trial Balance
4. Financial Statement
5. VAT Report

## Step 5 - Validate
Use sample files in `/reference/express` to create unit tests and integration tests.

## Do Not
- Do not convert Flask routes into ASP.NET controllers directly.
- Do not copy old templates into React.
- Do not copy old Jinja structure into React page components.
- Do not build large page-only React files when the UI can be decomposed into reusable components.
- Do not duplicate common table, form, filter, pagination, or status UI logic across modules.
- Do not keep Python web runtime in production.
