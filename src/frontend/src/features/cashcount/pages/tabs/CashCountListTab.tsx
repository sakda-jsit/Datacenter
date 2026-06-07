import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import CashCountFormModal from '../../components/CashCountFormModal'
import { useCashCountList, useDeleteCashCount } from '../../hooks/useCashCount'
import type { CashCountListItem } from '../../types/cashcount.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function CashCountListTab({ companyId, fiscalYear }: Props) {
  const { data: items, isLoading, isError } = useCashCountList(companyId, fiscalYear)
  const del = useDeleteCashCount(companyId)
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  async function handleDelete(c: CashCountListItem) {
    if (!window.confirm(`ลบใบตรวจนับวันที่ ${c.countDate.slice(0, 10)}? (บันทึก audit trail)`)) return
    await del.mutateAsync(c.id)
  }

  return (
    <div>
      <Card className="mb-4 flex items-center justify-between px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">ใบตรวจนับเงินสด ปี {fiscalYear}</p>
          <p className="text-xs text-gray-500">{items?.length ?? 0} ใบ</p>
        </div>
        <Button type="button" onClick={() => { setEditingId(null); setFormOpen(true) }}>+ เพิ่มใบตรวจนับ</Button>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {items && items.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีใบตรวจนับปีนี้ — กด "เพิ่มใบตรวจนับ"</StateMessage></Card>
      )}

      {items && items.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50">
              <tr className="text-xs text-gray-600">
                <th className="px-4 py-3 text-left font-medium w-32">วันที่นับ</th>
                <th className="px-4 py-3 text-left font-medium">จุดเก็บ/อ้างอิง</th>
                <th className="px-4 py-3 text-left font-medium w-40">บัญชีเงินสด</th>
                <th className="px-4 py-3 text-right font-medium w-36">นับได้รวม</th>
                <th className="px-4 py-3 text-right font-medium w-32">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {items.map((c) => (
                <tr key={c.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-2.5 text-gray-700">{c.countDate.slice(0, 10)}</td>
                  <td className="px-4 py-2.5 text-gray-800">
                    {c.reference}
                    {!c.isActive && <span className="ml-1 text-[10px] text-gray-400">(ปิด)</span>}
                  </td>
                  <td className="px-4 py-2.5 font-mono text-xs text-gray-500">{c.cashAccountCode}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-slate-800">{fmt(c.countedTotal)}</td>
                  <td className="px-4 py-2.5 text-right">
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
        <CashCountFormModal companyId={companyId} fiscalYear={fiscalYear} editingId={editingId} onClose={() => setFormOpen(false)} />
      )}
    </div>
  )
}
