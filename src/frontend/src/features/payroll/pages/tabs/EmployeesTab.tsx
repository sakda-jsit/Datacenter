import { useEffect, useMemo, useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import Pagination from '../../../../shared/components/ui/Pagination'
import { useDeleteEmployee, useEmployees } from '../../hooks/usePayroll'
import {
  EMPLOYMENT_STATUS_LABEL,
  SSO_STATUS_CLASS,
  SSO_STATUS_LABEL,
  type EmployeeListItem,
} from '../../types/payroll.types'
import EmployeeFormModal from '../../components/EmployeeFormModal'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
}

export default function EmployeesTab({ companyId }: Props) {
  const [includeResigned, setIncludeResigned] = useState(true)
  const { data, isLoading, isError } = useEmployees(companyId, includeResigned)
  const del = useDeleteEmployee(companyId)
  const [editId, setEditId] = useState<number | null>(null)
  const [formOpen, setFormOpen] = useState(false)

  function openCreate() {
    setEditId(null)
    setFormOpen(true)
  }
  function openEdit(id: number) {
    setEditId(id)
    setFormOpen(true)
  }
  async function handleDelete(e: EmployeeListItem) {
    if (!window.confirm(`ลบพนักงาน ${e.employeeCode} (${e.fullName})? ข้อมูลเอกสาร/การแจ้ง ปกส. จะถูกลบด้วย`)) return
    await del.mutateAsync(e.id)
  }

  const rows = useMemo(() => data ?? [], [data])

  const PAGE_SIZE = 10
  const [page, setPage] = useState(1)
  const totalPages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE))
  useEffect(() => { if (page > totalPages) setPage(1) }, [page, totalPages])
  useEffect(() => { setPage(1) }, [includeResigned])
  const paged = rows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-center justify-between gap-3 px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">พนักงานทั้งหมด</p>
          <p className="text-xs text-gray-500">{rows.length} คน · กรอกมือ (ข้อมูลส่วนบุคคล PDPA)</p>
        </div>
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-1.5 text-xs text-gray-600">
            <input type="checkbox" checked={includeResigned} onChange={(e) => setIncludeResigned(e.target.checked)} className="rounded" />
            แสดงผู้ลาออก
          </label>
          <Button type="button" onClick={openCreate}>+ เพิ่มพนักงาน</Button>
        </div>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีพนักงาน — กด “+ เพิ่มพนักงาน”</StateMessage></Card>
      )}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50 text-xs text-gray-600">
              <tr>
                <th className="px-4 py-2 text-left font-medium">รหัส</th>
                <th className="px-4 py-2 text-left font-medium">ชื่อ-สกุล</th>
                <th className="px-4 py-2 text-left font-medium w-32">ฝ่าย</th>
                <th className="px-4 py-2 text-left font-medium w-36">ตำแหน่ง</th>
                <th className="px-4 py-2 text-left font-medium w-28">วันเริ่มงาน</th>
                <th className="px-4 py-2 text-right font-medium w-28">เงินเดือน</th>
                <th className="px-4 py-2 text-center font-medium w-28">ปกส.</th>
                <th className="px-4 py-2 text-center font-medium w-20">สถานะ</th>
                <th className="px-4 py-2 text-right font-medium w-28">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((e) => (
                <tr key={e.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-1.5 font-mono text-xs text-slate-700">{e.employeeCode}</td>
                  <td className="px-4 py-1.5 text-gray-800">{e.fullName}</td>
                  <td className="px-4 py-1.5 text-gray-600">{e.department || '—'}</td>
                  <td className="px-4 py-1.5 text-gray-600">{e.position || '—'}</td>
                  <td className="px-4 py-1.5 text-gray-600">{e.startDate.slice(0, 10)}</td>
                  <td className="px-4 py-1.5 text-right font-mono text-slate-800">{fmt(e.baseSalary)}</td>
                  <td className="px-4 py-1.5 text-center">
                    <span className={`rounded-full px-2 py-0.5 text-xs ${SSO_STATUS_CLASS[e.ssoStatus]}`}>{SSO_STATUS_LABEL[e.ssoStatus]}</span>
                  </td>
                  <td className="px-4 py-1.5 text-center text-xs">
                    <span className={e.employmentStatus === 2 ? 'text-gray-400' : 'text-green-700'}>{EMPLOYMENT_STATUS_LABEL[e.employmentStatus]}</span>
                  </td>
                  <td className="px-4 py-1.5 text-right">
                    <div className="flex justify-end gap-1 whitespace-nowrap">
                      <Button type="button" variant="ghost" onClick={() => openEdit(e.id)} className="px-2 py-1 text-xs">แก้ไข</Button>
                      <Button type="button" variant="ghost" onClick={() => handleDelete(e)} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {rows.length > PAGE_SIZE && (
            <div className="px-4 pb-3">
              <Pagination page={page} totalPages={totalPages} totalCount={rows.length} onPageChange={setPage} />
            </div>
          )}
        </Card>
      )}

      {formOpen && (
        <EmployeeFormModal companyId={companyId} employeeId={editId} onClose={() => setFormOpen(false)} />
      )}
    </div>
  )
}
