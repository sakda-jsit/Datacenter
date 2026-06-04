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

## Migration Baseline & Parallel Run (Requirement v11)
- **Baseline = ปีบัญชี 2025/2568** — ไม่ migrate hidden sheet 2016–2024
- สูตร Excel ที่ใช้งานจริง (ในพื้นที่ Page Break Preview) ต้อง migrate เป็น business rule ทั้งหมด — ไม่ให้เหลือขั้นตอนคำนวณหลักใน Excel
- จัดทำ formula inventory แยกตาม module (TB/FS/TAX/VAT/WHT/FA/Prepaid/AR-AP/Stock) แล้วให้ฝ่ายบัญชียืนยันก่อนพัฒนา; สูตรที่ยังไม่เข้าใจ = open issue ห้ามเดา
- **Parallel run 1 งวด (ปี 2025): ผลลัพธ์ระบบใหม่ต้องตรง Excel เดิมเป๊ะทุกยอด (ผลต่าง = 0)** ก่อน go-live
- ต้องมีผู้ sign-off ผล parallel run (ผู้รับผิดชอบ/ผู้สอบบัญชี — ยังต้องยืนยันว่าใคร) ก่อนใช้งานจริง
- รายละเอียดเต็มใน docs/21
