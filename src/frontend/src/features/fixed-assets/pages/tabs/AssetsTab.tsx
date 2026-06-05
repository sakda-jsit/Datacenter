import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import FixedAssetFormModal from '../../components/FixedAssetFormModal'
import DepreciationScheduleModal from '../../components/DepreciationScheduleModal'
import AccountMappingModal from '../../components/AccountMappingModal'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import { useDeleteFixedAsset, useFixedAssets } from '../../hooks/useFixedAssets'
import { AssetStatus, STATUS_LABEL } from '../../types/fixedAsset.types'
import type { FixedAssetListItem } from '../../types/fixedAsset.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function statusClass(status: number) {
  if (status === AssetStatus.Active) return 'bg-green-50 text-green-700'
  if (status === AssetStatus.Sold) return 'bg-sky-50 text-sky-700'
  return 'bg-slate-100 text-slate-600'
}

interface Props {
  companyId: number
  fiscalYear: number
}

export default function AssetsTab({ companyId, fiscalYear }: Props) {
  const { data: assets, isLoading, isError } = useFixedAssets(companyId)
  const del = useDeleteFixedAsset(companyId)
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [scheduleId, setScheduleId] = useState<number | null>(null)
  const [mappingOpen, setMappingOpen] = useState(false)

  if (!companyId) {
    return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  }

  function openEdit(id: number) {
    setEditingId(id)
    setFormOpen(true)
  }

  async function handleDelete(a: FixedAssetListItem) {
    if (!window.confirm(`ลบสินทรัพย์ ${a.assetCode} (${a.assetName})? (บันทึก audit trail)`)) return
    await del.mutateAsync(a.id)
  }

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-center justify-between gap-3 px-6 py-4">
        <div>
          <p className="text-sm font-semibold text-slate-800">สินทรัพย์ทั้งหมด</p>
          <p className="text-xs text-gray-500">
            {assets?.length ?? 0} รายการ · ดึงจาก Express (FAMAS) 100% ที่เมนู “นำเข้าข้อมูล” — เฟสนี้ไม่สร้างสินทรัพย์เองในระบบ
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <ExportMenu
            meta={{ title: 'ทะเบียนสินทรัพย์ถาวร', fileName: `fixed-assets-${companyId}` }}
            getSections={(): ExportSection[] => [{
              name: 'ทะเบียนสินทรัพย์',
              columns: [
                { key: 'assetCode', header: 'รหัส' },
                { key: 'assetName', header: 'สินทรัพย์' },
                { key: 'type', header: 'ประเภท', value: (a) => a.assetTypeName ?? a.categoryCode ?? '' },
                { key: 'acquireDate', header: 'วันที่ได้มา', value: (a) => a.acquireDate.slice(0, 10) },
                { key: 'cost', header: 'ราคาทุน', align: 'right' },
                { key: 'bookRatePct', header: 'อัตรา%', align: 'right' },
                { key: 'status', header: 'สถานะ', value: (a) => STATUS_LABEL[a.status] },
              ],
              rows: assets ?? [],
            }]}
            disabled={!assets || assets.length === 0}
          />
          <Button type="button" variant="secondary" onClick={() => setMappingOpen(true)}>แมพบัญชี</Button>
        </div>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {assets && assets.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีสินทรัพย์ — นำเข้าจาก Express ที่เมนู “นำเข้าข้อมูล”</StateMessage></Card>
      )}

      {assets && assets.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50">
              <tr className="text-xs text-gray-600">
                <th className="px-4 py-3 text-left font-medium">รหัส</th>
                <th className="px-4 py-3 text-left font-medium">สินทรัพย์</th>
                <th className="px-4 py-3 text-left font-medium w-44">ประเภท</th>
                <th className="px-4 py-3 text-left font-medium w-28">วันที่ได้มา</th>
                <th className="px-4 py-3 text-right font-medium w-32">ราคาทุน</th>
                <th className="px-4 py-3 text-right font-medium w-20">อัตรา</th>
                <th className="px-4 py-3 text-center font-medium w-24">สถานะ</th>
                <th className="px-4 py-3 text-right font-medium w-44">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {assets.map((a) => (
                <tr key={a.id} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-2.5 font-mono text-xs text-slate-700">
                    {a.assetCode}
                    {!a.isActive && <span className="ml-1 text-[10px] text-gray-400">(ปิด)</span>}
                  </td>
                  <td className="px-4 py-2.5 text-gray-800">{a.assetName}</td>
                  <td className="px-4 py-2.5 text-gray-600">{a.assetTypeName ?? a.categoryCode ?? '—'}</td>
                  <td className="px-4 py-2.5 text-gray-600">{a.acquireDate.slice(0, 10)}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-slate-800">{fmt(a.cost)}</td>
                  <td className="px-4 py-2.5 text-right text-gray-600">{a.bookRatePct}%</td>
                  <td className="px-4 py-2.5 text-center">
                    <span className={`rounded-full px-2 py-0.5 text-xs ${statusClass(a.status)}`}>{STATUS_LABEL[a.status]}</span>
                  </td>
                  <td className="px-4 py-2.5 text-right">
                    <Button type="button" variant="ghost" onClick={() => setScheduleId(a.id)} className="px-2 py-1 text-xs text-sky-600">ค่าเสื่อม</Button>
                    <Button type="button" variant="ghost" onClick={() => openEdit(a.id)} className="px-2 py-1 text-xs">แก้ไข</Button>
                    <Button type="button" variant="ghost" onClick={() => handleDelete(a)} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {formOpen && (
        <FixedAssetFormModal
          companyId={companyId}
          fiscalYear={fiscalYear}
          editingId={editingId}
          onClose={() => setFormOpen(false)}
        />
      )}
      {scheduleId !== null && (
        <DepreciationScheduleModal
          companyId={companyId}
          fiscalYear={fiscalYear}
          assetId={scheduleId}
          onClose={() => setScheduleId(null)}
        />
      )}
      {mappingOpen && (
        <AccountMappingModal companyId={companyId} onClose={() => setMappingOpen(false)} />
      )}
    </div>
  )
}
