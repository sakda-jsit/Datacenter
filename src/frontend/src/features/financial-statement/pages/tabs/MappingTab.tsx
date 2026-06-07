import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import type { ClientListDto } from '../../../clients/types/client.types'
import { useAccountMappings, useDeleteMapping, useUnmappedAccounts, useUpsertMapping } from '../../hooks/useFinancialStatement'

function fmtAmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

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
  const [checkYear, setCheckYear] = useState(new Date().getFullYear())

  const { data: mappings, isLoading } = useAccountMappings(clientId)
  const { data: check } = useUnmappedAccounts({ clientCompanyId: clientId, fiscalYear: checkYear })
  const upsert = useUpsertMapping()
  const del = useDeleteMapping()

  // คลิก "แมพ" จากรายการตกหล่น → เติมรหัส/ชื่อลงฟอร์มด้านล่าง (เหลือแค่เลือก REF + บันทึก)
  function prefillMap(code: string, name: string) {
    setEditCode(code); setEditName(name); setEditRef('')
    document.getElementById('mapping-form')?.scrollIntoView({ behavior: 'smooth', block: 'center' })
  }

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
          {/* ตรวจบัญชีตกหล่น (เตือนก่อนปิดงบ) */}
          <Card className="mb-4 p-4">
            <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
              <p className="text-sm font-semibold text-slate-700">ตรวจบัญชีตกหล่น (ก่อนปิดงบ)</p>
              <div className="flex items-center gap-2">
                <label className="text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
                <input
                  type="number" value={checkYear} min={2000} max={2100}
                  onChange={(e) => setCheckYear(Number(e.target.value))}
                  className="w-24 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
                />
              </div>
            </div>

            {!check && <StateMessage>กำลังตรวจ...</StateMessage>}
            {check && check.unmappedWithBalanceCount === 0 && (
              <div className="rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-700">
                ✓ ไม่มีบัญชีตกหล่น — บัญชีที่มียอดสิ้นปี {checkYear} ถูก map เข้างบครบ ({check.mappedCount} บัญชี)
              </div>
            )}
            {check && check.unmappedWithBalanceCount > 0 && (
              <div>
                <div className={`mb-2 rounded-lg border px-3 py-2 text-sm ${
                  Math.abs(check.totalNet) > 0.01
                    ? 'border-rose-200 bg-rose-50 text-rose-700'
                    : 'border-amber-200 bg-amber-50 text-amber-700'
                }`}>
                  {Math.abs(check.totalNet) > 0.01 ? (
                    <>⚠ พบ <b>{check.unmappedWithBalanceCount}</b> บัญชีมียอดแต่ยังไม่ map → <b>งบจะไม่สมดุล {fmtAmt(check.totalNet)}</b> บาท (map แล้ว {check.mappedCount})</>
                  ) : (
                    <>พบ <b>{check.unmappedWithBalanceCount}</b> บัญชียังไม่ map (ยอดสุทธิ 0 — งบยังไม่สมดุลผิด แต่บัญชีเหล่านี้จะตกหล่นจากงบ; map แล้วเพียง {check.mappedCount})</>
                  )}
                </div>
                <div className="max-h-72 overflow-y-auto rounded border border-gray-100">
                  <table className="w-full text-sm">
                    <thead className="sticky top-0 bg-slate-50 text-xs text-gray-600">
                      <tr>
                        <th className="px-3 py-2 text-left font-medium w-28">รหัสบัญชี</th>
                        <th className="px-3 py-2 text-left font-medium">ชื่อบัญชี</th>
                        <th className="px-3 py-2 text-right font-medium w-36">ยอดสิ้นปี</th>
                        <th className="px-3 py-2 w-20"></th>
                      </tr>
                    </thead>
                    <tbody>
                      {check.items.map((a) => (
                        <tr key={a.accountCode} className="border-b border-gray-100 hover:bg-amber-50/40">
                          <td className="px-3 py-2 font-mono text-gray-600">{a.accountCode}</td>
                          <td className="px-3 py-2 text-gray-700">{a.accountName || '—'}</td>
                          <td className="px-3 py-2 text-right font-mono text-gray-800">{fmtAmt(a.netBalance)}</td>
                          <td className="px-3 py-2 text-right">
                            <Button type="button" variant="ghost" onClick={() => prefillMap(a.accountCode, a.accountName)} className="px-2 py-1 text-xs text-sky-600">แมพ →</Button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </Card>

          {/* Add mapping form */}
          <Card id="mapping-form" className="mb-4 p-4">
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
