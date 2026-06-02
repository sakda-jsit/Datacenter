import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import {
  useBalanceSheet,
  useExternalInputs,
  useProfitLoss,
  useUpsertExternalInput,
} from '../../financial-statement/hooks/useFinancialStatement'

const RATE = 20 // อัตราภาษีเงินได้นิติบุคคลมาตรฐาน (ใช้เป็นตัวช่วยประมาณ)

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function Pnd50Page() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [year, setYear] = useState(currentYear)
  const [queried, setQueried] = useState(false)
  const [taxAmount, setTaxAmount] = useState('')
  const [whtAmount, setWhtAmount] = useState('')
  const [note, setNote] = useState('')

  const params = { clientCompanyId: companyId, fiscalYear: year }
  const plQuery = useProfitLoss(params, queried)
  const bsQuery = useBalanceSheet(params, queried)
  const inputsQuery = useExternalInputs(params, queried)
  const upsert = useUpsertExternalInput()

  // Pre-fill saved values when they load
  useEffect(() => {
    const list = inputsQuery.data
    if (!list) return
    const x4 = list.find((i) => i.refCode === 'X4')
    const wht = list.find((i) => i.refCode === 'WHT')
    setTaxAmount(x4 ? String(x4.amount) : '')
    setWhtAmount(wht ? String(wht.amount) : '')
    setNote(x4?.note ?? '')
  }, [inputsQuery.data])

  useEffect(() => {
    setQueried(false)
  }, [companyId])

  const profitBeforeTax = plQuery.data?.profitBeforeTax ?? 0
  const tax = Number(taxAmount) || 0
  const wht = Number(whtAmount) || 0
  const estimate = Math.max(0, Math.round(profitBeforeTax * RATE) / 100)
  const netProfit = profitBeforeTax - tax
  const netPayable = tax - wht

  async function save(e: React.FormEvent) {
    e.preventDefault()
    if (!companyId) return
    await upsert.mutateAsync({ clientCompanyId: companyId, fiscalYear: year, refCode: 'X4', amount: tax, note: note || undefined })
    await upsert.mutateAsync({ clientCompanyId: companyId, fiscalYear: year, refCode: 'WHT', amount: wht, note: undefined })
  }

  return (
    <div>
      <PageHeader title="ภ.ง.ด.50 — ภาษีเงินได้นิติบุคคล" />

      <Card className="mb-5 flex flex-wrap items-end gap-3 p-4">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
          <input
            type="number" min={2000} max={2100} value={year}
            onChange={(e) => { setYear(Number(e.target.value)); setQueried(false) }}
            className="w-24 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
          />
        </div>
        <Button onClick={() => companyId && setQueried(true)} disabled={!companyId}>
          แสดงข้อมูล
        </Button>
        {!companyId && <span className="text-sm text-amber-600">กรุณาเลือกบริษัทก่อน</span>}
      </Card>

      {!queried ? (
        <Card><StateMessage centered>เลือกปีบัญชี แล้วกด "แสดงข้อมูล"</StateMessage></Card>
      ) : plQuery.isLoading ? (
        <StateMessage>กำลังคำนวณ...</StateMessage>
      ) : plQuery.isError ? (
        <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
      ) : !plQuery.data ? (
        <Card><StateMessage centered>ไม่พบข้อมูลงบกำไรขาดทุน — ตรวจสอบว่านำเข้าและ post ข้อมูลแล้ว</StateMessage></Card>
      ) : (
        <div className="grid gap-5 lg:grid-cols-2">
          {/* ── คำนวณภาษี ── */}
          <Card className="p-6">
            <h2 className="mb-4 text-base font-semibold text-slate-800">การคำนวณภาษี</h2>
            <dl className="space-y-2 text-sm">
              <Row label="กำไร (ขาดทุน) ก่อนภาษีเงินได้" value={profitBeforeTax} />
              <Row label={`ภาษีโดยประมาณ (${RATE}% ของกำไรก่อนภาษี)`} value={estimate} muted />
            </dl>

            <form onSubmit={save} className="mt-5 space-y-4 border-t border-gray-100 pt-4">
              <Field label="ภาษีเงินได้ที่ต้องชำระ (X4) — จากแบบ ภ.ง.ด.50">
                <div className="flex gap-2">
                  <input
                    type="number" min={0} step={0.01} value={taxAmount}
                    onChange={(e) => setTaxAmount(e.target.value)}
                    className="w-44 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
                  />
                  <Button type="button" variant="secondary" onClick={() => setTaxAmount(String(estimate))}>
                    ใช้ค่าประมาณ
                  </Button>
                </div>
              </Field>

              <Field label="ภาษีจ่ายล่วงหน้าที่นำมาหัก (WHT) — ภาษีถูกหัก ณ ที่จ่าย/จ่ายล่วงหน้า">
                <input
                  type="number" min={0} step={0.01} value={whtAmount}
                  onChange={(e) => setWhtAmount(e.target.value)}
                  className="w-44 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
                />
              </Field>

              <Field label="หมายเหตุ">
                <input
                  type="text" value={note}
                  onChange={(e) => setNote(e.target.value)}
                  className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
                />
              </Field>

              <div className="flex items-center gap-3">
                <Button type="submit" disabled={upsert.isPending} className="bg-blue-600 hover:bg-blue-700 text-white">
                  {upsert.isPending ? 'กำลังบันทึก...' : 'บันทึก'}
                </Button>
                {upsert.isSuccess && !upsert.isPending && (
                  <span className="text-sm text-green-600">บันทึกแล้ว ✓</span>
                )}
              </div>
            </form>
          </Card>

          {/* ── ผลลัพธ์ ── */}
          <Card className="p-6">
            <h2 className="mb-4 text-base font-semibold text-slate-800">ผลต่องบการเงิน</h2>
            <dl className="space-y-2 text-sm">
              <Row label="กำไร (ขาดทุน) สุทธิหลังภาษี" value={netProfit} highlight />
              <div className="border-t border-gray-100 pt-2" />
              <Row label="ภาษีเงินได้ทั้งปี" value={tax} />
              <Row label="หัก ภาษีจ่ายล่วงหน้า" value={-wht} />
              {netPayable >= 0 ? (
                <Row label="ภาษีเงินได้ค้างจ่าย (หนี้สิน)" value={netPayable} highlight />
              ) : (
                <Row label="ภาษีจ่ายล่วงหน้ารอขอคืน (สินทรัพย์)" value={-netPayable} highlight />
              )}
            </dl>

            {bsQuery.data && (
              <p className={`mt-4 rounded px-3 py-2 text-sm ${Math.abs(bsQuery.data.balanceDifference) < 0.01 ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>
                งบดุล: สินทรัพย์ {fmt(bsQuery.data.totalAssets)} / หนี้สิน+ทุน {fmt(bsQuery.data.totalLiabilitiesAndEquity)}
                {Math.abs(bsQuery.data.balanceDifference) < 0.01 ? ' — สมดุล ✓' : ` — ต่าง ${fmt(bsQuery.data.balanceDifference)}`}
              </p>
            )}
          </Card>
        </div>
      )}
    </div>
  )
}

function Row({ label, value, highlight, muted }: { label: string; value: number; highlight?: boolean; muted?: boolean }) {
  return (
    <div className={`flex items-center justify-between ${highlight ? 'font-semibold text-slate-800' : muted ? 'text-gray-500' : 'text-gray-700'}`}>
      <dt>{label}</dt>
      <dd className={`font-mono ${value < 0 ? 'text-red-600' : ''}`}>{fmt(value)}</dd>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-gray-600">{label}</label>
      {children}
    </div>
  )
}
