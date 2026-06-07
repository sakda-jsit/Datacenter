import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import InterestLoanFormModal from '../../components/InterestLoanFormModal'
import InterestScheduleModal from '../../components/InterestScheduleModal'
import { useDeleteInterestLoan, useInterestLoanList } from '../../hooks/useInterestIncome'
import type { InterestLoanListItem } from '../../types/interestincome.types'

interface Props {
  companyId: number
  fiscalYear: number
}

export default function InterestIncomeListTab({ companyId, fiscalYear }: Props) {
  const { data: items, isLoading, isError } = useInterestLoanList(companyId)
  const del = useDeleteInterestLoan(companyId)
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [scheduleId, setScheduleId] = useState<number | null>(null)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  async function handleDelete(c: InterestLoanListItem) {
    if (!window.confirm(`ลบ "${c.name}"? (บันทึก audit trail)`)) return
    await del.mutateAsync(c.id)
  }

  return (
    <div>
      <Card className="mb-4 flex items-center justify-between px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">เงินให้กู้ที่คิดดอกเบี้ย</p>
          <p className="text-xs text-gray-500">{items?.length ?? 0} รายการ</p>
        </div>
        <Button type="button" onClick={() => { setEditingId(null); setFormOpen(true) }}>+ เพิ่มเงินให้กู้</Button>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {items && items.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีรายการ — กด "เพิ่มเงินให้กู้"</StateMessage></Card>
      )}

      {items && items.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50">
              <tr className="text-xs text-gray-600">
                <th className="px-4 py-3 text-left font-medium">ชื่อ/ผู้กู้</th>
                <th className="px-4 py-3 text-left font-medium w-28">อ้างอิง</th>
                <th className="px-4 py-3 text-right font-medium w-28">อัตรา/ปี</th>
                <th className="px-4 py-3 text-right font-medium w-52">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {items.map((c) => (
                <tr key={c.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-2.5 text-gray-800">
                    {c.name}
                    {!c.isActive && <span className="ml-1 text-[10px] text-gray-400">(ปิด)</span>}
                  </td>
                  <td className="px-4 py-2.5 text-xs text-gray-500">{c.reference}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-slate-800">{c.annualRatePct.toLocaleString('th-TH', { minimumFractionDigits: 2 })}%</td>
                  <td className="px-4 py-2.5 text-right">
                    <Button type="button" variant="ghost" onClick={() => setScheduleId(c.id)} className="px-2 py-1 text-xs text-sky-600">ช่วงดอกเบี้ย</Button>
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
        <InterestLoanFormModal companyId={companyId} fiscalYear={fiscalYear} editingId={editingId} onClose={() => setFormOpen(false)} />
      )}
      {scheduleId !== null && (
        <InterestScheduleModal companyId={companyId} fiscalYear={fiscalYear} loanId={scheduleId} onClose={() => setScheduleId(null)} />
      )}
    </div>
  )
}
