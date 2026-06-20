#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
impl_status.py — ตรวจสถานะการพัฒนาจาก "โค้ดจริง" (ground truth) ไม่ใช่จาก docs/12 ที่ล้าสมัยบ่อย.

ทำ 3 อย่าง:
  1) Inventory: ดึง backend controllers+endpoints, feature areas, migrations, frontend routes จากโค้ด
  2) Drift check: เทียบกับตารางสถานะใน docs/12 — flag แถวที่ "อ้างว่ายังไม่เสร็จ (❌/⛔/🟡)"
     แต่มี controller/feature/route ในโค้ดที่ชื่อตรงกัน (= น่าจะทำแล้วแต่ doc ไม่อัปเดต)
  3) เขียน docs/_generated/impl-inventory.md (ground truth ให้คน/agent อ้างอิง)

ใช้: python tools/impl_status.py          # รายงาน + เขียน inventory
     python tools/impl_status.py --check  # exit 1 ถ้าเจอ drift (สำหรับ CI/pre-commit)
รันจาก repo root.
"""
import re, sys, pathlib, datetime

ROOT = pathlib.Path(__file__).resolve().parent.parent
BE = ROOT / "src/backend"
FE = ROOT / "src/frontend/src"
DOC12 = ROOT / "docs/12-implementation-status.md"
OUT = ROOT / "docs/_generated/impl-inventory.md"

HTTP = re.compile(r'\[Http(Get|Post|Put|Delete|Patch)\b')
ROUTE = re.compile(r'\[Route\("([^"]+)"\)\]')
FE_ROUTE = re.compile(r'<Route\s+path="([^"]+)"')

def controllers():
    out = []
    for f in sorted((BE / "Datacenter.Api/Controllers").glob("*Controller.cs")):
        t = f.read_text(encoding="utf-8", errors="replace")
        r = ROUTE.search(t)
        out.append((f.stem, r.group(1) if r else "(no route)", len(HTTP.findall(t))))
    return out

def feature_areas():
    base = BE / "Datacenter.Application/Features"
    return [(d.name, len(list(d.rglob("*.cs")))) for d in sorted(base.iterdir()) if d.is_dir()]

def migrations():
    base = BE / "Datacenter.Infrastructure/Migrations"
    ms = [f.stem for f in base.glob("*.cs") if not f.stem.endswith("Designer") and "ModelSnapshot" not in f.stem]
    return sorted(ms)

def fe_routes():
    f = FE / "routes/AppRouter.tsx"
    if not f.exists(): return []
    return sorted(set(FE_ROUTE.findall(f.read_text(encoding="utf-8", errors="replace"))))

def fe_features():
    base = FE / "features"
    if not base.exists(): return []
    return [d.name for d in sorted(base.iterdir()) if d.is_dir()]

# คำที่บอกว่า "ยังไม่เสร็จ" ในตาราง docs/12
NOT_DONE = ("❌", "⛔", "🟡", "ยังไม่เริ่ม", "ยังไม่ทำ", "รอสเปก", "รอ DBF", "รอไฟล์")

def drift_check(ctrls, feats):
    """flag แถว docs/12 ที่อ้างว่ายังไม่เสร็จ แต่มี controller/feature ชื่อตรงกัน"""
    if not DOC12.exists(): return []
    # corpus ชื่อโค้ด (lower) สำหรับจับคู่
    code_names = {c[0].replace("Controller", "").lower() for c in ctrls} | {f[0].lower() for f in feats}
    # alias ไทย→ชื่อโค้ด (ส่วนที่ doc มักเขียนไทย)
    alias = {
        "payroll": ["payroll", "เงินเดือน"], "attachment": ["attachment", "แนบ", "หลักฐาน", "evidence"],
        "vat": ["vat", "ภ.พ.30", "ภพ"], "bank": ["bank", "ธนาคาร", "เงินฝาก"],
        "reportpackages": ["report package", "ชุดรายงาน"], "auditlog": ["audit", "ตรวจสอบ"],
        "corporatetax": ["ภ.ง.ด.50", "ภาษีเงินได้นิติ"],
    }
    def hit(term, low):
        # ไทยใช้ substring (ไม่มี word boundary), อังกฤษใช้ word-boundary กัน "ap" ใน "map/snapshot"
        if re.search(r'[a-z]', term):
            return re.search(r'(?<![a-z])' + re.escape(term) + r'(?![a-z])', low) is not None
        return term in low
    flags = []
    for ln, line in enumerate(DOC12.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
        s = line.strip()
        # เฉพาะ "แถวตารางสถานะ" (| col | col |) — ข้ามร้อยแก้ว/หมายเหตุ/ขอบเขต-อนาคต
        if not (s.startswith("|") and s.count("|") >= 3):
            continue
        if not any(m in line for m in NOT_DONE):
            continue
        low = line.lower()
        for area in code_names:
            if len(area) <= 3 and area not in alias:   # ข้ามชื่อสั้น (ap/ar) ที่ไม่มี alias
                continue
            terms = alias.get(area, [area])
            if any(hit(t, low) for t in terms):
                flags.append((ln, area, s[:90]))
                break
    return flags

def main():
    ctrls, feats = controllers(), feature_areas()
    migs, routes, fefeat = migrations(), fe_routes(), fe_features()
    flags = drift_check(ctrls, feats)

    md = [f"# Code Inventory (generated) — {datetime.date.today().isoformat()}",
          "",
          "> สร้างโดย `tools/impl_status.py` จากโค้ดจริง = **ground truth**. ถ้า docs/12 ขัดกับไฟล์นี้ ให้เชื่อไฟล์นี้.",
          "",
          f"## Backend controllers ({len(ctrls)})", ""]
    md += [f"- `{r}` — **{n}** endpoints ({c})" for c, r, n in ctrls]
    md += ["", f"## Backend feature areas ({len(feats)})", ""]
    md += [f"- {n} ({k} files)" for n, k in feats]
    md += ["", f"## Frontend routes ({len(routes)})", "", "  " + ", ".join(routes)]
    md += ["", f"## Frontend feature folders ({len(fefeat)})", "", "  " + ", ".join(fefeat)]
    md += ["", f"## Migrations ({len(migs)})", ""]
    md += [f"- {m}" for m in migs]
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(md) + "\n", encoding="utf-8")

    print(f"controllers={len(ctrls)} features={len(feats)} migrations={len(migs)} fe_routes={len(routes)}")
    print(f"wrote {OUT.relative_to(ROOT)}")
    if flags:
        print(f"\n[DRIFT] docs/12 {len(flags)} แถวอ้างว่า 'ยังไม่เสร็จ' แต่โค้ดมี feature ตรงกัน — ตรวจซ้ำ:")
        for ln, area, txt in flags:
            print(f"  docs/12:{ln} (~{area}) {txt}")
    else:
        print("\n[OK] ไม่พบ drift (docs/12 ไม่มีแถว not-done ที่ขัดกับโค้ด)")

    if "--check" in sys.argv and flags:
        sys.exit(1)

if __name__ == "__main__":
    main()
