import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../shared/components/ui/ExportMenu'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import type { ExportSection } from '../../../shared/utils/exportTable'
import {
  useCompanySigners,
  useSaveTaxComputation,
  useSaveYearSigners,
  useSetDefaultSigners,
  useTaxComputation,
} from '../hooks/useCorporateTax'
import { useAuditors, useBookkeepers } from '../../settings/hooks/useAuditRegistry'
import { corporateTaxApi } from '../services/corporateTaxApi'
import {
  TAX_RATE_SCHEME_LABEL,
  TaxAdjustmentKind,
  TaxRateScheme,
} from '../types/corporateTax.types'

async function dlPnd50Pdf(companyId: number, year: number) {
  const blob = await corporateTaxApi.pnd50Pdf(companyId, year)
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `pnd50-${year + 543}.pdf`
  a.click()
  setTimeout(() => URL.revokeObjectURL(url), 30000)
}

interface AdjRow {
  description: string
  amount: string
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// ── คำนวณภาษีฝั่ง client (mirror CorporateTaxEngine) เพื่อ preview สด ──
function buildBrackets(income: number, scheme: number, customRate: number) {
  if (income <= 0) return [] as { label: string; base: number; ratePct: number; tax: number }[]
  if (scheme === TaxRateScheme.Flat20)
    return [{ label: 'กำไรสุทธิทั้งจำนวน', base: income, ratePct: 20, tax: round2(income * 0.2) }]
  if (scheme === TaxRateScheme.Custom)
    return [{ label: `กำไรสุทธิทั้งจำนวน (อัตรา ${customRate}%)`, base: income, ratePct: customRate, tax: round2((income * customRate) / 100) }]
  // SME ขั้นบันได
  const out: { label: string; base: number; ratePct: number; tax: number }[] = []
  const b1 = Math.min(income, 300_000)
  out.push({ label: '0 – 300,000 (ยกเว้น)', base: b1, ratePct: 0, tax: 0 })
  if (income > 300_000) {
    const b2 = Math.min(income, 3_000_000) - 300_000
    out.push({ label: '300,001 – 3,000,000', base: b2, ratePct: 15, tax: round2((b2 * 15) / 100) })
  }
  if (income > 3_000_000) {
    const b3 = income - 3_000_000
    out.push({ label: 'ส่วนเกิน 3,000,000', base: b3, ratePct: 20, tax: round2((b3 * 20) / 100) })
  }
  return out
}

function round2(n: number) {
  return Math.round(n * 100) / 100
}

export default function Pnd50Page() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [year, setYear] = useState(currentYear)
  const [queried, setQueried] = useState(false)

  const { data, isLoading, isError } = useTaxComputation(companyId, year, queried)
  const save = useSaveTaxComputation()

  // ── form state ──
  const [scheme, setScheme] = useState<number>(TaxRateScheme.SmeTiered)
  const [customRate, setCustomRate] = useState('')
  const [lossBf, setLossBf] = useState('')
  const [wht, setWht] = useState('')
  const [note, setNote] = useState('')
  const [addBacks, setAddBacks] = useState<AdjRow[]>([])
  const [deductions, setDeductions] = useState<AdjRow[]>([])

  // ยอดกำไรสุทธิทางบัญชีก่อนภาษี (จากงบ — ไม่ขึ้นกับฟอร์ม)
  const profitBeforeTax = data?.result.netProfitBeforeTax ?? 0

  useEffect(() => {
    if (!data) return
    setScheme(data.rateScheme)
    setCustomRate(data.customRatePct != null ? String(data.customRatePct) : '')
    setLossBf(data.lossBroughtForward ? String(data.lossBroughtForward) : '')
    setWht(data.whtCredit ? String(data.whtCredit) : '')
    setNote(data.note ?? '')
    setAddBacks(
      data.lines.filter((l) => l.kind === TaxAdjustmentKind.AddBack).map((l) => ({ description: l.description, amount: String(l.amount) })),
    )
    setDeductions(
      data.lines.filter((l) => l.kind === TaxAdjustmentKind.Deduction).map((l) => ({ description: l.description, amount: String(l.amount) })),
    )
  }, [data])

  useEffect(() => {
    setQueried(false)
  }, [companyId])

  // ── live computation ──
  const addBackTotal = addBacks.reduce((s, r) => s + (Number(r.amount) || 0), 0)
  const deductionTotal = deductions.reduce((s, r) => s + (Number(r.amount) || 0), 0)
  const lossBfNum = Number(lossBf) || 0
  const whtNum = Number(wht) || 0
  const customRateNum = Number(customRate) || 0

  const adjustedProfit = round2(profitBeforeTax + addBackTotal - deductionTotal)
  const lossUsed = adjustedProfit > 0 ? Math.min(adjustedProfit, Math.max(0, lossBfNum)) : 0
  const netTaxableIncome = Math.max(0, round2(adjustedProfit - lossUsed))
  const brackets = buildBrackets(netTaxableIncome, scheme, customRateNum)
  const taxAmount = round2(brackets.reduce((s, b) => s + b.tax, 0))
  const netPayable = round2(taxAmount - whtNum)
  const lossCarried =
    adjustedProfit < 0 ? round2(Math.max(0, lossBfNum) + -adjustedProfit) : round2(Math.max(0, lossBfNum) - lossUsed)

  function addRow(kind: 'add' | 'deduct') {
    const r = { description: '', amount: '' }
    if (kind === 'add') setAddBacks((p) => [...p, r])
    else setDeductions((p) => [...p, r])
  }
  function updateRow(kind: 'add' | 'deduct', i: number, field: keyof AdjRow, val: string) {
    const setter = kind === 'add' ? setAddBacks : setDeductions
    setter((p) => p.map((r, idx) => (idx === i ? { ...r, [field]: val } : r)))
  }
  function removeRow(kind: 'add' | 'deduct', i: number) {
    const setter = kind === 'add' ? setAddBacks : setDeductions
    setter((p) => p.filter((_, idx) => idx !== i))
  }

  async function onSave() {
    if (!companyId) return
    const lines = [
      ...addBacks
        .filter((r) => r.description.trim() && Number(r.amount) > 0)
        .map((r, i) => ({ kind: TaxAdjustmentKind.AddBack, description: r.description.trim(), amount: Number(r.amount), sortOrder: i })),
      ...deductions
        .filter((r) => r.description.trim() && Number(r.amount) > 0)
        .map((r, i) => ({ kind: TaxAdjustmentKind.Deduction, description: r.description.trim(), amount: Number(r.amount), sortOrder: i })),
    ]
    await save.mutateAsync({
      companyId,
      year,
      data: {
        rateScheme: scheme,
        customRatePct: scheme === TaxRateScheme.Custom ? customRateNum : null,
        lossBroughtForward: lossBfNum,
        whtCredit: whtNum,
        note: note || null,
        lines,
      },
    })
  }

  const exportSections = (): ExportSection[] => [
    {
      name: `ภ.ง.ด.50 ${year}`,
      columns: [
        { key: 'label', header: 'รายการ' },
        { key: 'amount', header: 'จำนวนเงิน', align: 'right' },
      ],
      rows: [
        { label: 'กำไรสุทธิทางบัญชีก่อนภาษี', amount: fmt(profitBeforeTax) },
        { label: 'บวก รายการบวกกลับ', amount: fmt(addBackTotal) },
        { label: 'หัก รายการหักออก', amount: fmt(deductionTotal) },
        { label: 'กำไรสุทธิทางภาษีก่อนหักขาดทุนสะสม', amount: fmt(adjustedProfit) },
        { label: 'หัก ผลขาดทุนสะสมยกมาที่ใช้ได้', amount: fmt(lossUsed) },
        { label: 'เงินได้สุทธิเพื่อเสียภาษี', amount: fmt(netTaxableIncome) },
        ...brackets.map((b) => ({ label: `  ${b.label} @ ${b.ratePct}%`, amount: fmt(b.tax) })),
        { label: 'ภาษีเงินได้นิติบุคคล', amount: fmt(taxAmount) },
        { label: 'หัก ภาษีจ่ายล่วงหน้า (WHT)', amount: fmt(whtNum) },
        { label: netPayable >= 0 ? 'ภาษีที่ต้องชำระเพิ่ม' : 'ภาษีชำระไว้เกิน (ขอคืน)', amount: fmt(Math.abs(netPayable)) },
        { label: 'ผลขาดทุนสะสมยกไปปีถัดไป', amount: fmt(lossCarried) },
      ],
    },
  ]

  return (
    <div>
      <PageHeader title="ภ.ง.ด.50 — คำนวณภาษีเงินได้นิติบุคคล" />

      <Card className="mb-5 flex flex-wrap items-end gap-3 p-4">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
          <input
            type="number" min={2000} max={2100} value={year}
            onChange={(e) => { setYear(Number(e.target.value)); setQueried(false) }}
            className="w-24 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
          />
        </div>
        <Button onClick={() => companyId && setQueried(true)} disabled={!companyId}>แสดงข้อมูล</Button>
        {queried && data && (
          <div className="ml-auto flex items-center gap-2">
            <Button type="button" variant="secondary" onClick={() => dlPnd50Pdf(companyId, year)}>⬇ ภ.ง.ด.50 (PDF)</Button>
            <ExportMenu meta={{ title: `ภ.ง.ด.50 ปี ${year}`, subtitle: data.clientName, fileName: `pnd50-${companyId}-${year}` }} getSections={exportSections} />
          </div>
        )}
        {!companyId && <span className="text-sm text-amber-600">กรุณาเลือกบริษัทก่อน</span>}
      </Card>

      {!queried ? (
        <Card><StateMessage centered>เลือกปีบัญชี แล้วกด "แสดงข้อมูล"</StateMessage></Card>
      ) : isLoading ? (
        <StateMessage>กำลังคำนวณ...</StateMessage>
      ) : isError ? (
        <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
      ) : !data ? (
        <Card><StateMessage centered>ไม่พบข้อมูล</StateMessage></Card>
      ) : (
        <>
          {data.warnings.length > 0 && (
            <Card className="mb-4 border-amber-200 bg-amber-50 p-4">
              <p className="mb-1 text-sm font-semibold text-amber-800">ข้อควรระวังก่อนสรุปภาษี</p>
              <ul className="list-inside list-disc text-sm text-amber-700">
                {data.warnings.map((w, i) => <li key={i}>{w}</li>)}
              </ul>
            </Card>
          )}

          <div className="grid gap-5 lg:grid-cols-2">
            {/* ── กระดาษทำการ ── */}
            <Card className="p-6">
              <h2 className="mb-4 text-base font-semibold text-slate-800">กระดาษทำการ</h2>

              <dl className="space-y-2 text-sm">
                <Row label="กำไร (ขาดทุน) สุทธิทางบัญชีก่อนภาษี" value={profitBeforeTax} highlight />
              </dl>

              <AdjustmentEditor
                title="บวก: รายการบวกกลับ (ค่าใช้จ่ายต้องห้าม ฯลฯ)"
                rows={addBacks} total={addBackTotal} tone="add"
                onAdd={() => addRow('add')}
                onChange={(i, f, v) => updateRow('add', i, f, v)}
                onRemove={(i) => removeRow('add', i)}
              />
              <AdjustmentEditor
                title="หัก: รายการหักออก (รายได้ยกเว้น/หักได้เพิ่ม)"
                rows={deductions} total={deductionTotal} tone="deduct"
                onAdd={() => addRow('deduct')}
                onChange={(i, f, v) => updateRow('deduct', i, f, v)}
                onRemove={(i) => removeRow('deduct', i)}
              />

              <div className="mt-5 space-y-4 border-t border-gray-100 pt-4">
                <Field label="อัตราภาษี">
                  <select
                    value={scheme}
                    onChange={(e) => setScheme(Number(e.target.value))}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
                  >
                    {Object.entries(TAX_RATE_SCHEME_LABEL).map(([k, v]) => (
                      <option key={k} value={k}>{v}</option>
                    ))}
                  </select>
                </Field>
                {scheme === TaxRateScheme.Custom && (
                  <Field label="อัตราภาษีกำหนดเอง (%)">
                    <input type="number" min={0} max={100} step={0.01} value={customRate}
                      onChange={(e) => setCustomRate(e.target.value)}
                      className="w-44 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400" />
                  </Field>
                )}
                <Field label="ผลขาดทุนสะสมยกมา (หักก่อนคำนวณภาษี, สิทธิ ≤ 5 ปี)">
                  <input type="number" min={0} step={0.01} value={lossBf}
                    onChange={(e) => setLossBf(e.target.value)}
                    className="w-44 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400" />
                </Field>
                <Field label="ภาษีจ่ายล่วงหน้า / ถูกหัก ณ ที่จ่าย (WHT)">
                  <input type="number" min={0} step={0.01} value={wht}
                    onChange={(e) => setWht(e.target.value)}
                    className="w-44 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400" />
                </Field>
                <Field label="หมายเหตุ">
                  <input type="text" value={note} onChange={(e) => setNote(e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400" />
                </Field>

                <div className="flex items-center gap-3">
                  <Button type="button" onClick={onSave} disabled={save.isPending} className="bg-blue-600 text-white hover:bg-blue-700">
                    {save.isPending ? 'กำลังบันทึก...' : 'บันทึก + ลงภาษีในงบ'}
                  </Button>
                  {save.isSuccess && !save.isPending && <span className="text-sm text-green-600">บันทึกแล้ว ✓</span>}
                  {save.isError && <span className="text-sm text-red-600">บันทึกไม่สำเร็จ — ตรวจข้อมูล</span>}
                </div>
              </div>
            </Card>

            {/* ── ผลการคำนวณ ── */}
            <Card className="p-6">
              <h2 className="mb-4 text-base font-semibold text-slate-800">ผลการคำนวณภาษี</h2>
              <dl className="space-y-2 text-sm">
                <Row label="กำไรสุทธิทางบัญชีก่อนภาษี" value={profitBeforeTax} />
                <Row label="บวก รายการบวกกลับ" value={addBackTotal} muted />
                <Row label="หัก รายการหักออก" value={-deductionTotal} muted />
                <div className="border-t border-gray-100 pt-2" />
                <Row label="กำไรสุทธิทางภาษีก่อนหักขาดทุนสะสม" value={adjustedProfit} />
                <Row label="หัก ผลขาดทุนสะสมยกมาที่ใช้ได้" value={-lossUsed} muted />
                <Row label="เงินได้สุทธิเพื่อเสียภาษี" value={netTaxableIncome} highlight />
              </dl>

              {brackets.length > 0 && (
                <div className="mt-4 rounded border border-gray-100 bg-slate-50 p-3 text-xs">
                  <p className="mb-2 font-medium text-gray-600">การคำนวณภาษีตามอัตรา</p>
                  <table className="w-full">
                    <tbody>
                      {brackets.map((b, i) => (
                        <tr key={i} className="text-gray-700">
                          <td className="py-0.5">{b.label}</td>
                          <td className="py-0.5 text-right font-mono">{fmt(b.base)}</td>
                          <td className="py-0.5 pl-2 text-right text-gray-400">{b.ratePct}%</td>
                          <td className="py-0.5 pl-2 text-right font-mono">{fmt(b.tax)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              <dl className="mt-4 space-y-2 border-t border-gray-100 pt-3 text-sm">
                <Row label="ภาษีเงินได้นิติบุคคล" value={taxAmount} highlight />
                <Row label="หัก ภาษีจ่ายล่วงหน้า (WHT)" value={-whtNum} muted />
                {netPayable >= 0 ? (
                  <Row label="ภาษีที่ต้องชำระเพิ่ม (หนี้สิน)" value={netPayable} highlight />
                ) : (
                  <Row label="ภาษีชำระไว้เกิน — ขอคืน (สินทรัพย์)" value={-netPayable} highlight />
                )}
                <div className="border-t border-gray-100 pt-2" />
                <Row label="ผลขาดทุนสะสมยกไปปีถัดไป" value={lossCarried} muted />
              </dl>
            </Card>
          </div>

          {/* ── ผู้ลงนามรับผิดชอบงบ (master + ค่าเริ่มต้น + override รายปี) ── */}
          <SignersCard companyId={companyId} year={year} />
        </>
      )}
    </div>
  )
}

function SignersCard({ companyId, year }: { companyId: number; year: number }) {
  const { data } = useCompanySigners(companyId, year)
  const { data: auditors } = useAuditors()
  const { data: bookkeepers } = useBookkeepers()
  const saveDefault = useSetDefaultSigners()
  const saveYear = useSaveYearSigners()

  const [defAuditor, setDefAuditor] = useState<number | ''>('')
  const [defBookkeeper, setDefBookkeeper] = useState<number | ''>('')
  const [signDate, setSignDate] = useState('')
  const [override, setOverride] = useState(false)
  const [yrAuditor, setYrAuditor] = useState<number | ''>('')
  const [yrBookkeeper, setYrBookkeeper] = useState<number | ''>('')

  useEffect(() => {
    if (!data) return
    setDefAuditor(data.defaultAuditorId ?? '')
    setDefBookkeeper(data.defaultBookkeeperId ?? '')
    setSignDate(data.signDate ? data.signDate.slice(0, 10) : '')
    setOverride(data.hasYearOverride)
    setYrAuditor(data.yearAuditorId ?? '')
    setYrBookkeeper(data.yearBookkeeperId ?? '')
  }, [data])

  const auditorOpts = auditors ?? []
  const bkOpts = bookkeepers ?? []
  const noMasters = auditorOpts.length === 0 && bkOpts.length === 0

  const resolvedAuditorId = (override ? (yrAuditor || null) : null) ?? (defAuditor || null)
  const resolvedBookkeeperId = (override ? (yrBookkeeper || null) : null) ?? (defBookkeeper || null)
  const ra = auditorOpts.find((a) => a.id === resolvedAuditorId)
  const rb = bkOpts.find((b) => b.id === resolvedBookkeeperId)
  const sameTaxId = !!ra?.taxId && !!rb?.taxId && ra.taxId === rb.taxId
  const taIsAuditor = ra?.type === 2 // TA

  async function onSaveDefault() {
    await saveDefault.mutateAsync({ companyId, data: { auditorId: defAuditor || null, bookkeeperId: defBookkeeper || null } })
  }
  async function onSaveYear() {
    await saveYear.mutateAsync({
      companyId, year,
      data: {
        auditorId: override ? (yrAuditor || null) : null,
        bookkeeperId: override ? (yrBookkeeper || null) : null,
        signDate: signDate || null,
      },
    })
  }

  return (
    <Card className="mt-5 p-6">
      <h2 className="mb-1 text-base font-semibold text-slate-800">ผู้ลงนามรับผิดชอบงบ (รอบปี {year + 543})</h2>
      <p className="mb-4 text-xs text-gray-400">
        เลือกจากทะเบียน{' '}
        <a href="/settings/auditors" className="font-medium text-blue-600 hover:underline">ผู้สอบบัญชี</a> /{' '}
        <a href="/settings/bookkeepers" className="font-medium text-blue-600 hover:underline">ผู้ทำบัญชี</a>
        {' '}· "สำนักงานทำบัญชี" ดึงจาก{' '}
        <a href="/settings/office-profile" className="font-medium text-blue-600 hover:underline">โปรไฟล์สำนักงาน</a>
      </p>

      {noMasters && (
        <p className="mb-4 rounded border border-amber-200 bg-amber-50 p-3 text-sm text-amber-700">
          ยังไม่มีรายชื่อในทะเบียน — เพิ่มผู้สอบบัญชี/ผู้ทำบัญชีที่เมนูตั้งค่ากลางก่อน แล้วกลับมาเลือก
        </p>
      )}

      {/* ค่าเริ่มต้นประจำบริษัท */}
      <p className="mb-2 text-sm font-medium text-slate-600">ผู้ลงนามประจำบริษัท (ใช้ทุกปีโดยอัตโนมัติ)</p>
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="ผู้ตรวจสอบและรับรองบัญชี">
          <SignerSelect value={defAuditor} onChange={setDefAuditor}
            options={auditorOpts.map((a) => ({ id: a.id, label: `${a.name}${a.type === 2 ? ' (TA)' : ''}`, active: a.isActive }))} />
        </Field>
        <Field label="ผู้ทำบัญชี">
          <SignerSelect value={defBookkeeper} onChange={setDefBookkeeper}
            options={bkOpts.map((b) => ({ id: b.id, label: b.name, active: b.isActive }))} />
        </Field>
      </div>
      <div className="mt-3 flex items-center gap-3">
        <Button type="button" onClick={onSaveDefault} disabled={saveDefault.isPending}>
          {saveDefault.isPending ? 'กำลังบันทึก...' : 'บันทึกค่าเริ่มต้นบริษัท'}
        </Button>
        {saveDefault.isSuccess && !saveDefault.isPending && <span className="text-sm text-green-600">บันทึกแล้ว ✓</span>}
      </div>

      {sameTaxId && (
        <p className="mt-3 rounded border border-red-200 bg-red-50 p-2 text-xs text-red-600">
          ⚠ ผู้สอบบัญชีและผู้ทำบัญชีมีเลขผู้เสียภาษีเดียวกัน — ผู้สอบบัญชีต้องเป็นอิสระ (ห้ามเป็นผู้ทำบัญชีของกิจการเดียวกัน)
        </p>
      )}
      {taIsAuditor && (
        <p className="mt-2 text-xs text-amber-600">
          หมายเหตุ: ผู้สอบเป็น TA — สอบได้เฉพาะห้างหุ้นส่วนจดทะเบียนขนาดเล็ก (บริษัทจำกัดต้องใช้ CPA)
        </p>
      )}

      {/* เฉพาะรอบปี */}
      <div className="mt-6 border-t border-gray-100 pt-4">
        <p className="mb-2 text-sm font-medium text-slate-600">เฉพาะรอบปี {year + 543}</p>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="วันที่ในรายงานของผู้สอบบัญชี">
            <input type="date" value={signDate} onChange={(e) => setSignDate(e.target.value)}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400" />
          </Field>
        </div>
        <label className="mt-3 flex items-center gap-2 text-sm text-gray-600">
          <input type="checkbox" checked={override} onChange={(e) => setOverride(e.target.checked)} />
          ปีนี้เปลี่ยนผู้ลงนามต่างจากค่าเริ่มต้น
        </label>
        {override && (
          <div className="mt-3 grid gap-4 sm:grid-cols-2">
            <Field label="ผู้สอบบัญชี (เฉพาะปีนี้)">
              <SignerSelect value={yrAuditor} onChange={setYrAuditor}
                options={auditorOpts.map((a) => ({ id: a.id, label: `${a.name}${a.type === 2 ? ' (TA)' : ''}`, active: a.isActive }))} />
            </Field>
            <Field label="ผู้ทำบัญชี (เฉพาะปีนี้)">
              <SignerSelect value={yrBookkeeper} onChange={setYrBookkeeper}
                options={bkOpts.map((b) => ({ id: b.id, label: b.name, active: b.isActive }))} />
            </Field>
          </div>
        )}
        <div className="mt-4 flex items-center gap-3">
          <Button type="button" onClick={onSaveYear} disabled={saveYear.isPending}
            className="bg-blue-600 text-white hover:bg-blue-700">
            {saveYear.isPending ? 'กำลังบันทึก...' : 'บันทึกข้อมูลปีนี้'}
          </Button>
          {saveYear.isSuccess && !saveYear.isPending && <span className="text-sm text-green-600">บันทึกแล้ว ✓</span>}
        </div>
      </div>
    </Card>
  )
}

function SignerSelect({
  value, onChange, options,
}: {
  value: number | ''
  onChange: (v: number | '') => void
  options: { id: number; label: string; active: boolean }[]
}) {
  return (
    <select value={value} onChange={(e) => onChange(e.target.value ? Number(e.target.value) : '')}
      className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400">
      <option value="">— ไม่ระบุ —</option>
      {options.filter((o) => o.active || o.id === value).map((o) => (
        <option key={o.id} value={o.id}>{o.label}{o.active ? '' : ' (ปิดใช้งาน)'}</option>
      ))}
    </select>
  )
}

function AdjustmentEditor({
  title, rows, total, tone, onAdd, onChange, onRemove,
}: {
  title: string
  rows: AdjRow[]
  total: number
  tone: 'add' | 'deduct'
  onAdd: () => void
  onChange: (i: number, field: keyof AdjRow, val: string) => void
  onRemove: (i: number) => void
}) {
  return (
    <div className="mt-4 border-t border-gray-100 pt-3">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-medium text-gray-600">{title}</p>
        <Button type="button" variant="secondary" onClick={onAdd} className="px-2 py-1 text-xs">+ เพิ่มรายการ</Button>
      </div>
      {rows.length === 0 ? (
        <p className="py-1 text-xs text-gray-400">— ไม่มีรายการ —</p>
      ) : (
        <div className="space-y-1.5">
          {rows.map((r, i) => (
            <div key={i} className="flex items-center gap-2">
              <input
                type="text" placeholder="คำอธิบายรายการ" value={r.description}
                onChange={(e) => onChange(i, 'description', e.target.value)}
                className="flex-1 rounded border border-gray-300 px-2 py-1 text-xs focus:outline-none focus:ring-1 focus:ring-blue-400"
              />
              <input
                type="number" min={0} step={0.01} placeholder="0.00" value={r.amount}
                onChange={(e) => onChange(i, 'amount', e.target.value)}
                className="w-32 rounded border border-gray-300 px-2 py-1 text-right text-xs font-mono focus:outline-none focus:ring-1 focus:ring-blue-400"
              />
              <button type="button" onClick={() => onRemove(i)} className="px-1 text-gray-400 hover:text-red-500" title="ลบ">✕</button>
            </div>
          ))}
        </div>
      )}
      <div className={`mt-1.5 flex justify-between text-xs font-semibold ${tone === 'add' ? 'text-slate-700' : 'text-slate-700'}`}>
        <span>รวม{tone === 'add' ? 'บวกกลับ' : 'หักออก'}</span>
        <span className="font-mono">{fmt(total)}</span>
      </div>
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
