# -*- coding: utf-8 -*-
import sys
from dbfread import DBF

def diag(path, n=5):
    print(f"\n===== {path} =====")
    try:
        t = DBF(path, encoding='cp874', char_decode_errors='replace', load=False, ignore_missing_memofile=True)
    except Exception as e:
        print("OPEN ERROR:", e); return
    print(f"records~ {len(t)}")
    print("FIELDS:")
    for f in t.fields:
        print(f"  {f.name:12s} {f.type} {f.length:>4d} dec={f.decimal_count}")
    print("SAMPLE:")
    i = 0
    for rec in t:
        # skip empty rows
        if all((v is None or str(v).strip()=='' or v==0) for v in rec.values()):
            continue
        print(f"--- row {i} ---")
        for k, v in rec.items():
            sv = str(v).strip()
            if sv and sv != '0' and sv != '0.0' and sv != 'None':
                print(f"  {k:12s} = {sv!r}")
        i += 1
        if i >= n: break

if __name__ == '__main__':
    for p in sys.argv[1:]:
        diag(p)
