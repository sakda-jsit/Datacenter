# Technical Architecture

## Legacy Architecture
The legacy prototype is Python Flask + Jinja Template.

It must not be used as the main architecture for the new project.

## New Architecture

### Frontend
- React
- TypeScript
- Vite
- TailwindCSS
- Component-Based Architecture
- Shared Component / Component Library

### Backend
- ASP.NET Core Web API
- Entity Framework Core
- FluentValidation
- AutoMapper or manual mapping
- Swagger/OpenAPI

### Database
- SQL Server
- EF Core Migrations

### Authentication
- JWT Authentication
- Role-based access control
- Company-level authorization

## Clean Architecture Projects

```text
src/backend
├─ Datacenter.Domain
├─ Datacenter.Application
├─ Datacenter.Infrastructure
└─ Datacenter.Api
```

## Frontend Structure

```text
src/frontend
├─ src/app
├─ src/features
├─ src/shared
├─ src/routes
└─ src/services
```

### Frontend Component Architecture
The frontend must be organized as a reusable component system.

```text
src/frontend/src
├─ app                     # global providers and app bootstrap
├─ routes                  # route definitions and guards
├─ features
│  └─ <feature>
│     ├─ pages             # route-level screens, orchestration only
│     ├─ components        # feature-specific composed UI
│     ├─ hooks             # feature data and state hooks
│     ├─ services          # feature API clients
│     └─ types             # feature DTO/types
└─ shared
   ├─ components
   │  ├─ table             # shared data table and table utilities
   │  ├─ form              # inputs, field wrappers, validation display
   │  ├─ feedback          # badges, alerts, empty states, loading states
   │  ├─ layout            # app shell, sidebar, topbar
   │  └─ navigation        # tabs, breadcrumbs, menu primitives
   ├─ hooks
   ├─ services
   └─ types
```

Rules:
- Route pages should stay thin and compose components.
- Shared UI patterns must be implemented once under `shared/components` and reused by all modules.
- Feature components should depend on shared components, not duplicate them.
- Reusable table, form, filter, pagination, modal, tab, badge, card, and layout behavior belongs in the shared component library.
- Feature-specific business labels and column definitions can stay inside feature components.
- Shared components must be typed with TypeScript generics where appropriate, for example reusable data tables.

## Dependency Rule
- Domain depends on nothing.
- Application depends on Domain.
- Infrastructure depends on Application and Domain.
- API depends on Application and Infrastructure.
- Frontend communicates with API only.
- Frontend feature modules may depend on `shared`, but `shared` must not depend on feature modules.

## EF Core in Application — ข้อตกลงที่ยอมรับร่วมกัน (Accepted Tradeoff)
ตามหลักการ "Use EF Core in Infrastructure only" เราตีความว่าหมายถึง **การตั้งค่าและ implementation
ที่ผูกกับฐานข้อมูล** (`DbContext` จริง, `IEntityTypeConfiguration`, migrations, connection string) ต้องอยู่ใน
Infrastructure เท่านั้น

แต่ Application layer ยังอ้างอิง `Microsoft.EntityFrameworkCore` ในระดับ abstraction ผ่าน:
- `IApplicationDbContext` ที่ expose `DbSet<T>` เป็น read/write API ให้ handler
- การใช้ async LINQ extension (`ToListAsync`, `AnyAsync`, ฯลฯ) ภายใน handler

เหตุผล: เป็นรูปแบบ Clean Architecture + CQRS ที่ใช้กันแพร่หลาย ลดชั้น repository ที่ไม่จำเป็นออก
สอดคล้องกับกฎ *"Prefer clear, maintainable code over over-engineered abstractions"* ใน AGENTS.md
โดย `DbContext` ตัวจริง (`AppDbContext`) ยังคงอยู่ใน Infrastructure และ Application ไม่เคยอ้างถึง
SQL Server provider หรือ configuration โดยตรง

หากในอนาคตต้องสลับ persistence engine หรือต้องการ unit test ที่ไม่พึ่ง EF การใส่ repository abstraction
เฉพาะจุดที่จำเป็นถือเป็นทางเลือกที่ยอมรับได้ แต่ไม่ใช่ข้อบังคับสำหรับทุก entity

## Multi-Company Isolation & Company-Level Authorization
บังคับใช้แบบรวมศูนย์ ไม่กระจายตรรกะไปยังแต่ละ handler:
- `IRequireCompanyAccess` — marker interface บน query/command ที่ทำงานกับบริษัทรายเดียว
- `CompanyAccessBehaviour` — MediatR pipeline behaviour ที่ตรวจสิทธิ์อัตโนมัติก่อนเข้า handler
- `ICompanyAccessGuard` — บริการกลางสำหรับตรวจสิทธิ์ (`EnsureAccessAsync`) และดึงรายการบริษัท
  ที่เข้าถึงได้ (`GetAccessibleCompanyIdsAsync`) ใช้ทั้งใน behaviour, endpoint แบบ list และ handler
  ที่อ้างบริษัทผ่าน id ทางอ้อม (เช่น ImportBatchId, TaskId)

Admin เข้าถึงได้ทุกบริษัท ผู้ใช้อื่นต้องมีสิทธิ์ใน `CompanyUserAccess` มิฉะนั้นจะได้รับ
`ForbiddenException` (HTTP 403)
