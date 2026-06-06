import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import { useDeletePayrollConfig, usePayrollConfigs, useSavePayrollConfig } from '../../hooks/usePayroll'
import type { PayrollRateConfig, PayrollRateConfigInput } from '../../types/payroll.types'

interface Props {
  companyId: number
}

const EMPTY: PayrollRateConfigInput = {
  effectiveFrom: new Date().toISOString().slice(0, 10),
  ssoEmployeePct: 5, ssoEmployerPct: 5, ssoWageFloor: 1650, ssoWageCap: 15000,
  wcfRatePct: 0.2, wcfWageCapPerYear: 240000, note: '',
}

export default function RateConfigTab({ companyId }: Props) {
  const { data, isLoading } = usePayrollConfigs(companyId)
  const save = useSavePayrollConfig(companyId)
  const del = useDeletePayrollConfig(companyId)
  const [editId, setEditId] = useState<number | null>(null)
  const [form, setForm] = useState<PayrollRateConfigInput | null>(null)

  function openNew() { setEditId(null); setForm({ ...EMPTY }) }
  function openEdit(c: PayrollRateConfig) {
    setEditId(c.id)
    setForm({
      effectiveFrom: c.effectiveFrom.slice(0, 10), ssoEmployeePct: c.ssoEmployeePct, ssoEmployerPct: c.ssoEmployerPct,
      ssoWageFloor: c.ssoWageFloor, ssoWageCap: c.ssoWageCap, wcfRatePct: c.wcfRatePct,
      wcfWageCapPerYear: c.wcfWageCapPerYear, note: c.note ?? '',
    })
  }
  async function submit() {
    if (!form) return
    await save.mutateAsync({ id: editId, data: form })
    setForm(null); setEditId(null)
  }
  function setF<K extends keyof PayrollRateConfigInput>(k: K, v: PayrollRateConfigInput[K]) {
    setForm((p) => (p ? { ...p, [k]: v } : p))
  }

  const rows = data ?? []

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-center justify-between gap-3 px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">อัตราเงินสมทบ ประกันสังคม / กองทุนเงินทดแทน</p>
          <p className="text-xs text-gray-500">มีผลตามวันที่ (effective-dated) — เพิ่มอัตราใหม่ไม่กระทบงวดที่คำนวณไปแล้ว · “ค่ากลาง” ใช้ทุกบริษัท แก้ไม่ได้</p>
        </div>
        <Button type="button" onClick={openNew}>+ เพิ่มอัตรา (เฉพาะบริษัทนี้)</Button>
      </Card>

      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50 text-xs text-gray-600">
              <tr>
                <th className="px-4 py-2 text-left font-medium">มีผลตั้งแต่</th>
                <th className="px-4 py-2 text-left font-medium w-24">ขอบเขต</th>
                <th className="px-4 py-2 text-right font-medium">ปกส. ลูกจ้าง%</th>
                <th className="px-4 py-2 text-right font-medium">ปกส. นายจ้าง%</th>
                <th className="px-4 py-2 text-right font-medium">ฐานต่ำ/สูง</th>
                <th className="px-4 py-2 text-right font-medium">กองทุน%</th>
                <th className="px-4 py-2 text-right font-medium">เพดานกองทุน/ปี</th>
                <th className="px-4 py-2 text-right font-medium w-28">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((c) => (
                <tr key={c.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-1.5 font-mono text-xs">{c.effectiveFrom.slice(0, 10)}</td>
                  <td className="px-4 py-1.5">
                    {c.isGlobal
                      ? <span className="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-600">ค่ากลาง</span>
                      : <span className="rounded bg-sky-50 px-2 py-0.5 text-xs text-sky-700">บริษัทนี้</span>}
                  </td>
                  <td className="px-4 py-1.5 text-right font-mono">{c.ssoEmployeePct}</td>
                  <td className="px-4 py-1.5 text-right font-mono">{c.ssoEmployerPct}</td>
                  <td className="px-4 py-1.5 text-right font-mono text-gray-500">{c.ssoWageFloor.toLocaleString()}/{c.ssoWageCap.toLocaleString()}</td>
                  <td className="px-4 py-1.5 text-right font-mono">{c.wcfRatePct}</td>
                  <td className="px-4 py-1.5 text-right font-mono text-gray-500">{c.wcfWageCapPerYear.toLocaleString()}</td>
                  <td className="px-4 py-1.5 text-right">
                    {c.isGlobal ? (
                      <span className="text-xs text-gray-300">—</span>
                    ) : (
                      <div className="flex justify-end gap-1 whitespace-nowrap">
                        <Button type="button" variant="ghost" onClick={() => openEdit(c)} className="px-2 py-1 text-xs">แก้ไข</Button>
                        <Button type="button" variant="ghost" onClick={() => { if (window.confirm('ลบอัตรานี้?')) del.mutate(c.id) }} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {form && (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
          <div className="my-12 w-full max-w-lg rounded-2xl bg-white shadow-2xl">
            <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
              <h2 className="text-lg font-bold text-slate-800">{editId ? 'แก้ไขอัตรา' : 'เพิ่มอัตรา (เฉพาะบริษัทนี้)'}</h2>
              <button type="button" onClick={() => setForm(null)} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
            </div>
            <div className="grid grid-cols-2 gap-3 px-6 py-4">
              <L label="มีผลตั้งแต่ *"><input type="date" className={inp} value={form.effectiveFrom} onChange={(e) => setF('effectiveFrom', e.target.value)} /></L>
              <span />
              <L label="ปกส. ลูกจ้าง %"><input type="number" step="0.01" className={inp} value={form.ssoEmployeePct} onChange={(e) => setF('ssoEmployeePct', Number(e.target.value))} /></L>
              <L label="ปกส. นายจ้าง %"><input type="number" step="0.01" className={inp} value={form.ssoEmployerPct} onChange={(e) => setF('ssoEmployerPct', Number(e.target.value))} /></L>
              <L label="ฐานค่าจ้างต่ำสุด"><input type="number" className={inp} value={form.ssoWageFloor} onChange={(e) => setF('ssoWageFloor', Number(e.target.value))} /></L>
              <L label="ฐานค่าจ้างสูงสุด"><input type="number" className={inp} value={form.ssoWageCap} onChange={(e) => setF('ssoWageCap', Number(e.target.value))} /></L>
              <L label="กองทุนทดแทน %"><input type="number" step="0.01" className={inp} value={form.wcfRatePct} onChange={(e) => setF('wcfRatePct', Number(e.target.value))} /></L>
              <L label="เพดานกองทุน/ปี"><input type="number" className={inp} value={form.wcfWageCapPerYear} onChange={(e) => setF('wcfWageCapPerYear', Number(e.target.value))} /></L>
              <div className="col-span-2"><L label="หมายเหตุ"><input className={inp} value={form.note} onChange={(e) => setF('note', e.target.value)} /></L></div>
            </div>
            <div className="flex justify-end gap-2 border-t border-slate-100 px-6 py-4">
              <Button type="button" variant="secondary" onClick={() => setForm(null)}>ยกเลิก</Button>
              <Button type="button" onClick={submit} disabled={save.isPending}>{save.isPending ? 'กำลังบันทึก...' : 'บันทึก'}</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

const inp = 'w-full rounded border border-gray-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400'
function L({ label, children }: { label: string; children: React.ReactNode }) {
  return <label className="block"><span className="mb-1 block text-xs font-medium text-gray-600">{label}</span>{children}</label>
}
