import { Fragment, useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import AdjustmentFormModal from '../../components/AdjustmentFormModal'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useAdjustmentEntries, useDeleteAdjustment } from '../../hooks/useAdjustments'
import { SOURCE_TYPE_LABEL } from '../../types/adjustment.types'
import type { AdjustmentEntryDto } from '../../types/adjustment.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function AdjustmentEntriesTab({ companyId, fiscalYear }: Props) {
  const { data: entries, isLoading, isError } = useAdjustmentEntries(companyId, fiscalYear)
  const del = useDeleteAdjustment(companyId, fiscalYear)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<AdjustmentEntryDto | null>(null)
  const [expanded, setExpanded] = useState<number | null>(null)

  if (!companyId) {
    return (
      <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
    )
  }

  function openCreate() {
    setEditing(null)
    setModalOpen(true)
  }

  function openEdit(entry: AdjustmentEntryDto) {
    setEditing(entry)
    setModalOpen(true)
  }

  async function handleDelete(entry: AdjustmentEntryDto) {
    if (!window.confirm(`ลบรายการปรับปรุง ${entry.documentNo}? (บันทึก audit trail)`)) return
    await del.mutateAsync(entry.id)
  }

  return (
    <div>
      <Card className="mb-4 flex items-center justify-between px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">รายการปรับปรุง ปีบัญชี {fiscalYear}</p>
          <p className="text-xs text-gray-500">{entries?.length ?? 0} รายการ</p>
        </div>
        <div className="flex items-center gap-2">
          {entries && entries.length > 0 && (
            <ExportMenu
              meta={{ title: `รายการปรับปรุงปิดงบ ปี ${fiscalYear}`, fileName: `adjustment-entries-${companyId}-${fiscalYear}` }}
              getSections={(): ExportSection[] => [{
                name: 'รายการปรับปรุง',
                columns: [
                  { key: 'documentNo', header: 'เลขที่' },
                  { key: 'entryDate', header: 'วันที่', value: (e) => e.entryDate.slice(0, 10) },
                  { key: 'sourceType', header: 'ที่มา', value: (e) => SOURCE_TYPE_LABEL[e.sourceType] ?? '' },
                  { key: 'reason', header: 'เหตุผล' },
                  { key: 'reference', header: 'อ้างอิง', value: (e) => e.reference ?? '' },
                  { key: 'totalDebit', header: 'จำนวนเงิน', align: 'right' },
                ],
                rows: entries,
              }]}
            />
          )}
          <Button type="button" onClick={openCreate}>+ สร้างรายการปรับปรุง</Button>
        </div>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {entries && entries.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีรายการปรับปรุง — กด "สร้างรายการปรับปรุง"</StateMessage></Card>
      )}

      {entries && entries.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 border-b">
              <tr className="text-xs text-gray-600">
                <th className="px-4 py-3 text-left font-medium w-36">เลขที่</th>
                <th className="px-4 py-3 text-left font-medium w-28">วันที่</th>
                <th className="px-4 py-3 text-left font-medium w-24">ที่มา</th>
                <th className="px-4 py-3 text-left font-medium">เหตุผล</th>
                <th className="px-4 py-3 text-right font-medium w-32">จำนวนเงิน</th>
                <th className="px-4 py-3 text-right font-medium w-32">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((entry) => (
                <Fragment key={entry.id}>
                  <tr className="border-b border-gray-100 hover:bg-slate-50">
                    <td className="px-4 py-2.5">
                      <button
                        type="button"
                        onClick={() => setExpanded((v) => (v === entry.id ? null : entry.id))}
                        className="font-mono text-xs text-sky-700 hover:underline"
                      >
                        {expanded === entry.id ? '▼' : '▶'} {entry.documentNo}
                      </button>
                    </td>
                    <td className="px-4 py-2.5 text-gray-600">{entry.entryDate.slice(0, 10)}</td>
                    <td className="px-4 py-2.5">
                      <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600">
                        {SOURCE_TYPE_LABEL[entry.sourceType]}
                      </span>
                    </td>
                    <td className="px-4 py-2.5 text-gray-800">
                      {entry.reason}
                      {entry.reference && <span className="ml-2 text-xs text-gray-400">({entry.reference})</span>}
                    </td>
                    <td className="px-4 py-2.5 text-right font-mono text-slate-800">{fmt(entry.totalDebit)}</td>
                    <td className="px-4 py-2.5 text-right">
                      <Button type="button" variant="ghost" onClick={() => openEdit(entry)} className="px-2 py-1 text-xs">แก้ไข</Button>
                      <Button type="button" variant="ghost" onClick={() => handleDelete(entry)} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                    </td>
                  </tr>
                  {expanded === entry.id && (
                    <tr className="bg-slate-50/60">
                      <td colSpan={6} className="px-4 py-3">
                        <table className="w-full text-xs">
                          <thead className="text-gray-500">
                            <tr>
                              <th className="px-3 py-1 text-left font-medium">บัญชี</th>
                              <th className="px-3 py-1 text-left font-medium">คำอธิบาย</th>
                              <th className="px-3 py-1 text-right font-medium w-28">เดบิต</th>
                              <th className="px-3 py-1 text-right font-medium w-28">เครดิต</th>
                            </tr>
                          </thead>
                          <tbody>
                            {entry.lines.map((l) => (
                              <tr key={l.id} className="border-t border-gray-100">
                                <td className="px-3 py-1.5 text-gray-700">
                                  <span className="font-mono text-gray-400">{l.accountCode}</span> {l.accountName}
                                </td>
                                <td className="px-3 py-1.5 text-gray-500">{l.description ?? '—'}</td>
                                <td className="px-3 py-1.5 text-right font-mono">{l.debitAmount ? fmt(l.debitAmount) : '—'}</td>
                                <td className="px-3 py-1.5 text-right font-mono">{l.creditAmount ? fmt(l.creditAmount) : '—'}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {modalOpen && (
        <AdjustmentFormModal
          companyId={companyId}
          fiscalYear={fiscalYear}
          editing={editing}
          onClose={() => setModalOpen(false)}
        />
      )}
    </div>
  )
}
