import { useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import { useSendWht } from '../hooks/useWht'
import type { WhtSendResult } from '../types/wht.types'

interface Props {
  companyId: number
  entryIds: number[]
  onClose: () => void
  onSent: (results: WhtSendResult[]) => void
}

interface SendDefault { mode: 0 | 1; email: string }

function loadDefault(companyId: number): SendDefault {
  try {
    const raw = localStorage.getItem(`wht.sendDefault.${companyId}`)
    if (raw) {
      const d = JSON.parse(raw) as SendDefault
      return { mode: d.mode === 1 ? 1 : 0, email: d.email ?? '' }
    }
  } catch { /* ignore */ }
  return { mode: 0, email: '' }
}

export default function WhtSendModal({ companyId, entryIds, onClose, onSent }: Props) {
  const send = useSendWht(companyId)
  const initial = loadDefault(companyId)
  const [mode, setMode] = useState<0 | 1>(initial.mode)
  const [email, setEmail] = useState(initial.email)
  const [saveDefault, setSaveDefault] = useState(false)
  const [error, setError] = useState('')

  async function confirm() {
    setError('')
    if (mode === 1 && !email.trim()) {
      setError('กรุณาระบุอีเมลผู้รับ')
      return
    }
    try {
      const res = await send.mutateAsync({
        entryIds,
        grouping: mode,
        recipientEmail: mode === 1 ? email.trim() : undefined,
      })
      if (saveDefault) {
        localStorage.setItem(`wht.sendDefault.${companyId}`,
          JSON.stringify({ mode, email: mode === 1 ? email.trim() : '' } satisfies SendDefault))
      }
      onSent(res)
      onClose()
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'ส่งอีเมลไม่สำเร็จ')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4">
      <div className="my-12 w-full max-w-lg rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <h2 className="text-lg font-bold text-slate-800">ส่งหนังสือรับรองหัก ณ ที่จ่าย</h2>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="space-y-4 px-6 py-4">
          <p className="text-sm text-gray-600">เลือกรูปแบบการส่ง ({entryIds.length} ฉบับที่เลือก)</p>

          <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-gray-200 p-3 hover:bg-slate-50">
            <input type="radio" name="mode" checked={mode === 0} onChange={() => setMode(0)} className="mt-1" />
            <span>
              <span className="block text-sm font-medium text-slate-800">รวมตามผู้ถูกหัก</span>
              <span className="block text-xs text-gray-500">ส่ง 1 อีเมลต่อผู้ถูกหัก 1 ราย — ถ้ามีหลายฉบับของคนเดียวกัน แนบรวมส่งครั้งเดียว (ใช้อีเมลที่กำหนดไว้รายผู้ถูกหัก)</span>
            </span>
          </label>

          <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-gray-200 p-3 hover:bg-slate-50">
            <input type="radio" name="mode" checked={mode === 1} onChange={() => setMode(1)} className="mt-1" />
            <span className="flex-1">
              <span className="block text-sm font-medium text-slate-800">รวมส่งเมลเดียว</span>
              <span className="block text-xs text-gray-500">แนบทุกฉบับที่เลือกในอีเมลฉบับเดียว ส่งไปยังอีเมลเดียวที่ระบุ (เช่น อีเมลกลางของบริษัท)</span>
              {mode === 1 && (
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="name@example.com"
                  className="mt-2 w-full rounded border border-gray-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
              )}
            </span>
          </label>

          <label className="flex cursor-pointer items-center gap-2 text-sm text-gray-600">
            <input type="checkbox" checked={saveDefault} onChange={(e) => setSaveDefault(e.target.checked)} className="rounded" />
            บันทึกเป็นค่าเริ่มต้นของบริษัทนี้
          </label>

          {error && <p className="rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-100 px-6 py-4">
          <Button type="button" variant="secondary" onClick={onClose}>ยกเลิก</Button>
          <Button type="button" onClick={confirm} disabled={send.isPending || entryIds.length === 0}>
            {send.isPending ? 'กำลังส่ง...' : 'ส่งอีเมล'}
          </Button>
        </div>
      </div>
    </div>
  )
}
