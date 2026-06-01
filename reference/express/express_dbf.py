# -*- coding: utf-8 -*-
"""
express_dbf.py
ตัวดึงข้อมูลจากไฟล์ .dbf ของโปรแกรม Express (โฟลเดอร์ข้อมูลบริษัท)
ใช้กับโฟลเดอร์ เช่น D:\\ExpressI\\JSIT2016

ความสามารถ
  - read_table(folder, name)            : อ่านตาราง .dbf ใดก็ได้เป็น list[dict] (ไทย cp874)
  - list_tables(folder)                 : สรุปทุกตาราง + จำนวนระเบียน
  - company_info(folder)                : ข้อมูลบริษัทจาก ISINFO (ชื่อ/เลขภาษี)
  - chart_of_accounts(folder)           : ผังบัญชีจาก GLACC
  - trial_balance(folder, year_set)     : งบทดลองจาก GLBAL (เลือกงวด LY/CUR/NY)
  - dump_all_csv(folder, out_dir)       : ส่งออกทุกตารางเป็น CSV

Express GLBAL เก็บยอดเดบิต/เครดิตรายเดือนของ 3 งวดในระเบียนเดียว
    *LY  = งวดก่อน (last year)   เช่น DEBIT1LY..DEBIT12LY, CREDIT1LY..
    (ไม่มี suffix) = งวดเปิดปัจจุบัน  DEBIT1..DEBIT12, CREDIT1..CREDIT12
    *NY  = งวดถัดไป (next year)  DEBIT1NY..DEBIT12NY
    BEGLY = ยอดยกมาต้นงวดก่อน, DEBITCLS/CREDITCLS = รายการปิด/ปรับเข้างวดปัจจุบัน

สูตรที่ผ่านการตรวจสอบกับชีต TB ในไฟล์ Excel:
    begin(งวดปัจจุบัน) = BEGLY + ΣDEBITxLY - ΣCREDITxLY
    balance(งวดปัจจุบัน) = begin + (DEBITCLS - CREDITCLS) + ΣDEBITx - ΣCREDITx
"""
import os
import csv
import datetime
from dbfread import DBF

ENCODING = "cp874"          # ไทย TIS-620
MONTHS = range(1, 13)


# ---------- core readers ----------
def _dbf(path):
    return DBF(path, encoding=ENCODING, ignore_missing_memofile=True, char_decode_errors="replace")


def _find(folder, name):
    """หาไฟล์ .dbf แบบไม่สนตัวพิมพ์ใหญ่เล็ก (Express ปนทั้ง GLACC.DBF / glacc.dbf)"""
    for ext in (".DBF", ".dbf"):
        for nm in (name.upper(), name.lower()):
            p = os.path.join(folder, nm + ext)
            if os.path.exists(p):
                return p
    raise FileNotFoundError(f"ไม่พบตาราง {name} ใน {folder}")


def read_table(folder, name):
    return list(_dbf(_find(folder, name)))


def list_tables(folder):
    out = []
    for f in sorted(os.listdir(folder)):
        if f.lower().endswith(".dbf"):
            p = os.path.join(folder, f)
            try:
                n = len(_dbf(p))
            except Exception as e:
                n = f"ERROR: {e}"
            out.append((f, n))
    return out


def company_info(folder):
    r = read_table(folder, "ISINFO")[0]
    return {
        "thai_name": r.get("THINAM", ""),
        "eng_name": r.get("ENGNAM", ""),
        "tax_id": r.get("TAXID", ""),
        "vat_rate": r.get("VATRAT", ""),
        "year_thai": r.get("YEARTHAI", ""),
    }


def chart_of_accounts(folder):
    rows = read_table(folder, "GLACC")
    return [{
        "account_code": r["ACCNUM"].strip(),
        "account_name": (r.get("ACCNAM") or "").strip(),
        "account_name2": (r.get("ACCNAM2") or "").strip(),
        "level": r.get("LEVEL"),
        "parent": (r.get("PARENT") or "").strip(),
        "group": r.get("GROUP"),       # 1=สินทรัพย์ 2=หนี้สิน 3=ทุน 4=รายได้ 5=ค่าใช้จ่าย
        "acctype": r.get("ACCTYP"),
        "status": r.get("STATUS"),
    } for r in rows]


def _msum(r, prefix, suffix):
    return sum((r.get(f"{prefix}{m}{suffix}") or 0.0) for m in MONTHS)


def trial_balance(folder, year_set="CUR"):
    """
    คืนงบทดลองต่อบัญชี สำหรับงวดที่เลือก
      year_set = 'CUR' งวดเปิดปัจจุบัน | 'LY' งวดก่อน | 'NY' งวดถัดไป
    แต่ละแถว: account_code, begin_net, period_debit, period_credit, balance_net
    (net = เดบิต - เครดิต ; + คือยอดเดบิต, - คือยอดเครดิต)
    """
    suffix = {"LY": "LY", "CUR": "", "NY": "NY"}[year_set]
    rows = read_table(folder, "GLBAL")
    out = []
    for r in rows:
        acc = (r.get("ACCNUM") or "").strip()
        if not acc:
            continue
        begly = r.get("BEGLY") or 0.0
        cls = (r.get("DEBITCLS") or 0.0) - (r.get("CREDITCLS") or 0.0)
        ly_begin = begly                                              # ต้นงวดก่อน
        ly_end = ly_begin + _msum(r, "DEBIT", "LY") - _msum(r, "CREDIT", "LY")

        if year_set == "LY":
            begin = ly_begin
        elif year_set == "CUR":
            begin = ly_end                                           # ต้นงวดปัจจุบัน = ปลายงวดก่อน
        else:  # NY
            begin = ly_end + cls + _msum(r, "DEBIT", "") - _msum(r, "CREDIT", "")

        pdeb = _msum(r, "DEBIT", suffix)
        pcrd = _msum(r, "CREDIT", suffix)
        extra_cls = cls if year_set == "CUR" else 0.0                # รายการปิดเข้างวดปัจจุบัน
        balance = begin + extra_cls + pdeb - pcrd
        out.append({
            "account_code": acc,
            "begin_net": round(begin, 2),
            "period_debit": round(pdeb, 2),
            "period_credit": round(pcrd, 2),
            "balance_net": round(balance, 2),
        })
    return out


# ---------- export ----------
def _conv(v):
    if isinstance(v, (datetime.date, datetime.datetime)):
        return v.isoformat()
    return v


def dump_all_csv(folder, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    written = []
    for f in sorted(os.listdir(folder)):
        if not f.lower().endswith(".dbf"):
            continue
        name = os.path.splitext(f)[0]
        try:
            tbl = _dbf(os.path.join(folder, f))
            fields = tbl.field_names
            with open(os.path.join(out_dir, name + ".csv"), "w", newline="", encoding="utf-8-sig") as fp:
                w = csv.DictWriter(fp, fieldnames=fields)
                w.writeheader()
                for rec in tbl:
                    w.writerow({k: _conv(rec.get(k)) for k in fields})
            written.append(name)
        except Exception as e:
            print(f"  ข้าม {f}: {e}")
    return written


if __name__ == "__main__":
    import sys
    folder = sys.argv[1] if len(sys.argv) > 1 else r"D:\ExpressI\JSIT2016"
    info = company_info(folder)
    print("บริษัท:", info["thai_name"], "| เลขภาษี:", info["tax_id"])
    print("จำนวนบัญชีในผัง:", len(chart_of_accounts(folder)))
    tb = trial_balance(folder, "CUR")
    print("จำนวนบัญชีในงบทดลอง (งวดปัจจุบัน):", len(tb))
