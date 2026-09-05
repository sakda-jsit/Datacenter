# คู่มือการใช้งาน JSP Datacenter (static site)

เว็บคู่มือแบบไฟล์นิ่ง เปิดได้ตรงๆ ด้วยเบราว์เซอร์ (ไม่ต้องมี server) — เปิด `site/index.html`

```
site/
  index.html            ภาพรวมระบบ (หน้าแรก)
  pages/*.html          คู่มือรายหน้า 1 ไฟล์ = 1 เมนู
  assets/style.css      สไตล์ทั้งเว็บ
  assets/nav.js         เมนูซ้าย — จุดเดียวที่แก้รายการเมนู
  assets/img/*.png      ภาพหน้าจอจากระบบจริง
tools/
  capture.mjs           แคปภาพหน้าจอจากระบบจริงด้วย Playwright
  capture-nav.mjs       แคปเฉพาะ sidebar (กางกลุ่มเมนู)
  verify-site.mjs       ตรวจทุกหน้า: ภาพเสีย ลิงก์ตาย เมนู active JS error
  list-companies.mjs    ดูรายชื่อบริษัท/id ไว้ตั้ง MANUAL_COMPANY_ID
```

## เพิ่มคู่มือหน้าใหม่

1. คัดลอกโครงจากไฟล์ที่มีอยู่ เช่น `site/pages/trial-balance.html`
   แล้วแก้ `<title>`, `data-current` ท้ายไฟล์ และเนื้อหา
2. เปิด `site/assets/nav.js` ใส่ `href` ให้รายการเมนูนั้น
   (รายการที่ไม่มี `href` จะขึ้นว่า “เร็วๆ นี้”)
3. เพิ่ม route ที่จะแคปภาพใน `tools/capture.mjs`
4. รันแคปภาพ แล้วตรวจ (ดูด้านล่าง)

> เนื้อหาต้องตรงกับหน้าจอจริง — อ่านโค้ดของหน้านั้นใน `src/frontend/src/features/**` ก่อนเขียนเสมอ อย่าเดา

## แคปภาพหน้าจอ

ต้องมี **backend + frontend รันอยู่** และ **SQL Server ทำงาน** ก่อน

```powershell
# 1) ฐานข้อมูล (ต้องสิทธิ์ admin)
Start-Service MSSQLSERVER

# 2) backend
cd src\backend\Datacenter.Api;  dotnet run --urls http://localhost:5229

# 3) frontend (พอร์ตที่เครื่องมือแคปใช้เป็นค่าตั้งต้น)
cd src\frontend;  npx vite --port 5199 --strictPort

# 4) ปลดธง "ต้องเปลี่ยนรหัสผ่าน" ของ admin ชั่วคราว (ไม่งั้น login แล้วเด้งไป /change-password ทุกครั้ง)
sqlcmd -S localhost -U sa -P "UEBzc3cwcmQ=" -d DatacenterDb -Q "UPDATE Users SET MustChangePassword=0 WHERE Username='admin'"

# 5) แคป (ครั้งแรกต้อง npm install + npx playwright install chromium)
cd manual
node tools/capture.mjs              # ทุกหน้า
node tools/capture.mjs trial-balance general-ledger   # เฉพาะที่ระบุ

# 6) ตั้งธงกลับให้เหมือนเดิม
sqlcmd -S localhost -U sa -P "UEBzc3cwcmQ=" -d DatacenterDb -Q "UPDATE Users SET MustChangePassword=1 WHERE Username='admin'"
```

> ธง `MustChangePassword` ถูก `DbInitializer` ตั้งกลับเป็น 1 เองทุกครั้งที่ backend start
> ตราบใดที่รหัส admin ยังเป็น `admin1234` — ขั้นตอนข้อ 6 จึงเป็นแค่การเก็บให้เรียบร้อยทันที

ตัวแปรที่ปรับได้: `MANUAL_BASE`, `MANUAL_USER`, `MANUAL_PASS`, `MANUAL_COMPANY_ID`, `MANUAL_YEAR`

> หน้ารายงาน (งบทดลอง / บัญชีแยกประเภท) ไม่ดึงข้อมูลเอง — `capture.mjs` จะตั้งปีแล้วกด
> “แสดงรายงาน” ให้ก่อนแคป ถ้าไม่ทำจะได้ภาพหน้าจอเปล่า

## ตรวจงาน

```bash
cd manual
node tools/verify-site.mjs
```

ผ่านเมื่อขึ้น `ALL OK` — เช็กให้ทุกหน้า: ไม่มีภาพเสีย ไม่มีลิงก์ภายในตาย เมนูซ้ายเรนเดอร์ครบและมี active
พอดี 1 รายการ และไม่มี JS error (ภาพผลลัพธ์เก็บที่ `_preview/`)
