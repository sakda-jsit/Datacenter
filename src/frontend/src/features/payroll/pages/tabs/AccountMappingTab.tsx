import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import { useDeleteAccountMapping, usePayrollAccountMappings, useSaveAccountMapping } from '../../hooks/usePayroll'
import {
  PAYROLL_POSTING_ROLE,
  PAYROLL_POSTING_ROLE_COMPANYWIDE,
  type PayrollAccountMapping,
} from '../../types/payroll.types'

interface Props {
  companyId: number
}

const ROLE_OPTIONS = Object.entries(PAYROLL_POSTING_ROLE).map(([k, v]) => ({ value: Number(k), label: v }))

export default function AccountMappingTab({ companyId }: Props) {
  const { data, isLoading } = usePayrollAccountMappings(companyId)
  const save = useSaveAccountMapping(companyId)
  const del = useDeleteAccountMapping(companyId)
  const [editId, setEditId] = useState<number | null>(null)
  const [accountCode, setAccountCode] = useState('')
  const [role, setRole] = useState(1)
  const [department, setDepartment] = useState('')
  const [error, setError] = useState('')

  const companyWide = PAYROLL_POSTING_ROLE_COMPANYWIDE.includes(role)

  function reset() { setEditId(null); setAccountCode(''); setRole(1); setDepartment(''); setError('') }
  function edit(m: PayrollAccountMapping) { setEditId(m.id); setAccountCode(m.accountCode); setRole(m.role); setDepartment(m.department ?? '') }
  async function submit() {
    setError('')
    if (!accountCode.trim() || (!companyWide && !department.trim())) { setError('กรุณาระบุรหัสบัญชี และฝ่าย (สำหรับบทบาทที่แยกฝ่าย)'); return }
    try {
      await save.mutateAsync({ id: editId, data: { accountCode: accountCode.trim(), role, department: companyWide ? null : department.trim() } })
      reset()
    } catch (err) {
      const m = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(m?.detail ?? m?.title ?? 'บันทึกไม่สำเร็จ')
    }
  }

  const rows = data ?? []

  return (
    <div>
      <div className="mb-3 rounded-lg border border-sky-200 bg-sky-50 px-4 py-2 text-xs text-sky-800">
        🔗 แมพรหัสบัญชี GL ตาม<b>บทบาท</b> ใช้ 2 อย่าง: (1) บทบาท <b>เงินเดือน/ค่าจ้างรายวัน</b> = ระบบดึงพนักงานจาก Express อัตโนมัติ ·
        (2) บทบาทอื่น = ใช้สร้างใบสำคัญลงบัญชี + กระทบยอด GL (ปุ่ม “ลงบัญชี/กระทบยอด” ในงวด) — บทบาทรอนำส่ง/ภาษีค้าง ไม่ต้องระบุฝ่าย
      </div>

      <Card className="mb-4 px-6 py-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">รหัสบัญชี GL</label>
            <input value={accountCode} onChange={(e) => setAccountCode(e.target.value)} placeholder="5310-01" className="w-32 rounded border border-gray-300 px-2 py-1.5 text-sm" />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">บทบาท</label>
            <select value={role} onChange={(e) => setRole(Number(e.target.value))} className="w-56 rounded border border-gray-300 px-2 py-1.5 text-sm">
              {ROLE_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ฝ่าย/แผนก{companyWide ? ' (ไม่ต้องระบุ)' : ''}</label>
            <input value={department} onChange={(e) => setDepartment(e.target.value)} placeholder="ฝ่ายบริหาร" disabled={companyWide} className="w-44 rounded border border-gray-300 px-2 py-1.5 text-sm disabled:bg-gray-100 disabled:text-gray-400" />
          </div>
          <Button type="button" onClick={submit} disabled={save.isPending}>{editId ? 'บันทึกแก้ไข' : '+ เพิ่ม'}</Button>
          {editId && <Button type="button" variant="secondary" onClick={reset}>ยกเลิก</Button>}
        </div>
        {error && <p className="mt-2 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
      </Card>

      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
      {data && rows.length === 0 && (
        <Card><StateMessage centered>ยังไม่ได้แมพบัญชี — เพิ่มรหัสบัญชีด้านบน</StateMessage></Card>
      )}
      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50 text-xs text-gray-600">
              <tr>
                <th className="px-4 py-2 text-left font-medium w-32">รหัสบัญชี GL</th>
                <th className="px-4 py-2 text-left font-medium">บทบาท</th>
                <th className="px-4 py-2 text-left font-medium w-40">ฝ่าย/แผนก</th>
                <th className="px-4 py-2 text-right font-medium w-28">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((m) => (
                <tr key={m.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-1.5 font-mono">{m.accountCode}</td>
                  <td className="px-4 py-1.5 text-gray-700">{PAYROLL_POSTING_ROLE[m.role] ?? m.role}</td>
                  <td className="px-4 py-1.5 text-gray-600">{m.department || <span className="text-gray-400">— ทั้งบริษัท —</span>}</td>
                  <td className="px-4 py-1.5 text-right">
                    <div className="flex justify-end gap-1">
                      <Button type="button" variant="ghost" onClick={() => edit(m)} className="px-2 py-1 text-xs">แก้ไข</Button>
                      <Button type="button" variant="ghost" onClick={() => { if (window.confirm('ลบการแมพนี้?')) del.mutate(m.id) }} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}
