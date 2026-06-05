import { useEffect, useRef, useState } from 'react'
import type { ExportMeta, ExportSection, ExportFormat } from '../../utils/exportTable'
import { runExport } from '../../utils/exportTable'

interface Props {
  meta: ExportMeta
  /** ส่วนของรายงาน (อาจมีหลายตาราง → หลาย sheet/section) — หรือใช้ getSections() เพื่อ lazy build */
  sections?: ExportSection[]
  getSections?: () => ExportSection[]
  disabled?: boolean
  label?: string
  className?: string
}

const OPTIONS: { fmt: ExportFormat; label: string }[] = [
  { fmt: 'xlsx', label: 'Excel (.xlsx)' },
  { fmt: 'csv', label: 'CSV' },
  { fmt: 'pdf', label: 'PDF (พิมพ์)' },
]

export default function ExportMenu({ meta, sections, getSections, disabled, label = 'ส่งออก', className = '' }: Props) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (!open) return
    function onDown(e: PointerEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('pointerdown', onDown)
    return () => document.removeEventListener('pointerdown', onDown)
  }, [open])

  function pick(fmt: ExportFormat) {
    setOpen(false)
    const data = sections ?? getSections?.() ?? []
    if (data.length === 0 || data.every((s) => s.rows.length === 0)) {
      alert('ไม่มีข้อมูลให้ส่งออก')
      return
    }
    runExport(fmt, meta, data)
  }

  return (
    <div className={`relative inline-block ${className}`} ref={ref}>
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen((o) => !o)}
        className="inline-flex items-center gap-1.5 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
      >
        <svg className="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
          <path d="M12 3v12M8 11l4 4 4-4" />
          <path d="M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2" />
        </svg>
        {label}
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
