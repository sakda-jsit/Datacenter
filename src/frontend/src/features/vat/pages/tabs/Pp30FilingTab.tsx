import { useEffect, useMemo, useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import Button from '../../../../shared/components/ui/Button'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import DataAsOfBanner from '../../../../shared/components/ui/DataAsOfBanner'
import { useVatReport } from '../../hooks/useVat'
import { vatApi } from '../../services/vatApi'
import { MONTH_LABEL } from '../../types/vat.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

async function dlTransfer(companyId: number, year: number, month: number) {
  const blob = await vatApi.pp30Transfer(companyId, year, month)
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `pp30-transfer-${year + 543}-${String(month).padStart(2, '0')}.txt`
  a.click()
  setTimeout(() => URL.revokeObjectURL(url), 30000)
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  year: number
}

/**
 * ใบช่วยกรอก ภ.พ.30 สำหรับยื่น e-Filing (หน้า "ข้อมูลการคำนวณภาษี" ของกรมสรรพากร).
 * แสดงค่าตรงตามช่องในเว็บ RD เรียงลำดับเดียวกัน เพื่ออ่านแล้วพิมพ์ลงเว็บได้ทันที.
 * ค่าทั้งหมดดึงจาก VatReport (Express ISVAT) ยกเว้น "ภาษีชำระเกินยกมา" (กรอกเอง — ระบบไม่ track ยอดสะสม).
 */
export default function Pp30FilingTab({ companyId, year }: Props) {
  const { data, isLoading, isError } = useVatReport(companyId, year)
  const [month, setMonth] = useState(1)
  const [creditCarried, setCreditCarried] = useState('')

  // เลือกเดือนแรกที่มีข้อมูลอัตโนมัติ
  const monthsWithData = useMemo(
    () => (data?.months ?? []).filter((m) => m.outputCount > 0 || m.inputCount > 0).map((m) => m.month),
    [data],
  )
  useEffect(() => {
    if (monthsWithData.length > 0 && !monthsWithData.includes(month)) setMonth(monthsWithData[0])
  }, [monthsWithData]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => setCreditCarried(''), [month, companyId, year])

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  if (isError) return <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
  if (isLoading) return <StateMessage>กำลังโหลด...</StateMessage>

  const hasData = data && monthsWithData.length > 0
  if (!data || !hasData) {
    return <Card><StateMessage centered>{`ไม่มีข้อมูลภาษีมูลค่าเพิ่มสำหรับปี ${year} — นำเข้าข้อมูลจาก Express (ISVAT) ที่เมนูนำเข้าข้อมูล`}</StateMessage></Card>
  }

  const m = data.months.find((x) => x.month === month)
  // ── map ช่องในเว็บ ภ.พ.30 ──
  const zeroRated = m?.outputZeroRated ?? 0
  const taxableSales = m?.outputBase ?? 0          // ยอดขายที่ต้องเสียภาษี (7%)
  const totalSales = taxableSales + zeroRated       // ยอดขายในเดือนนี้ (รวม; ไม่รวมขายยกเว้นที่ไม่อยู่ใน ISVAT)
  const eligiblePurchase = m?.inputBase ?? 0
  const outputVat = m?.outputVat ?? 0
  const inputVat = m?.inputVat ?? 0
  const taxThisMonth = Math.round((outputVat - inputVat) * 100) / 100 // >0 ต้องชำระ, <0 ชำระเกิน
  const credit = Number(creditCarried) || 0
  const netTax = Math.round((taxThisMonth - credit) * 100) / 100

  const yearBe = year + 543
  const periodTh = `${MONTH_LABEL[month]} ${yearBe}`

  const exportSections = (): ExportSection[] => [
    {
      name: `ภ.พ.30 ${periodTh}`,
      columns: [
        { key: 'label', header: 'ช่องในแบบ ภ.พ.30' },
        { key: 'value', header: 'จำนวนเงิน', align: 'right' },
      ],
      rows: [
        { label: 'ยอดขายในเดือนนี้', value: fmt(totalSales) },
        { label: 'ยอดขายที่เสียภาษีในอัตรา ร้อยละ 0', value: fmt(zeroRated) },
        { label: 'ยอดขายที่ได้รับยกเว้น', value: fmt(0) },
        { label: 'ยอดซื้อที่มีสิทธินำภาษีซื้อมาหัก', value: fmt(eligiblePurchase) },
        { label: 'ภาษีขายเดือนนี้', value: fmt(outputVat) },
        { label: 'ภาษีซื้อเดือนนี้', value: fmt(inputVat) },
        { label: 'ภาษีเดือนนี้ (รอคำนวณ)', value: fmt(taxThisMonth) },
        { label: 'ภาษีชำระเกินยกมา', value: fmt(credit) },
        { label: 'ภาษีสุทธิ (รอคำนวณ)', value: fmt(netTax) },
      ],
    },
  ]

  return (
    <div>
      <DataAsOfBanner dataAsOf={data.dataAsOf} noun="ภาษีมูลค่าเพิ่ม" />

      <Card className="mb-4 flex flex-wrap items-end justify-between gap-3 p-4">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">เดือนภาษี</label>
          <select
            value={month}
            onChange={(e) => setMonth(Number(e.target.value))}
            className="w-40 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
          >
            {Array.from({ length: 12 }, (_, i) => i + 1).map((mm) => {
              const has = monthsWithData.includes(mm)
              return (
                <option key={mm} value={mm} disabled={!has}>
                  {MONTH_LABEL[mm]} {yearBe}{has ? '' : ' (ไม่มีข้อมูล)'}
                </option>
              )
            })}
          </select>
        </div>
        <div className="flex items-center gap-2">
          <Button type="button" variant="secondary" onClick={() => dlTransfer(companyId, year, month)}>
            ⬇ ไฟล์โอนย้าย (.txt)
          </Button>
          <ExportMenu
            meta={{ title: `ใบกรอก ภ.พ.30 — ${periodTh}`, subtitle: data.clientName, fileName: `pp30-filing-${companyId}-${yearBe}-${String(month).padStart(2, '0')}` }}
            getSections={exportSections}
          />
        </div>
      </Card>

      <Card className="p-6">
        <div className="mb-5 border-b pb-3">
          <p className="text-base font-semibold text-slate-800">ใบช่วยกรอก ภ.พ.30 — ยื่น e-Filing</p>
          <p className="text-xs text-gray-500">{data.clientName} · เดือนภาษี {periodTh} · กรอกค่าตามนี้ลงหน้า "ข้อมูลการคำนวณภาษี" ของกรมสรรพากร</p>
        </div>

        <Section title="ยอดขาย และยอดซื้อ">
          <FieldRow label="ยอดขายในเดือนนี้" value={totalSales} required />
          <FieldRow label="ยอดขายที่เสียภาษีในอัตรา ร้อยละ 0" value={zeroRated} />
          <FieldRow label="ยอดขายที่ได้รับยกเว้น" value={0} hint="ISVAT ไม่เก็บขายยกเว้น — บวกเองถ้ามี" />
          <FieldRow label="ยอดซื้อที่มีสิทธินำภาษีซื้อมาหักในการคำนวณภาษีเดือนนี้" value={eligiblePurchase} required />
        </Section>

        <Section title="ภาษีขาย และภาษีซื้อ">
          <FieldRow label="ภาษีขายเดือนนี้" value={outputVat} required />
          <FieldRow label="ภาษีซื้อเดือนนี้" value={inputVat} required />
          <FieldRow
            label="ภาษีเดือนนี้ (รอคำนวณ)" value={taxThisMonth} computed
            hint={taxThisMonth >= 0 ? 'ภาษีที่ต้องชำระ' : 'ภาษีชำระเกิน (ยกไปเดือนถัดไป)'}
          />
        </Section>

        <Section title="ภาษีมูลค่าเพิ่มที่ชำระเกินยกมา">
          <div className="flex items-center justify-between py-1.5">
            <div className="text-sm text-gray-700">
              ภาษีชำระเกินยกมา
              <span className="ml-2 text-xs text-gray-400">(จากเดือนก่อน — ระบบไม่เก็บยอดสะสม กรอกเอง)</span>
            </div>
            <input
              type="number" min={0} step={0.01} value={creditCarried} placeholder="0.00"
              onChange={(e) => setCreditCarried(e.target.value)}
              className="w-40 rounded border border-gray-300 px-3 py-1.5 text-right text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-400"
            />
          </div>
          <FieldRow label="ภาษีสุทธิ (รอคำนวณ)" value={netTax} computed
            hint={netTax >= 0 ? 'ภาษีสุทธิที่ต้องชำระ' : 'ภาษีชำระเกินคงเหลือยกไป'} />
        </Section>

        <p className="mt-4 rounded bg-slate-50 px-3 py-2 text-xs text-gray-500">
          หมายเหตุ: "ยอดขายในเดือนนี้" = ยอดขายที่ต้องเสียภาษี + ยอดขายอัตรา 0 (ดึงจาก Express ISVAT) ·
          ยอดขายยกเว้นที่ไม่ผ่าน VAT ไม่อยู่ใน ISVAT ให้ตรวจ/บวกเอง · ค่าทุกช่องตรงกับ ภ.พ.30 รายเดือนในแท็บแรก
          <br />
          <span className="font-medium">ไฟล์โอนย้าย (.txt):</span> สำหรับอัปโหลดหน้า "โอนย้ายข้อมูล ภ.พ.30" ของกรมสรรพากร
          (1 สาขา = 1 แถว, คั่นด้วย | + มีหัวคอลัมน์) — ที่หน้าเว็บเลือก "แบ่งด้วยสัญลักษณ์ = |", เปิด "บรรทัดแรกคือชื่อคอลัมน์"
          แล้วแมพคอลัมน์ตามชื่อหัวในไฟล์ (เดือน/สาขา/ประเภทยื่น กรอกบนฟอร์มเว็บ)
        </p>
      </Card>
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="mb-5">
      <p className="mb-2 text-sm font-semibold text-emerald-700">{title}</p>
      <div className="divide-y divide-gray-100">{children}</div>
    </div>
  )
}

function FieldRow({
  label, value, required, computed, hint,
}: { label: string; value: number; required?: boolean; computed?: boolean; hint?: string }) {
  return (
    <div className="flex items-center justify-between py-1.5">
      <div className="text-sm text-gray-700">
        {label}
        {required && <span className="ml-0.5 text-red-500">*</span>}
        {hint && <span className="ml-2 text-xs text-gray-400">({hint})</span>}
      </div>
      <div className={`w-40 rounded px-3 py-1.5 text-right font-mono text-sm ${computed ? 'bg-slate-100 text-slate-500' : 'bg-blue-50 font-semibold text-slate-800'}`}>
        {fmt(value)}
      </div>
    </div>
  )
}
