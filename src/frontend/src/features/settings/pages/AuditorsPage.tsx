import { useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAuditors, useDeleteAuditor, useSaveAuditor } from '../hooks/useAuditRegistry'
import { AUDITOR_TYPE_LABEL, AuditorType, type Auditor, type AuditorInput } from '../services/auditRegistryApi'

const blank: AuditorInput = {
  name: '', type: AuditorType.Cpa, licenseNo: '', taxId: '',
  auditFirmName: '', auditFirmTaxId: '', isActive: true,
}

export default function AuditorsPage() {
  const { data, isLoading, isError } = useAuditors()
  const save = useSaveAuditor()
  const del = useDeleteAuditor()
  const [editId, setEditId] = useState<number | null>(null)
  const [form, setForm] = useState<AuditorInput>(blank)

  function startNew() { setEditId(0); setForm(blank) }
  function startEdit(a: Auditor) {
    setEditId(a.id)
    setForm({ name: a.name, type: a.type, licenseNo: a.licenseNo ?? '', taxId: a.taxId ?? '',
      auditFirmName: a.auditFirmName ?? '', auditFirmTaxId: a.auditFirmTaxId ?? '', isActive: a.isActive })
  }
  function set<K extends keyof AuditorInput>(k: K, v: AuditorInput[K]) { setForm((p) => ({ ...p, [k]: v })) }

  const taxInvalid = (v?: string | null) => !!v && v.replace(/\D/g, '').length > 0 && v.replace(/\D/g, '').length !== 13
  const invalid = !form.name.trim() || taxInvalid(form.taxId) || taxInvalid(form.auditFirmTaxId)

  async function onSave() {
    if (invalid) return
    await save.mutateAsync({ id: editId && editId > 0 ? editId : null, data: {
      ...form,
      taxId: (form.taxId || '').replace(/\D/g, '') || null,
      auditFirmTaxId: (form.auditFirmTaxId || '').replace(/\D/g, '') || null,
    } })
    setEditId(null)
  }

  return (
    <div className="max-w-4xl">
      <PageHeader title="ทะเบียนผู้สอบบัญชี" />
      <Card className="mb-4 p-4">
        <p className="text-sm text-gray-500">
          ผู้ตรวจสอบและรับรองบัญชี (master) ใช้เลือกเป็น "ผู้ลงนามประจำบริษัท" ในหน้า ภ.ง.ด.50 — ผู้สอบ 1 คนใช้ได้หลายบริษัท
        </p>
        <p className="mt-1 text-xs text-amber-600">หมายเหตุ: TA สอบได้เฉพาะห้างหุ้นส่วนจดทะเบียนขนาดเล็ก · บริษัทจำกัดต้องใช้ CPA</p>
      </Card>

      {isLoading ? <StateMessage>กำลังโหลด...</StateMessage>
        : isError ? <StateMessage tone="error">โหลดไม่สำเร็จ</StateMessage>
        : (
        <Card className="overflow-hidden">
          <div className="flex items-center justify-between border-b border-gray-100 p-3">
            <span className="text-sm font-medium text-slate-700">รายชื่อผู้สอบบัญชี</span>
            <Button type="button" onClick={startNew} className="px-3 py-1 text-xs">+ เพิ่มผู้สอบบัญชี</Button>
          </div>
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-xs text-gray-500">
              <tr>
                <th className="px-3 py-2 text-left">ชื่อ</th>
                <th className="px-3 py-2 text-left">ประเภท</th>
                <th className="px-3 py-2 text-left">ทะเบียน</th>
                <th className="px-3 py-2 text-left">เลขผู้เสียภาษี</th>
                <th className="px-3 py-2 text-left">สำนักงานสอบบัญชี</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {(data ?? []).map((a) => (
                <tr key={a.id} className={`border-t border-gray-50 ${a.isActive ? '' : 'text-gray-300'}`}>
                  <td className="px-3 py-2">{a.name}{!a.isActive && ' (ปิดใช้งาน)'}</td>
                  <td className="px-3 py-2">{a.type === AuditorType.TaxAuditor ? 'TA' : 'CPA'}</td>
                  <td className="px-3 py-2 font-mono">{a.licenseNo}</td>
                  <td className="px-3 py-2 font-mono">{a.taxId}</td>
                  <td className="px-3 py-2">{a.auditFirmName}</td>
                  <td className="px-3 py-2 text-right whitespace-nowrap">
                    <button onClick={() => startEdit(a)} className="text-blue-600 hover:underline">แก้ไข</button>
                    {a.isActive && <button onClick={() => del.mutate(a.id)} className="ml-3 text-red-500 hover:underline">ลบ</button>}
                  </td>
                </tr>
              ))}
              {(data ?? []).length === 0 && <tr><td colSpan={6} className="px-3 py-6 text-center text-gray-400">ยังไม่มีข้อมูล</td></tr>}
            </tbody>
          </table>
        </Card>
      )}

      {editId !== null && (
        <Card className="mt-4 p-5">
          <h3 className="mb-3 text-sm font-semibold text-slate-800">{editId > 0 ? 'แก้ไข' : 'เพิ่ม'}ผู้สอบบัญชี</h3>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="ชื่อผู้สอบบัญชี *"><input value={form.name} onChange={(e) => set('name', e.target.value)} className={cls(!form.name.trim())} /></Field>
            <Field label="ประเภททะเบียน">
              <select value={form.type} onChange={(e) => set('type', Number(e.target.value))} className={cls(false)}>
                {Object.entries(AUDITOR_TYPE_LABEL).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </Field>
            <Field label="ทะเบียนเลขที่ผู้สอบบัญชี"><input value={form.licenseNo ?? ''} onChange={(e) => set('licenseNo', e.target.value)} className={cls(false)} /></Field>
            <Field label="เลขผู้เสียภาษีผู้สอบบัญชี (13 หลัก)"><input value={form.taxId ?? ''} maxLength={17} onChange={(e) => set('taxId', e.target.value)} className={cls(taxInvalid(form.taxId))} /></Field>
            <Field label="ชื่อสำนักงานสอบบัญชี"><input value={form.auditFirmName ?? ''} onChange={(e) => set('auditFirmName', e.target.value)} className={cls(false)} /></Field>
            <Field label="เลขผู้เสียภาษีสำนักงานสอบบัญชี (13 หลัก)"><input value={form.auditFirmTaxId ?? ''} maxLength={17} onChange={(e) => set('auditFirmTaxId', e.target.value)} className={cls(taxInvalid(form.auditFirmTaxId))} /></Field>
          </div>
          <label className="mt-3 flex items-center gap-2 text-sm text-gray-600">
            <input type="checkbox" checked={form.isActive} onChange={(e) => set('isActive', e.target.checked)} /> ใช้งาน
          </label>
          <div className="mt-4 flex gap-2">
            <Button type="button" onClick={onSave} disabled={invalid || save.isPending}>{save.isPending ? 'กำลังบันทึก...' : 'บันทึก'}</Button>
            <Button type="button" variant="secondary" onClick={() => setEditId(null)}>ยกเลิก</Button>
          </div>
        </Card>
      )}
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return <div><label className="mb-1 block text-xs font-medium text-gray-600">{label}</label>{children}</div>
}
function cls(err: boolean) {
  return `w-full rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 ${err ? 'border-red-400' : 'border-gray-300'}`
}
