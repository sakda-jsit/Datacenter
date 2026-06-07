import { useEffect, useRef, useState } from 'react'
import { runExport, type ExportFormat, type ExportSection } from '../../../shared/utils/exportTable'
import { auditLogApi } from '../services/auditLogApi'
import type { AuditLogDto, AuditLogExportParams } from '../types/auditLog.types'

interface Props {
  params: AuditLogExportParams
  /** ป้ายช่วงข้อมูล (เช่น ช่วงวันที่/บริษัท) ใส่ใน subtitle */
  subtitle?: string
  fmtDateTime: (iso: string) => string
}

const OPTIONS: { fmt: ExportFormat; label: string }[] = [
  { fmt: 'xlsx', label: 'Excel (.xlsx)' },
  { fmt: 'csv', label: 'CSV' },
  { fmt: 'pdf', label: 'PDF (พิมพ์)' },
]

function buildSections(rows: AuditLogDto[], fmtDateTime: (iso: string) => string): ExportSection[] {
  return [{
    name: 'ประวัติการใช้งาน',
    columns: [
      { key: 'createdAt', header: 'เวลา', value: (r) => fmtDateTime(r.createdAt) },
      { key: 'username', header: 'ผู้ใช้', value: (r) => r.username || '' },
      { key: 'action', header: 'การกระทำ' },
      { key: 'entityName', header: 'รายการ' },
      { key: 'entityId', header: 'รหัส' },
      { key: 'fieldName', header: 'ฟิลด์', value: (r) => r.fieldName ?? '' },
      { key: 'clientName', header: 'บริษัท', value: (r) => r.clientName ?? '' },
      { key: 'beforeValue', header: 'ก่อน', value: (r) => r.beforeValue ?? '' },
      { key: 'afterValue', header: 'หลัง', value: (r) => r.afterValue ?? '' },
    ],
    rows,
  }]
}

/**
 * ส่งออก audit log "ทั้งชุดตามตัวกรอง" (ไม่ใช่แค่หน้าปัจจุบัน) — ดึงจาก /audit-log/export
 * แล้วสร้างไฟล์ผ่าน utility กลาง. แจ้งเตือนถ้าจำนวนเกิน cap.
 */
export default function AuditLogExportMenu({ params, subtitle, fmtDateTime }: Props) {
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const ref = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (!open) return
    function onDown(e: PointerEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('pointerdown', onDown)
    return () => document.removeEventListener('pointerdown', onDown)
  }, [open])

  async function pick(fmt: ExportFormat) {
    setOpen(false)
    setBusy(true)
    try {
      const res = await auditLogApi.export(params)
      if (res.items.length === 0) {
        alert('ไม่มีข้อมูลให้ส่งออก')
        return
      }
      if (res.capped) {
        const ok = window.confirm(
          `พบ ${res.totalCount.toLocaleString('th-TH')} รายการ แต่ส่งออกได้สูงสุด ${res.cap.toLocaleString('th-TH')} รายการ\n` +
          `(จะได้ ${res.cap.toLocaleString('th-TH')} รายการล่าสุด) — แนะนำแคบช่วงวันที่ลง ดำเนินการต่อหรือไม่?`,
        )
        if (!ok) return
      }
      const total = res.totalCount.toLocaleString('th-TH')
      const meta = {
        title: 'ประวัติการใช้งาน (Audit Log)',
        subtitle: `${subtitle ? subtitle + ' · ' : ''}รวม ${total} รายการ`,
        fileName: 'audit-log',
      }
      runExport(fmt, meta, buildSections(res.items, fmtDateTime))
    } catch {
      alert('ส่งออกไม่สำเร็จ กรุณาลองใหม่')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="relative inline-block" ref={ref}>
      <button
        type="button"
        disabled={busy}
        onClick={() => setOpen((o) => !o)}
        className="inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
      >
        <svg className="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
          <path d="M12 3v12M8 11l4 4 4-4" />
          <path d="M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2" />
        </svg>
        {busy ? 'กำลังส่งออก...' : 'ส่งออกทั้งหมด'}
        <span className="text-xs text-slate-400">▾</span>
      </button>

      {open && (
        <div className="absolute right-0 z-50 mt-1 w-44 overflow-hidden rounded-lg border border-slate-200 bg-white shadow-lg">
          {OPTIONS.map((o) => (
            <button
              key={o.fmt}
              type="button"
              onClick={() => pick(o.fmt)}
              className="block w-full px-4 py-2 text-left text-sm text-slate-700 hover:bg-sky-50 hover:text-sky-700"
            >
              {o.label}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
