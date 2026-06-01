# Datacenter — Accounting Office Platform

ระบบสำนักงานบัญชีสำหรับจัดการลูกค้าหลายบริษัท บน React + ASP.NET Core + SQL Server

---

## Prerequisites

| เครื่องมือ | เวอร์ชันขั้นต่ำ |
|---|---|
| .NET SDK | 8.0 |
| Node.js | 20 LTS |
| SQL Server | 2019 / LocalDB / Express |
| npm | 10+ |

---

## Backend Setup

### 1. แก้ไข Connection String

เปิดไฟล์ [`src/backend/Datacenter.Api/appsettings.json`](src/backend/Datacenter.Api/appsettings.json) แล้วแก้ไข:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=DatacenterDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### 2. แก้ไข JWT Secret

```json
"Jwt": {
  "Key": "YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS"
}
```

> **สำคัญ:** อย่า commit JWT Key จริงเข้า git

### 3. สร้างฐานข้อมูลและรัน Migration

```bash
cd src/backend

dotnet ef migrations add InitialCreate --project Datacenter.Infrastructure --startup-project Datacenter.Api

dotnet ef database update --project Datacenter.Infrastructure --startup-project Datacenter.Api
```

### 4. รัน Backend

```bash
cd src/backend/Datacenter.Api
dotnet run
```

API จะรันที่ `https://localhost:7001`
Swagger UI: `https://localhost:7001/swagger`

---

## Frontend Setup

### 1. ติดตั้ง Dependencies

```bash
cd src/frontend
npm install
```

### 2. รัน Frontend

```bash
npm run dev
```

Frontend จะรันที่ `http://localhost:5173`

> Frontend proxy `/api` → `https://localhost:7001` ผ่าน `vite.config.ts`

---

## โครงสร้างโปรเจกต์

```
Datacenter/
├── docs/                          # เอกสาร requirements และ architecture
├── reference/                     # Legacy business logic สำหรับอ้างอิง
├── src/
│   ├── backend/
│   │   ├── Datacenter.sln
│   │   ├── Datacenter.Domain/     # Entities, Enums, Exceptions
│   │   ├── Datacenter.Application/# Use Cases, DTOs, Interfaces
│   │   ├── Datacenter.Infrastructure/ # EF Core, JWT, Services
│   │   └── Datacenter.Api/        # Controllers, Middleware, Program.cs
│   └── frontend/
│       └── src/
│           ├── app/               # Global setup (router, query client)
│           ├── features/          # 1 โฟลเดอร์ต่อ 1 โมดูลธุรกิจ
│           ├── shared/            # Components, hooks, services ใช้ร่วมกัน
│           │   └── components/     # Shared Component Library ใช้ซ้ำได้ทุกหน้า
│           └── routes/            # ProtectedRoute, AppRouter
└── README.md
```

### Frontend Component-Based Architecture

Frontend ต้องพัฒนาแบบ Component-Based Architecture:

- สร้าง shared component / component library กลางไว้ที่ `src/frontend/src/shared/components`
- ทุกหน้าควร reuse component กลางก่อนสร้าง component ใหม่เฉพาะหน้า
- Component ที่ควรเป็นของกลาง เช่น `DataTable`, form controls, filter bars, buttons, badges, cards, tabs, modals, pagination, loading states, empty states และ layout primitives
- Route pages ควรทำหน้าที่ orchestration เช่น load data, route state, pagination, และเรียก component มา compose
- Component เฉพาะโมดูลให้วางไว้ที่ `src/frontend/src/features/<feature>/components`
- ถ้า component เฉพาะโมดูลถูกใช้ซ้ำมากกว่า 1 feature ให้ย้ายขึ้น `shared/components`

---

## EF Core Migration Commands

```bash
# สร้าง migration ใหม่
dotnet ef migrations add <MigrationName> \
  --project src/backend/Datacenter.Infrastructure \
  --startup-project src/backend/Datacenter.Api

# อัปเดตฐานข้อมูล
dotnet ef database update \
  --project src/backend/Datacenter.Infrastructure \
  --startup-project src/backend/Datacenter.Api

# ดู migrations ที่มี
dotnet ef migrations list \
  --project src/backend/Datacenter.Infrastructure \
  --startup-project src/backend/Datacenter.Api
```

---

## Build สำหรับ Production

```bash
# Backend
cd src/backend
dotnet publish Datacenter.Api -c Release -o ./publish

# Frontend
cd src/frontend
npm run build
# ผลลัพธ์อยู่ใน dist/
```

---

## Tech Stack

| ส่วน | Technology |
|---|---|
| Frontend | React 18, TypeScript, Vite, TailwindCSS |
| Frontend Architecture | Component-Based Architecture, Shared Component Library |
| State / Data | TanStack Query, React Router v6 |
| Backend | ASP.NET Core 8, MediatR, FluentValidation |
| Database | SQL Server, EF Core 8 |
| Auth | JWT Bearer |
| API Docs | Swagger / OpenAPI |
