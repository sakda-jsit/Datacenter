import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useCit50BsMapping, useCit50Mapping, useSaveCit50Mapping } from '../hooks/useCorporateTax'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

type TabDef = {
  key: string
  label: string
  sched: number | null // null = งบดุล (BS)
  scope: string
  defaultLabel: string
  help: string
}

const TABS: TabDef[] = [
  {
    key: 'r4', label: 'รายการ 4 (ต้นทุนผลิต)', sched: 4, scope: 'R4_',
    defaultLabel: '— ไม่ลงรายการนี้ —',
    help: 'แมพบัญชีต้นทุนการผลิต → บรรทัดในรายการที่ 4 — บัญชีวัตถุดิบ/งานระหว่างทำใช้สำหรับยอดต้น/ปลายงวด, ' +
      'บัญชีซื้อ→ต้นทุนวัตถุดิบใช้ไป, เงินเดือนฝ่ายผลิต/ค่าเสื่อมการผลิต ฯลฯ. บัญชีที่ไม่เลือกจะไม่เข้ารายการ 4. ' +
      'ยอดซื้อ/ผลรวม/ต้นทุนผลิต คำนวณให้อัตโนมัติ',
  },
  {
    key: 'r8', label: 'รายการ 7/8 (ขาย/บริหาร)', sched: 8, scope: 'R8',
    defaultLabel: '— รายจ่ายอื่น (ค่าเริ่มต้น) —',
    help: 'แมพบัญชีค่าใช้จ่ายขายและบริหาร → บรรทัดรายการที่ 8. บัญชีที่ไม่เลือกจะลง "รายจ่ายอื่น" อัตโนมัติ ' +
      '(บัญชีที่แมพไปรายการ 4/6 จะไม่แสดงที่นี่)',
  },
  {
    key: 'r5', label: 'รายการ 5 (รายได้อื่น)', sched: 5, scope: 'R5_',
    defaultLabel: '— รายได้อื่น (ค่าเริ่มต้น) —',
    help: 'แมพบัญชีรายได้อื่น (นอกจากรายได้หลัก) → บรรทัดรายการที่ 5',
  },
  {
    key: 'r6', label: 'รายการ 6 (รายจ่ายอื่น)', sched: 6, scope: 'R6_',
    defaultLabel: '— รายจ่ายอื่น (ค่าเริ่มต้น) —',
    help: 'แมพรายจ่ายอื่น/ต้นทุนทางการเงิน → บรรทัดรายการที่ 6',
  },
  {
    key: 'bs', label: 'งบดุล (รายการ 9)', sched: null, scope: 'BS_',
    defaultLabel: '— ตามผังบัญชี (ค่าเริ่มต้น) —',
    help: 'แมพบัญชีสินทรัพย์/หนี้สิน → บรรทัดงบดุลรายการที่ 9 (เช่น แยกที่ดิน+อาคารจากทรัพย์สินอื่น). ' +
      'ยอดรวมไม่เปลี่ยน เปลี่ยนแค่บรรทัดที่ลง',
  },
]

export default function Cit50MappingPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [year, setYear] = useState(currentYear)
  const [queried, setQueried] = useState(false)
  const [tabKey, setTabKey] = useState('r4')
  const tab = TABS.find((t) => t.key === tabKey)!

  const sched = useCit50Mapping(companyId, year, tab.sched ?? 8, queried && tab.sched !== null)
  const bs = useCit50BsMapping(companyId, year, queried && tab.sched === null)
  const active = tab.sched === null ? bs : sched
  const { data, isLoading, isError } = active
  const save = useSaveCit50Mapping()

  const [edits, setEdits] = useState<Record<string, string>>({})
  useEffect(() => { setEdits({}) }, [data])
  useEffect(() => { setQueried(false) }, [companyId])
  useEffect(() => { setEdits({}) }, [tabKey])

  const lines = (data?.lines ?? []).filter((l) => !l.isTotal)
  function lineOf(accountCode: string, orig?: string | null) {
    return edits[accountCode] !== undefined ? edits[accountCode] : (orig ?? '')
  }

  async function onSave() {
    if (!companyId || !data) return
    const items = data.accounts.map((a) => ({
      accountCode: a.accountCode,
      accountName: a.accountName,
      cit50LineCode: lineOf(a.accountCode, a.cit50LineCode) || null,
    }))
    await save.mutateAsync({ companyId, items, scope: tab.scope })
    setEdits({})
  }

  const dirty = data
    ? data.accounts.some((a) => edits[a.accountCode] !== undefined && edits[a.accountCode] !== (a.cit50LineCode ?? ''))
    : false

  return (
    <div>
      <PageHeader title="แมพบัญชี → ภ.ง.ด.50" />

      <div className="mb-4 flex flex-wrap gap-1 border-b border-gray-200">
        {TABS.map((t) => (
          <button key={t.key} onClick={() => { setTabKey(t.key); setQueried(false) }}
            className={`px-3 py-2 text-sm font-medium ${tabKey === t.key ? 'border-b-2 border-slate-700 text-slate-800' : 'text-gray-500 hover:text-gray-700'}`}>
            {t.label}
          </button>
        ))}
      </div>

      <Card className="mb-4 flex flex-wrap items-end gap-3 p-4">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
          <input type="number" min={2000} max={2100} value={year}
            onChange={(e) => { setYear(Number(e.target.value)); setQueried(false) }}
            className="w-24 rounded border border-gray-300 px-3 py-2 text-sm" />
        </div>
        <Button onClick={() => companyId && setQueried(true)} disabled={!companyId}>แสดงบัญชี</Button>
        {dirty && <Button onClick={onSave} disabled={save.isPending} className="ml-auto">{save.isPending ? 'กำลังบันทึก...' : 'บันทึกการแมพ'}</Button>}
        {save.isSuccess && !save.isPending && !dirty && <span className="ml-auto text-sm text-green-600">บันทึกแล้ว ✓</span>}
        {!companyId && <span className="text-sm text-amber-600">เลือกบริษัทก่อน</span>}
      </Card>

      <Card className="mb-4 p-4">
        <p className="text-sm text-gray-500">{tab.help}</p>
      </Card>

      {!queried ? (
        <Card><StateMessage centered>เลือกปี แล้วกด "แสดงบัญชี"</StateMessage></Card>
      ) : isLoading ? <StateMessage>กำลังโหลด...</StateMessage>
        : isError ? <StateMessage tone="error">โหลดไม่สำเร็จ</StateMessage>
        : !data || data.accounts.length === 0 ? <Card><StateMessage centered>ไม่พบบัญชี (ตรวจว่านำเข้า/post งบปีนี้แล้ว)</StateMessage></Card>
        : (
        <Card className="overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-xs text-gray-500">
              <tr>
                <th className="px-3 py-2 text-left">บัญชี</th>
                <th className="px-3 py-2 text-right">ยอดปีนี้</th>
                <th className="px-3 py-2 text-left">{tab.label}</th>
              </tr>
            </thead>
            <tbody>
              {data.accounts.map((a) => {
                const val = lineOf(a.accountCode, a.cit50LineCode)
                const changed = edits[a.accountCode] !== undefined && edits[a.accountCode] !== (a.cit50LineCode ?? '')
                return (
                  <tr key={a.accountCode} className={`border-t border-gray-50 ${changed ? 'bg-amber-50' : ''}`}>
                    <td className="px-3 py-1.5">
                      <span className="text-gray-400">{a.accountCode}</span> {a.accountName}
                    </td>
                    <td className="px-3 py-1.5 text-right font-mono">{fmt(a.amount)}</td>
                    <td className="px-3 py-1.5">
                      <select value={val} onChange={(e) => setEdits((p) => ({ ...p, [a.accountCode]: e.target.value }))}
                        className="w-80 rounded border border-gray-300 px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400">
                        <option value="">{tab.defaultLabel}</option>
                        {lines.map((l) => <option key={l.code} value={l.code}>{l.label}</option>)}
                      </select>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}
