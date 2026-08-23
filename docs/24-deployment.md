# 24 การนำขึ้นใช้งานจริง (Deployment) — v1

สถานะ: จัดทำ 2026-08-23 พร้อมกับงาน "hardening ก่อนขึ้น production v1"
สคริปต์และตัวอย่างค่าตั้งอยู่ที่โฟลเดอร์ `deploy/` (ขั้นตอนลงมือดู `deploy/README.md`)

---

## 1. สิ่งที่แก้ไปแล้วในรอบนี้ (ก่อนหน้านี้เป็นตัวขัดขวางการขึ้น production)

| ประเด็น | เดิม | ปัจจุบัน |
|---|---|---|
| กุญแจ JWT | ค่าตัวอย่าง `CHANGE_THIS_SECRET_KEY...` commit อยู่ใน git → ใครอ่าน repo ได้ก็ปลอม token เป็น Admin | `appsettings.json` ไม่มีค่าลับแล้ว; อ่านจาก env var / `appsettings.Local.json`; `StartupConfigValidator` ไม่ให้ระบบสตาร์ตถ้าค่าว่าง/สั้น/เป็นค่าตัวอย่าง |
| รหัส SQL Server | connection string ของ `sa` commit อยู่ใน git | ย้ายออกจากไฟล์ที่ commit; production ใช้ SQL login เฉพาะงาน; ระบบเตือนถ้า connection string ยังใช้ `sa` |
| ผู้ใช้ระบบ | มีแต่ `admin/admin1234` ไม่มีหน้าจอ/API สร้างผู้ใช้ → ต้องใช้บัญชีร่วมกัน | เมนู **ระบบ → ผู้ใช้งานระบบ** (Admin เท่านั้น): สร้าง/แก้/ปิดใช้งาน/รีเซ็ตรหัส/ปลดล็อก + ผูกสิทธิ์รายบริษัท; ผู้ใช้เปลี่ยนรหัสตัวเองได้ |
| รหัสตั้งต้น | `admin1234` ใช้ต่อได้เรื่อย ๆ | บังคับเปลี่ยนรหัสตอน login ครั้งแรก (รวมฐานข้อมูลเดิมที่ยังใช้รหัสนี้ — ระบบตรวจตอนสตาร์ต) |
| เดารหัสผ่าน (brute force) | ไม่มีการจำกัด | ผิดครบ 5 ครั้ง → ล็อก 15 นาที (ปรับได้ที่ `Auth`), เขียน audit ทั้ง `Login`/`LoginFailed` |
| หมดอายุ session | token 8 ชม. ไม่มี refresh (`/auth/refresh` = 501) | access token 60 นาที + refresh token 14 วัน แบบ rotation (เก็บเฉพาะ hash), frontend ต่ออายุเองเงียบ ๆ |
| CORS | ฮาร์ดโค้ด `localhost:5173` | อ่านจาก `Cors:AllowedOrigins`; ถ้า deploy แบบ same-origin (frontend อยู่ `wwwroot`) ไม่เปิด CORS เลย |
| log | console เท่านั้น (หายเมื่อ restart) | log ไฟล์รายวัน `logs/datacenter-yyyyMMdd.log` เก็บ 90 วัน (`Logging:File`) |
| ชุด deploy | ไม่มี | `deploy/publish.ps1` (frontend+backend ชุดเดียว), `deploy/backup-db.ps1`, ตัวอย่าง `appsettings.Production.json` |
| post batch นำเข้าเก่า | กรองด้วย `FiscalYear` → batch เก่าที่ tag ทุก slot เป็นปีเดียวกันจะได้ 3 ระเบียน/บัญชี = **ยอดเบิ้ล 3 เท่า** | `ExpressPostingService` ตรวจเจอ batch แบบนั้นแล้วถอยไปใช้ slot `CUR` ตามพฤติกรรมเดิม + แจ้งเตือนให้นำเข้าใหม่ |

## 2. งานที่ยังเป็นหน้าที่ฝ่าย IT (ระบบทำแทนไม่ได้)

1. **เปลี่ยนรหัส `sa` ของ SQL Server** — รหัสเดิมเคยอยู่ในไฟล์ที่ commit ลง git จึงถือว่ารั่วแล้ว
   สร้าง login `dc_app` สำหรับระบบนี้ และเก็บ `sa` ไว้ใช้เฉพาะงานดูแลฐานข้อมูล
2. **ประวัติ git ยังมีค่าลับเดิมอยู่** (commit เก่า) — ถ้า repo นี้จะแชร์ออกนอกทีม ให้ล้างประวัติ
   (`git filter-repo`) หรือย้ายไป repo ใหม่. ถ้าอยู่ในเครือข่ายภายในเท่านั้น การเปลี่ยนรหัสตามข้อ 1 เพียงพอ
3. **ใบรับรอง HTTPS** + ผูกกับ IIS/reverse proxy
4. **สิทธิ์เข้าถึงข้อมูล Express** — เครื่อง server ต้องเห็น path ตาม `Import:ExpressBasePath`
   (network share ต้องใช้ UNC + บัญชี service ที่มีสิทธิ์อ่าน — mapped drive `J:` ของผู้ใช้ไม่ทำงานใน service)
5. **ตั้งงานสำรองข้อมูลรายวัน** + ทดลองกู้คืน 1 ครั้งก่อนเริ่มใช้จริง
6. **แจ้งผู้ใช้เรื่อง PDPA** (ดูข้อ 4)

## 3. Checklist ก่อนเปิดให้ผู้ใช้ทดลองใช้

- [ ] `appsettings.Production.json` ตั้ง `Jwt:Key` (สุ่ม ≥32 ตัว) และ connection string ที่ไม่ใช่ `sa`
- [ ] `ASPNETCORE_ENVIRONMENT=Production` (ไม่งั้น Swagger เปิดและ HTTPS redirect ไม่ทำงานตามที่ตั้งใจ)
- [ ] เข้า `https://...` ได้ และ `http://` ถูก redirect (หรือปิดที่ proxy)
- [ ] login `admin` แล้วระบบพาไปหน้าเปลี่ยนรหัสทันที → เปลี่ยนรหัสสำเร็จ
- [ ] สร้างผู้ใช้ทดลอง 1 คน (Maker) ผูก 1 บริษัท → login ได้ เห็นเฉพาะบริษัทนั้น
- [ ] ทดลองใส่รหัสผิด 5 ครั้ง → ถูกล็อก และ Admin ปลดล็อกได้
- [ ] นำเข้าข้อมูล Express 1 บริษัทจากเครื่อง server ได้ (path เห็นจริง)
- [ ] `deploy\backup-db.ps1` รันผ่าน + ไฟล์ `.bak` ตรวจ `RESTORE VERIFYONLY` ผ่าน
- [ ] มีไฟล์ log เกิดขึ้นที่ `logs\` และไม่มี error ค้าง
- [ ] **ห้ามใช้บัญชี `admin` ร่วมกันหลายคน** — audit log ต้องระบุตัวบุคคลได้

## 4. PDPA — ข้อควรระวังของระบบนี้

ฐานข้อมูลเก็บข้อมูลส่วนบุคคลจริง: ทะเบียนพนักงานของลูกค้า, **รูปบัตรประชาชน** (เก็บเป็น blob ในตาราง
`Attachments`/`EmployeeDocuments`), เงินเดือนรายคน, statement ธนาคาร ดังนั้น

- ไฟล์ backup = ข้อมูลส่วนบุคคล → เก็บในที่ที่คุมสิทธิ์ ห้ามวางบน cloud drive ที่แชร์ทั้งองค์กร
- ให้สิทธิ์ผู้ใช้เท่าที่จำเป็น (ผูกเฉพาะบริษัทที่รับผิดชอบ; ใช้ Admin เท่าที่จำเป็น)
- ประวัติการเข้าถึง/แก้ไขดูได้ที่ **ระบบ → ประวัติการใช้งาน** (audit log) — เก็บ hash รหัสผ่านไว้นอก audit เสมอ
- `reference/payroll/`, `reference/bank/` อยู่ใน `.gitignore` แล้ว — ห้ามนำข้อมูลลูกค้าจริงเข้า repo

## 5. ค่าตั้งความปลอดภัยที่ปรับได้ (section `Auth`)

| ค่า | ค่าเริ่มต้น | ความหมาย |
|---|---|---|
| `AccessTokenMinutes` | 60 | อายุ access token — สั้นลง = ปลอดภัยขึ้น (frontend ต่ออายุอัตโนมัติ) |
| `RefreshTokenDays` | 14 | ไม่ต้อง login ใหม่ภายในกี่วัน |
| `MaxFailedAttempts` | 5 | ใส่รหัสผิดกี่ครั้งจึงล็อก |
| `LockoutMinutes` | 15 | ล็อกนานกี่นาที |

เกณฑ์รหัสผ่าน (ฮาร์ดโค้ดที่ `PasswordPolicy`): ยาว ≥ 8, มีทั้งตัวอักษรและตัวเลข,
ไม่ซ้ำชื่อผู้ใช้, ไม่อยู่ในรายการรหัสที่ใช้กันทั่วไป/รหัสตั้งต้นของระบบ

## 6. บทบาทผู้ใช้

| บทบาท | ขอบเขต |
|---|---|
| Admin (1) | ทุกบริษัท + จัดการผู้ใช้ + ตั้งค่ากลาง |
| Maker (2) | เฉพาะบริษัทที่ผูกสิทธิ์ไว้ (บันทึก/นำเข้า) |
| Checker (3) | เฉพาะบริษัทที่ผูกสิทธิ์ไว้ (ตรวจสอบ) |

การกรองสิทธิ์บังคับที่ฝั่ง server (`CompanyAccessGuard` + `CompanyAccessBehaviour`) ทุก query/command
ที่อ้างบริษัท — การซ่อนเมนูที่ frontend เป็นเพียงชั้นความสะดวก ไม่ใช่ด่านความปลอดภัย

---

## 7. แผนที่เลือกสำหรับ v1 (ยืนยัน 2026-08-23)

| หัวข้อ | ที่เลือก | ผลต่อการติดตั้ง |
|---|---|---|
| เครื่อง server | ยังไม่กำหนด — **เตรียมชุด deploy ไว้ก่อน** | ชุดพร้อมใช้อยู่ที่ `D:\Datacenterelease1` (สร้างใหม่ได้ด้วย `deploy\publish.ps1`) คัดลอกทั้งโฟลเดอร์ไปเครื่องไหนก็ติดตั้งได้ |
| ฐานข้อมูล | **`DatacenterDb` เดิม** (มีข้อมูลจริงแล้ว) | สตาร์ตครั้งแรกจะรัน migration `AddUserSecurityAndRefreshTokens` → `install-service.ps1` สำรองฐานให้ก่อนอัตโนมัติ |
| การเข้าถึง | **Windows service + HTTP ในวง LAN** (พอร์ต 5000) | ไม่ต้องมีใบรับรอง/IIS; ค่าตั้ง `Hosting:UseHttpsRedirection=false`; firewall เปิดเฉพาะโปรไฟล์ Domain/Private |
| ผู้ใช้ชุดแรก | **บัญชี admin คนเดียว** | ไม่ต้องสร้างผู้ใช้อื่นตอนติดตั้ง; `admin` ถูกบังคับเปลี่ยนรหัสตอน login ครั้งแรก แล้วค่อยเพิ่มทีมที่เมนู ระบบ → ผู้ใช้งานระบบ |

**ขั้นตอนลงมือ:** ดู `deploy/README.md` หัวข้อ "ติดตั้งครั้งแรก — 4 คำสั่ง"

### สองเรื่องที่ต้องจัดการก่อนกดติดตั้งจริง
1. **SQL Server ต้องรันอยู่** — ตอนตรวจ (2026-08-23) service `MSSQLSERVER` บนเครื่อง dev หยุดอยู่
   และ `Start-Service` ต้องสิทธิ์ administrator; `install-service.ps1 -CheckOnly` จะรายงานให้เห็นก่อนติดตั้ง
2. **path ข้อมูล Express ต้องเป็น UNC** — `J:` บนเครื่อง dev คือ `\\js-server\ExpressI`
   (ตรวจแล้วว่า `secure\sccomp.dbf` เข้าถึงได้ทาง UNC) service มองไม่เห็น mapped drive ของผู้ใช้
   → ตั้ง `Import:ExpressBasePath` เป็น `\\js-server\ExpressI\` (ในไฟล์ JSON ต้อง escape เป็น `"\\\\js-server\\ExpressI\\"`)
   และรัน service ด้วยบัญชีที่มีสิทธิ์อ่าน share (`-ServiceUser`)

### สคริปต์ที่เพิ่มในรอบนี้
- `deploy/install-service.ps1` — ติดตั้ง/ถอน Windows service ครั้งเดียวจบ (idempotent, มี `-CheckOnly`)
- `deploy/create-sql-login.ps1` — สร้าง login `dc_app` + db_owner เฉพาะฐานนี้ แทนการใช้ `sa`
- `deploy/publish.ps1` — บรรจุสคริปต์ทั้งหมดไว้ใน `deploy\` ของชุดที่ publish ด้วย (เครื่อง server ใช้โฟลเดอร์เดียวจบ)
