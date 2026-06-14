import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useCit50Mapping, useSaveCit50Mapping } from '../hooks/useCorporateTax'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function Cit50MappingPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [year, setYear] = useState(currentYear)
  const [queried, setQueried] = useState(false)

  const { data, isLoading, isError } = useCit50Mapping(companyId, year, queried)
  const save = useSaveCit50Mapping()

  // edits: accountCode -> lineCode ('' = ไม่ระบุ → ลง "อื่นๆ")
  const [edits, setEdits] = useState<Record<string, string>>({})
  useEffect(() => { setEdits({}) }, [data])
  useEffect(() => { setQueried(false) }, [companyId])

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
    await save.mutateAsync({ companyId, items })
    setEdits({})
  }

  const dirty = data ? data.accounts.some((a) => edits[a.accountCode] !== undefined && edits[a.accountCode] !== (a.cit50LineCode ?? '')) : false

  return (
    <div>
      <PageHeader title="แมพบัญชี → รายการที่ 8 (ภ.ง.ด.50)" />
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
        <p className="text-sm text-gray-500">
          แมพบัญชีค่าใช้จ่ายขายและบริหาร → บรรทัดในรายการที่ 8 ของ ภ.ง.ด.50 (เช่น เงินเดือน→รายจ่ายเกี่ยวกับพนักงาน,
          ค่าเสื่อม→ค่าสึกหรอฯ). บัญชีที่ไม่เลือก จะลง "รายจ่ายอื่น (1.-29.)" อัตโนมัติ — ยอดรวมจะตรงเสมอ
        </p>
      </Card>

      {!queried ? (
        <Card><StateMessage centered>เลือกปี แล้วกด "แสดงบัญชี"</StateMessage></Card>
      ) : isLoading ? <StateMessage>กำลังโหลด...</StateMessage>
        : isError ? <StateMessage tone="error">โหลดไม่สำเร็จ</StateMessage>
        : !data || data.accounts.length === 0 ? <Card><StateMessage centered>ไม่พบบัญชีค่าใช้จ่าย (ตรวจว่านำเข้า/post งบปีนี้แล้ว)</StateMessage></Card>
        : (
        <Card className="overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-xs text-gray-500">
              <tr>
                <th className="px-3 py-2 text-left">บัญชี</th>
                <th className="px-3 py-2 text-right">ยอดปีนี้</th>
                <th className="px-3 py-2 text-left">บรรทัด ภ.ง.ด.50 (รายการ 8)</th>
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
                        className="w-72 rounded border border-gray-300 px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400">
                        <option value="">— รายจ่ายอื่น (ค่าเริ่มต้น) —</option>
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
