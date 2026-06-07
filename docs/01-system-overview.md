# System Overview

## System Name
Automated Accounting Office Platform

## Objective
Build a centralized platform for Thai accounting firms to manage accounting data, tax reports, payroll, reconciliation, financial statements, and monthly compliance for multiple client companies.

## Core Domain Principles (หลักการพื้นฐาน)
ระบบนี้สร้างขึ้นสำหรับ **สำนักงานบัญชี** ที่รับทำบัญชีให้ลูกค้าหลายราย โดยลูกค้ามีความหลากหลายทางประเภทธุรกิจ หลักการตั้งต้น:

- **รองรับลูกค้าได้ไม่จำกัดจำนวน** (scale ตามจำนวนบริษัทลูกค้า)
- **1 ลูกค้า = 1 บริษัท** — แต่ละลูกค้าคือบริษัทหนึ่งราย แยกข้อมูลกันด้วย multi-company isolation
- **ลูกค้าหลากหลายประเภทธุรกิจ** — ระบบต้องไม่ผูกสมมติฐานเฉพาะอุตสาหกรรมใด (ผังบัญชี/หมวด/อัตรา/รายงาน ยืดหยุ่นต่อบริษัท) — ดูนิยาม "ประเภทธุรกิจ" ใน [docs/06-glossary.md](06-glossary.md)
- **ทุกข้อมูลทางบัญชีมาจาก Express (single source of truth)** — ข้อมูลทุกอย่างที่ Express มีต้องดึงจาก Express เท่านั้น ห้ามป้อนมือซ้ำ; ทำ import ที่เดียวดึงทุกอย่างครั้งเดียว (โมดูลย่อยห้ามมีปุ่ม import แยก) — การป้อนมือเป็นได้เฉพาะข้อมูลที่ Express ไม่มี DBF รองรับ (payroll, prepaid, รายการปรับปรุงปิดงบ, เอกสารแนบ) ซึ่งเป็นข้อยกเว้น
- **ข้อมูลที่ปิดงวดแล้วต้องไม่เปลี่ยน** — import/delete ผูกกับรอบบัญชีที่ยังเปิดใน Express + เก็บ snapshot ต้นฉบับถาวร ≥ 10 ปี

## Current Legacy System
The previous prototype was a Python Flask application focused on:

- Express Accounting data reading
- Financial statement generation
- Tax adjustment
- Lease adjustment
- Reconciliation page
- Dashboard

The new project must use the legacy system as reference only.

## Target System
The target system is a production-ready platform based on:

- React frontend
- ASP.NET Core backend
- SQL Server database
- Clean Architecture

## Frontend Direction
The React frontend must follow Component-Based Architecture.

- Build pages by composing reusable shared components.
- Maintain a shared component library under `src/frontend/src/shared/components`.
- Reuse shared UI patterns such as data tables, forms, filter bars, buttons, badges, cards, tabs, modals, pagination, loading states, empty states, and layout primitives across modules.
- Keep feature pages focused on workflow, data loading, and orchestration.
- Place feature-only UI in `src/frontend/src/features/<feature>/components`, and promote repeated patterns to `shared/components`.
- Avoid duplicating table, form, filter, pagination, and status-display logic in each page.

## Main Goals
- Reduce manual accounting work
- Import data from Express Accounting
- Store accounting data in SQL Server
- Support monthly tax and compliance tasks
- Generate financial statements
- Track payroll and withholding tax
- Provide accounting office dashboard
- Support multi-company and multi-client operations
