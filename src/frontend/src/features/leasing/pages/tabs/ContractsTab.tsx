import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import LeaseContractFormModal from '../../components/LeaseContractFormModal'
import ScheduleModal from '../../components/ScheduleModal'
import { useDeleteLeaseContract, useLeaseContracts } from '../../hooks/useLeasing'
import { CONTRACT_TYPE_LABEL } from '../../types/leasing.types'
import type { LeaseContractListItem } from '../../types/leasing.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function ContractsTab({ companyId, fiscalYear }: Props) {
  const { data: contracts, isLoading, isError } = useLeaseContracts(companyId)
  const del = useDeleteLeaseContract(companyId)
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [scheduleId, setScheduleId] = useState<number | null>(null)

  if (!companyId) {
    return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  }

  function openCreate() {
    setEditingId(null)
    setFormOpen(true)
  }

  function openEdit(id: number) {
    setEditingId(id)
    setFormOpen(true)
  }

  async function handleDelete(c: LeaseContractListItem) {
    if (!window.confirm(`ลบสัญญา ${c.contractNo} (${c.assetName})? (บันทึก audit trail)`)) return
    await del.mutateAsync(c.id)
  }

  return (
    <div>
      <Card className="mb-4 flex items-center justify-between px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">สัญญาทั้งหมด</p>
          <p className="text-xs text-gray-500">{contracts?.length ?? 0} สัญญา</p>
        </div>
        <Button type="button" onClick={openCreate}>+ สร้างสัญญา</Button>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {contracts && contracts.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีสัญญา — กด "สร้างสัญญา"</StateMessage></Card>
      )}

      {contracts && contracts.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50">
              <tr className="text-xs text-gray-600">
                <th className="px-4 py-3 text-left font-medium">เลขที่สัญญา</th>
                <th className="px-4 py-3 text-left font-medium">ทรัพย์สิน / รายการ</th>
                <th className="px-4 py-3 text-left font-medium w-28">ประเภท</th>
                <th className="px-4 py-3 text-left font-medium w-28">งวดแรก</th>
                <th className="px-4 py-3 text-right font-medium w-20">งวด</th>
                <th className="px-4 py-3 text-right font-medium w-32">เงินต้น</th>
                <th className="px-4 py-3 text-right font-medium w-28">ค่างวด</th>
                <th className="px-4 py-3 text-right font-medium w-40">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {contracts.map((c) => (
                <tr key={c.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-2.5 font-mono text-xs text-slate-700">
                    {c.contractNo}
                    {!c.isActive && <span className="ml-1 text-[10px] text-gray-400">(ปิด)</span>}
                  </td>
                  <td className="px-4 py-2.5 text-gray-800">
                    {c.assetName}
                    {c.lessor && <span className="ml-2 text-xs text-gray-400">{c.lessor}</span>}
                  </td>
                  <td className="px-4 py-2.5">
                    <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600">
                      {CONTRACT_TYPE_LABEL[c.contractType]}
                    </span>
                  </td>
                  <td className="px-4 py-2.5 text-gray-600">{c.firstInstallmentDate.slice(0, 10)}</td>
                  <td className="px-4 py-2.5 text-right text-gray-600">{c.numberOfPeriods}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-slate-800">{fmt(c.financedPrincipal)}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-slate-800">{fmt(c.installmentAmount)}</td>
                  <td className="px-4 py-2.5 text-right">
                    <Button type="button" variant="ghost" onClick={() => setScheduleId(c.id)} className="px-2 py-1 text-xs text-sky-600">ตาราง</Button>
                    <Button type="button" variant="ghost" onClick={() => openEdit(c.id)} className="px-2 py-1 text-xs">แก้ไข</Button>
                    <Button type="button" variant="ghost" onClick={() => handleDelete(c)} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {formOpen && (
        <LeaseContractFormModal
          companyId={companyId}
          fiscalYear={fiscalYear}
          editingId={editingId}
          onClose={() => setFormOpen(false)}
        />
      )}
      {scheduleId !== null && (
        <ScheduleModal
          companyId={companyId}
          fiscalYear={fiscalYear}
          contractId={scheduleId}
          onClose={() => setScheduleId(null)}
        />
      )}
    </div>
  )
}
