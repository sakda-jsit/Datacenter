import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useNoteTemplates, useUpsertNoteTemplate, useResetNoteTemplate } from '../hooks/useFinancialStatement'
import type { NoteTemplateSectionDto } from '../types/financialStatement.types'

interface Props {
  companyId: number
  fiscalYear: number
  onClose: () => void
}

interface EditRow {
  noteKey: string
  title: string
  bodyText: string
  effectiveYear: number
  isOverride: boolean
  sortOrder: number
}

const PLACEHOLDERS = '{{CompanyName}} {{TaxId}} {{Address}} {{FiscalYearTh}} {{PriorYearTh}}'

export default function NoteTemplateEditorModal({ companyId, fiscalYear, onClose }: Props) {
  const { data, isLoading } = useNoteTemplates({ clientCompanyId: companyId, fiscalYear })
  const upsert = useUpsertNoteTemplate()
  const reset = useResetNoteTemplate()
  const [rows, setRows] = useState<EditRow[]>([])
  const [msg, setMsg] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (data) {
      setRows(data.map((s: NoteTemplateSectionDto) => ({
        noteKey: s.noteKey,
        title: s.title,
        bodyText: s.bodyText,
        effectiveYear: s.effectiveYear,
        isOverride: s.clientCompanyId != null,
        sortOrder: s.sortOrder,
      })))
    }
  }, [data])

  function patch(key: string, field: 'title' | 'bodyText', value: string) {
    setRows((prev) => prev.map((r) => (r.noteKey === key ? { ...r, [field]: value } : r)))
    setMsg(''); setError('')
  }

  async function save(r: EditRow) {
    setMsg(''); setError('')
    try {
      await upsert.mutateAsync({
        clientCompanyId: companyId,
        effectiveYear: r.effectiveYear,
        noteKey: r.noteKey,
        title: r.title,
        bodyText: r.bodyText,
        sortOrder: r.sortOrder,
      })
      setMsg(`บันทึกข้อความหมายเหตุข้อ ${r.noteKey} แล้ว (เฉพาะบริษัทนี้)`)
    } catch (err) {
      setError(extractError(err))
    }
  }

  async function revert(r: EditRow) {
    setMsg(''); setError('')
    try {
      await reset.mutateAsync({ clientCompanyId: companyId, effectiveYear: r.effectiveYear, noteKey: r.noteKey })
      setMsg(`คืนค่าข้อความข้อ ${r.noteKey} กลับเป็นแบบมาตรฐานแล้ว`)
    } catch (err) {
      setError(extractError(err))
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-4xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">แก้ไขข้อความหมายเหตุประกอบงบ (NOTE2)</h2>
            <p className="text-xs text-gray-500">
              แก้ได้เฉพาะ "ข้อความบรรยาย" (ตัวเลขดึงจากงบอัตโนมัติ แก้ไม่ได้). บันทึกแล้วใช้เฉพาะบริษัทนี้ —
              ตัวแปรที่ระบบแทนค่า: <span className="font-mono">{PLACEHOLDERS}</span>
            </p>
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="max-h-[70vh] overflow-y-auto px-6 py-4">
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
          {!isLoading && rows.length === 0 && <StateMessage centered>ไม่มีข้อความ template</StateMessage>}

          <div className="space-y-5">
            {rows.map((r) => (
              <div key={r.noteKey} className="rounded-lg border border-gray-200 p-3">
                <div className="mb-2 flex items-center gap-2">
                  <span className="rounded bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-600">ข้อ {r.noteKey}</span>
                  <input
                    value={r.title}
                    onChange={(e) => patch(r.noteKey, 'title', e.target.value)}
                    className="flex-1 rounded border border-gray-300 px-2 py-1 text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-slate-400"
                  />
                  {r.isOverride
                    ? <span className="rounded bg-amber-100 px-2 py-0.5 text-[11px] text-amber-700">แก้ไขเฉพาะบริษัท</span>
                    : <span className="rounded bg-gray-100 px-2 py-0.5 text-[11px] text-gray-500">มาตรฐาน</span>}
                </div>
                <textarea
                  value={r.bodyText}
                  onChange={(e) => patch(r.noteKey, 'bodyText', e.target.value)}
                  rows={Math.min(10, Math.max(3, r.bodyText.split('\n').length))}
                  className="w-full rounded border border-gray-300 px-2 py-1.5 text-xs leading-relaxed focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
                <div className="mt-2 flex justify-end gap-2">
                  {r.isOverride && (
                    <Button type="button" variant="secondary" onClick={() => revert(r)} disabled={reset.isPending}>
                      คืนค่ามาตรฐาน
                    </Button>
                  )}
                  <Button type="button" onClick={() => save(r)} disabled={upsert.isPending}>
                    {upsert.isPending ? 'กำลังบันทึก...' : 'บันทึก'}
                  </Button>
                </div>
              </div>
            ))}
          </div>

          {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
          {msg && <p className="mt-3 rounded bg-green-50 px-3 py-2 text-sm text-green-700">{msg}</p>}
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-100 px-6 py-4">
          <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
        </div>
      </div>
    </div>
  )
}

function extractError(err: unknown): string {
  const data = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
  return data?.detail ?? data?.title ?? 'ดำเนินการไม่สำเร็จ'
}
