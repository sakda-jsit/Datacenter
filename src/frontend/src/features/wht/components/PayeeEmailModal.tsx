import { useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useSetPayeeEmail } from '../hooks/useWht'

export interface PayeeRow {
  taxId: string
  name: string
  email: string
}

interface Props {
  companyId: number
  payees: PayeeRow[]
  onClose: () => void
}

export default function PayeeEmailModal({ companyId, payees, onClose }: Props) {
  const setEmail = useSetPayeeEmail(companyId)
  const [rows, setRows] = useState<PayeeRow[]>(payees)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)

  function patch(idx: number, email: string) {
    setRows((prev) => prev.map((r, i) => (i === idx ? { ...r, email } : r)))
    setSaved(false)
  }

  async function handleSave() {
    setError('')
    try {
      // บันทึกเฉพาะที่เปลี่ยนจากค่าเดิม
      const changed = rows.filter((r, i) => r.email.trim() !== (payees[i]?.email ?? '').trim())
      for (const r of changed) {
        await setEmail.mutateAsync({ taxId: r.taxId, email: r.email.trim() || null })
      }
      setSaved(true)
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'บันทึกไม่สำเร็จ')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-3xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">อีเมลผู้ถูกหักภาษี</h2>
            <p className="text-xs text-gray-500">กำหนดอีเมลของบุคคล/นิติบุคคล เพื่อใช้ส่งหนังสือรับรองหัก ณ ที่จ่าย (1 อีเมลต่อผู้ถูกหัก)</p>
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="px-6 py-4">
          {rows.length === 0 ? (
            <StateMessage centered>ไม่มีผู้ถูกหักในรายการที่แสดง</StateMessage>
          ) : (
            <div className="overflow-x-auto rounded border border-gray-200">
              <table className="w-full text-xs">
                <thead className="bg-slate-50 text-gray-600">
                  <tr>
                    <th className="px-3 py-2 text-left font-medium">ผู้ถูกหัก</th>
                    <th className="px-3 py-2 text-left font-medium w-36">เลขผู้เสียภาษี</th>
                    <th className="px-3 py-2 text-left font-medium w-72">อีเมล</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((r, i) => (
                    <tr key={r.taxId} className="border-t border-gray-100">
                      <td className="px-3 py-2 text-gray-700">{r.name || '—'}</td>
                      <td className="px-3 py-2 font-mono text-gray-500">{r.taxId}</td>
                      <td className="px-3 py-2">
                        <input
                          type="email" value={r.email} onChange={(e) => patch(i, e.target.value)}
                          placeholder="name@example.com"
                          className="w-full rounded border border-gray-300 px-2 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-slate-400"
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
          {saved && <p className="mt-3 rounded bg-green-50 px-3 py-2 text-sm text-green-700">บันทึกอีเมลแล้ว</p>}
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-100 px-6 py-4">
          <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
          <Button type="button" onClick={handleSave} disabled={setEmail.isPending || rows.length === 0}>
            {setEmail.isPending ? 'กำลังบันทึก...' : 'บันทึกอีเมล'}
          </Button>
        </div>
      </div>
    </div>
  )
}
