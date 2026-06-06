import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import PrepaidFormModal from '../../components/PrepaidFormModal'
import PrepaidScheduleModal from '../../components/PrepaidScheduleModal'
import { useDeletePrepaid, usePrepaidList } from '../../hooks/usePrepaid'
import type { PrepaidListItem } from '../../types/prepaid.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function PrepaidListTab({ companyId, fiscalYear }: Props) {
  const { data: items, isLoading, isError } = usePrepaidList(companyId)
  const del = useDeletePrepaid(companyId)
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [scheduleId, setScheduleId] = useState<number | null>(null)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  async function handleDelete(c: PrepaidListItem) {
    if (!window.confirm(`ลบ "${c.name}"? (บันทึก audit trail)`)) return
    await del.mutateAsync(c.id)
  }

  return (
    <div>
      <Card className="mb-4 flex items-center justify-between px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">ค่าใช้จ่ายจ่ายล่วงหน้าทั้งหมด</p>
          <p className="text-xs text-gray-500">{items?.length ?? 0} รายการ</p>
        </div>
        <Button type="button" onClick={() => { setEditingId(null); setFormOpen(true) }}>+ เพิ่มรายการ</Button>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {items && items.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีรายการ — กด "เพิ่มรายการ"</StateMessage></Card>
      )}

      {items && items.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50">
              <tr className="text-xs text-gray-600">
                <th className="px-4 py-3 text-left font-medium">รายละเอียด</th>
                <th className="px-4 py-3 text-left font-medium w-28">อ้างอิง</th>
                <th className="px-4 py-3 text-right font-medium w-32">มูลค่าตั้งต้น</th>
                <th className="px-4 py-3 text-left font-medium w-28">วันเริ่ม</th>
                <th className="px-4 py-3 text-left font-medium w-28">วันสิ้นสุด</th>
                <th className="px-4 py-3 text-right font-medium w-44">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {items.map((c) => (
                <tr key={c.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-2.5 text-gray-800">
                    {c.code && <span className="mr-2 font-mono text-xs text-gray-400">{c.code}</span>}
                    {c.name}
                    {!c.isActive && <span className="ml-1 text-[10px] text-gray-400">(ปิด)</span>}
                  </td>
                  <td className="px-4 py-2.5 text-xs text-gray-500">{c.reference}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-slate-800">{fmt(c.totalAmount)}</td>
                  <td className="px-4 py-2.5 text-gray-600">{c.startDate.slice(0, 10)}</td>
                  <td className="px-4 py-2.5 text-gray-600">{c.endDate.slice(0, 10)}</td>
                  <td className="px-4 py-2.5 text-right">
                    <Button type="button" variant="ghost" onClick={() => setScheduleId(c.id)} className="px-2 py-1 text-xs text-sky-600">ตาราง</Button>
                    <Button type="button" variant="ghost" onClick={() => { setEditingId(c.id); setFormOpen(true) }} className="px-2 py-1 text-xs">แก้ไข</Button>
                    <Button type="button" variant="ghost" onClick={() => handleDelete(c)} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {formOpen && (
        <PrepaidFormModal companyId={companyId} fiscalYear={fiscalYear} editingId={editingId} onClose={() => setFormOpen(false)} />
      )}
      {scheduleId !== null && (
        <PrepaidScheduleModal companyId={companyId} fiscalYear={fiscalYear} prepaidId={scheduleId} onClose={() => setScheduleId(null)} />
      )}
    </div>
  )
}
