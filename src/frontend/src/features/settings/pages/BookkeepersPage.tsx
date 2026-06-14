import { useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useBookkeepers, useDeleteBookkeeper, useSaveBookkeeper } from '../hooks/useAuditRegistry'
import type { Bookkeeper, BookkeeperInput } from '../services/auditRegistryApi'

const blank: BookkeeperInput = { name: '', taxId: '', isActive: true }

export default function BookkeepersPage() {
  const { data, isLoading, isError } = useBookkeepers()
  const save = useSaveBookkeeper()
  const del = useDeleteBookkeeper()
  const [editId, setEditId] = useState<number | null>(null)
  const [form, setForm] = useState<BookkeeperInput>(blank)

  function startNew() { setEditId(0); setForm(blank) }
  function startEdit(b: Bookkeeper) { setEditId(b.id); setForm({ name: b.name, taxId: b.taxId ?? '', isActive: b.isActive }) }
  const taxInvalid = !!form.taxId && form.taxId.replace(/\D/g, '').length > 0 && form.taxId.replace(/\D/g, '').length !== 13
  const invalid = !form.name.trim() || taxInvalid

  async function onSave() {
    if (invalid) return
    await save.mutateAsync({ id: editId && editId > 0 ? editId : null,
      data: { ...form, taxId: (form.taxId || '').replace(/\D/g, '') || null } })
    setEditId(null)
  }

  return (
    <div className="max-w-3xl">
      <PageHeader title="ทะเบียนผู้ทำบัญชี" />
      <Card className="mb-4 p-4">
        <p className="text-sm text-gray-500">
          ผู้ทำบัญชี (master) ใช้เลือกเป็น "ผู้ลงนามประจำบริษัท" ในหน้า ภ.ง.ด.50 — ผู้ทำบัญชี 1 คนใช้ได้หลายบริษัท
        </p>
      </Card>

      {isLoading ? <StateMessage>กำลังโหลด...</StateMessage>
        : isError ? <StateMessage tone="error">โหลดไม่สำเร็จ</StateMessage>
        : (
        <Card className="overflow-hidden">
          <div className="flex items-center justify-between border-b border-gray-100 p-3">
            <span className="text-sm font-medium text-slate-700">รายชื่อผู้ทำบัญชี</span>
            <Button type="button" onClick={startNew} className="px-3 py-1 text-xs">+ เพิ่มผู้ทำบัญชี</Button>
          </div>
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-xs text-gray-500">
              <tr><th className="px-3 py-2 text-left">ชื่อ</th><th className="px-3 py-2 text-left">เลขผู้เสียภาษี</th><th className="px-3 py-2"></th></tr>
            </thead>
            <tbody>
              {(data ?? []).map((b) => (
                <tr key={b.id} className={`border-t border-gray-50 ${b.isActive ? '' : 'text-gray-300'}`}>
                  <td className="px-3 py-2">{b.name}{!b.isActive && ' (ปิดใช้งาน)'}</td>
                  <td className="px-3 py-2 font-mono">{b.taxId}</td>
                  <td className="px-3 py-2 text-right whitespace-nowrap">
                    <button onClick={() => startEdit(b)} className="text-blue-600 hover:underline">แก้ไข</button>
                    {b.isActive && <button onClick={() => del.mutate(b.id)} className="ml-3 text-red-500 hover:underline">ลบ</button>}
                  </td>
                </tr>
              ))}
              {(data ?? []).length === 0 && <tr><td colSpan={3} className="px-3 py-6 text-center text-gray-400">ยังไม่มีข้อมูล</td></tr>}
            </tbody>
          </table>
        </Card>
      )}

      {editId !== null && (
        <Card className="mt-4 p-5">
          <h3 className="mb-3 text-sm font-semibold text-slate-800">{editId > 0 ? 'แก้ไข' : 'เพิ่ม'}ผู้ทำบัญชี</h3>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="ชื่อผู้ทำบัญชี *"><input value={form.name} onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))} className={cls(!form.name.trim())} /></Field>
            <Field label="เลขผู้เสียภาษี (13 หลัก)"><input value={form.taxId ?? ''} maxLength={17} onChange={(e) => setForm((p) => ({ ...p, taxId: e.target.value }))} className={cls(taxInvalid)} /></Field>
          </div>
          <label className="mt-3 flex items-center gap-2 text-sm text-gray-600">
            <input type="checkbox" checked={form.isActive} onChange={(e) => setForm((p) => ({ ...p, isActive: e.target.checked }))} /> ใช้งาน
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
