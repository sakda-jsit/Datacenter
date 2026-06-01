import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import type { ClientListDto } from '../../../clients/types/client.types'
import { useAccountMappings, useDeleteMapping, useUpsertMapping } from '../../hooks/useFinancialStatement'

// All 23 REF codes
const REF_CODES = [
  { code: 'A1', label: 'เงินสดและรายการเทียบเท่าเงินสด', section: 'A' },
  { code: 'A2', label: 'เงินลงทุนระยะสั้น', section: 'A' },
  { code: 'A7', label: 'ลูกหนี้การค้าและลูกหนี้หมุนเวียนอื่น', section: 'A' },
  { code: 'A8', label: 'ลูกหนี้เงินให้กู้ยืม', section: 'A' },
  { code: 'A3', label: 'สินค้าคงเหลือ', section: 'A' },
  { code: 'A4', label: 'สินทรัพย์หมุนเวียนอื่น', section: 'A' },
  { code: 'A9', label: 'เงินลงทุนระยะยาว', section: 'A' },
  { code: 'A5', label: 'ที่ดิน อาคารและอุปกรณ์', section: 'A' },
  { code: 'A10', label: 'สินทรัพย์ไม่มีตัวตน', section: 'A' },
  { code: 'A6', label: 'สินทรัพย์ไม่หมุนเวียนอื่น', section: 'A' },
  { code: 'L1', label: 'เจ้าหนี้การค้าและเจ้าหนี้หมุนเวียนอื่น', section: 'L' },
  { code: 'L5', label: 'หนี้สินสัญญาเช่า (ส่วนหมุนเวียน)', section: 'L' },
  { code: 'L3', label: 'เงินกู้ยืมระยะสั้น', section: 'L' },
  { code: 'L2', label: 'หนี้สินหมุนเวียนอื่น', section: 'L' },
  { code: 'L6', label: 'เงินกู้ยืมระยะยาว', section: 'L' },
  { code: 'L4', label: 'หนี้สินตามสัญญาเช่า (ระยะยาว)', section: 'L' },
  { code: 'C1', label: 'ทุนที่ออกและชำระแล้ว', section: 'E' },
  { code: 'RE', label: 'กำไร (ขาดทุน) สะสม', section: 'E' },
  { code: 'I1', label: 'รายได้จากการขาย', section: 'I' },
  { code: 'I2', label: 'รายได้จากการให้บริการ', section: 'I' },
  { code: 'I3', label: 'รายได้ดอกเบี้ย', section: 'I' },
  { code: 'I4', label: 'รายได้อื่น', section: 'I' },
  { code: 'C', label: 'ต้นทุนขาย / ต้นทุนบริการ', section: 'X' },
  { code: 'X1', label: 'ค่าใช้จ่ายในการขาย', section: 'X' },
  { code: 'X2', label: 'ค่าใช้จ่ายในการบริหาร', section: 'X' },
  { code: 'X3', label: 'ต้นทุนทางการเงิน', section: 'X' },
  { code: 'X4', label: 'ภาษีเงินได้ (จากภายนอก)', section: 'X' },
]

const SECTION_LABEL: Record<string, string> = {
  A: 'สินทรัพย์',
  L: 'หนี้สิน',
  E: 'ส่วนของเจ้าของ',
  I: 'รายได้',
  X: 'ค่าใช้จ่าย',
}

interface Props {
  clientId: number
  clients: ClientListDto[]
  onClientChange: (id: number) => void
}

export default function MappingTab({ clientId, clients }: Props) {
  const [editCode, setEditCode] = useState('')
  const [editName, setEditName] = useState('')
  const [editRef, setEditRef] = useState('')

  const { data: mappings, isLoading } = useAccountMappings(clientId)
  const upsert = useUpsertMapping()
  const del = useDeleteMapping()

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    if (!clientId || !editCode || !editRef) return
    await upsert.mutateAsync({ clientCompanyId: clientId, accountCode: editCode, accountName: editName, refCode: editRef })
    setEditCode(''); setEditName(''); setEditRef('')
  }

  const byRef = (mappings ?? []).reduce<Record<string, typeof mappings>>((acc, m) => {
    if (!acc[m!.refCode]) acc[m!.refCode] = []
    acc[m!.refCode]!.push(m)
    return acc
  }, {})

  return (
    <div>
      {/* Company picker */}
      <Card className="mb-4 flex items-center gap-4 p-4">
        <div>
          <p className="text-xs font-medium text-gray-500">บริษัทลูกค้า</p>
          <p className="text-sm font-semibold text-slate-800">
            {clients.find((client) => client.id === clientId)?.name ?? (clientId ? `Company #${clientId}` : 'เลือกบริษัทที่ header')}
          </p>
        </div>
        {mappings && (
          <span className="text-sm text-gray-500">{mappings.length} บัญชีที่ map แล้ว</span>
        )}
      </Card>

      {clientId > 0 && (
        <>
          {/* Add mapping form */}
          <Card className="mb-4 p-4">
            <p className="text-sm font-semibold text-slate-700 mb-3">เพิ่ม / แก้ไข Mapping</p>
            <form onSubmit={handleSave} className="flex flex-wrap gap-3 items-end">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">รหัสบัญชี *</label>
                <input
                  value={editCode} onChange={(e) => setEditCode(e.target.value)} required
                  placeholder="เช่น 11110"
                  className="border border-gray-300 rounded px-3 py-2 text-sm w-32 focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">ชื่อบัญชี</label>
                <input
                  value={editName} onChange={(e) => setEditName(e.target.value)}
                  placeholder="ชื่อบัญชี"
                  className="border border-gray-300 rounded px-3 py-2 text-sm w-56 focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">REF Code *</label>
                <select
                  value={editRef} onChange={(e) => setEditRef(e.target.value)} required
                  className="border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                >
                  <option value="">-- เลือก --</option>
                  {Object.entries(
                    REF_CODES.reduce<Record<string, typeof REF_CODES>>((acc, r) => {
                      if (!acc[r.section]) acc[r.section] = []
                      acc[r.section].push(r)
                      return acc
                    }, {})
                  ).map(([sec, codes]) => (
                    <optgroup key={sec} label={SECTION_LABEL[sec]}>
                      {codes.map(c => (
                        <option key={c.code} value={c.code}>{c.code} — {c.label}</option>
                      ))}
                    </optgroup>
                  ))}
                </select>
              </div>
              <Button
                type="submit" disabled={upsert.isPending}
              >
                บันทึก
              </Button>
            </form>
          </Card>

          {/* Mapping table grouped by REF code */}
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
          {mappings && (
            <Card className="overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 border-b">
                  <tr>
                    <th className="text-left px-4 py-3 font-medium text-gray-600 w-24">REF</th>
                    <th className="text-left px-4 py-3 font-medium text-gray-600">ชื่อบรรทัดในงบ</th>
                    <th className="text-left px-4 py-3 font-medium text-gray-600 w-28">รหัสบัญชี</th>
                    <th className="text-left px-4 py-3 font-medium text-gray-600">ชื่อบัญชี</th>
                    <th className="px-4 py-3 w-16"></th>
                  </tr>
                </thead>
                <tbody>
                  {REF_CODES.map((ref) => {
                    const rows = byRef[ref.code] ?? []
                    if (rows.length === 0) return null
                    return rows.map((m, i) => (
                      <tr key={m!.accountCode} className="border-b border-gray-100 hover:bg-gray-50">
                        {i === 0 && (
                          <td rowSpan={rows.length}
                            className="px-4 py-2 font-mono font-medium text-slate-600 align-top">
                            {ref.code}
                          </td>
                        )}
                        {i === 0 && (
                          <td rowSpan={rows.length}
                            className="px-4 py-2 text-gray-700 text-xs align-top">
                            {m!.lineName}
                          </td>
                        )}
                        <td className="px-4 py-2 font-mono text-gray-600">{m!.accountCode}</td>
                        <td className="px-4 py-2 text-gray-700">{m!.accountName}</td>
                        <td className="px-4 py-2 text-center">
                          <Button
                            type="button"
                            variant="ghost"
                            onClick={() => del.mutate({ clientCompanyId: clientId, accountCode: m!.accountCode })}
                            className="px-2 py-1 text-xs text-red-500 hover:text-red-600"
                          >
                            ลบ
                          </Button>
                        </td>
                      </tr>
                    ))
                  })}
                  {mappings.length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-4 py-8">
                        <StateMessage centered>ยังไม่มี Mapping — เพิ่มโดยใช้ฟอร์มด้านบน</StateMessage>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </Card>
          )}
        </>
      )}

      {clientId === 0 && (
        <Card>
          <StateMessage centered>เลือกบริษัทลูกค้าเพื่อจัดการ Mapping</StateMessage>
        </Card>
      )}
    </div>
  )
}
