import { useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import Button from '../../../../shared/components/ui/Button'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import NoteTemplateEditorModal from '../../components/NoteTemplateEditorModal'
import { financialStatementApi } from '../../services/financialStatementApi'
import { useNotesToFs } from '../../hooks/useFinancialStatement'
import type {
  NotesToFsDto, NoteScheduleDto, NoteMovementDto, NoteCostOfSalesDto, NoteNarrativeDto,
} from '../../types/financialStatement.types'
import type { ExportSection } from '../../../../shared/utils/exportTable'

function fmt(n: number) {
  if (n === 0) return '—'
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
  queried: boolean
}

export default function NotesTab({ companyId, fiscalYear, queried }: Props) {
  const [showEditor, setShowEditor] = useState(false)
  const [exporting, setExporting] = useState(false)
  const { data, isLoading, isError } = useNotesToFs({ clientCompanyId: companyId, fiscalYear }, queried)

  async function downloadExcel() {
    setExporting(true)
    try {
      const blob = await financialStatementApi.getNotesExcel({ clientCompanyId: companyId, fiscalYear })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `NOTE2-${companyId}-${fiscalYear}.xlsx`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      setTimeout(() => URL.revokeObjectURL(url), 1000)
    } finally {
      setExporting(false)
    }
  }

  if (!queried) return <Card><StateMessage centered>เลือกบริษัทและปีบัญชี แล้วกด "แสดงรายงาน"</StateMessage></Card>
  if (isLoading) return <StateMessage>กำลังคำนวณหมายเหตุประกอบงบ...</StateMessage>
  if (isError) return <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
  if (!data) return <Card><StateMessage centered>ไม่มีข้อมูล</StateMessage></Card>

  const yTh = data.fiscalYear + 543
  const pTh = data.priorYear + 543

  // รวมทุกหมายเหตุเป็นลำดับเดียวตาม sortOrder
  type Item =
    | { sort: number; kind: 'narrative'; v: NoteNarrativeDto }
    | { sort: number; kind: 'schedule'; v: NoteScheduleDto }
    | { sort: number; kind: 'movement'; v: NoteMovementDto }
    | { sort: number; kind: 'cos'; v: NoteCostOfSalesDto }
  const items: Item[] = [
    ...data.narratives.map((v) => ({ sort: v.sortOrder, kind: 'narrative' as const, v })),
    ...data.schedules.map((v) => ({ sort: v.sortOrder, kind: 'schedule' as const, v })),
    ...data.movements.map((v) => ({ sort: v.sortOrder, kind: 'movement' as const, v })),
    ...(data.costOfSales ? [{ sort: data.costOfSales.sortOrder, kind: 'cos' as const, v: data.costOfSales }] : []),
  ].sort((a, b) => a.sort - b.sort)

  return (
    <div>
      <Card className="mb-4 flex items-start justify-between px-6 py-4">
        <div>
          <p className="text-lg font-semibold text-slate-800">{data.clientName}</p>
          <p className="text-sm text-gray-500">หมายเหตุประกอบงบการเงิน — {data.periodLabel}</p>
          <p className="mt-1 text-xs text-gray-400">
            เลขประจำตัวผู้เสียภาษี {data.taxId || '—'} · ตัวเลขในข้อ 6 ดึงจากงบโดยอัตโนมัติ (แก้ไม่ได้)
          </p>
        </div>
        <div className="flex gap-2">
          <Button type="button" variant="secondary" onClick={() => setShowEditor(true)}>แก้ไขข้อความ</Button>
          <Button type="button" onClick={downloadExcel} disabled={exporting}>
            {exporting ? 'กำลังสร้าง...' : 'ส่งออก Excel (รูปแบบงบ)'}
          </Button>
          <ExportMenu
            meta={{
              title: `หมายเหตุประกอบงบการเงิน ปี ${yTh}`,
              subtitle: data.clientName,
              fileName: `notes-${data.clientCompanyId}-${data.fiscalYear}`,
            }}
            getSections={() => buildExportSections(data, yTh, pTh)}
          />
        </div>
      </Card>

      <div className="space-y-4">
        {items.map((it) => {
          if (it.kind === 'narrative') return <NarrativeBlock key={`n${it.v.noteNo}`} v={it.v} />
          if (it.kind === 'schedule') return <ScheduleBlock key={`s${it.v.noteNo}`} v={it.v} yTh={yTh} pTh={pTh} />
          if (it.kind === 'movement') return <MovementBlock key={`m${it.v.noteNo}`} v={it.v} yTh={yTh} pTh={pTh} />
          return <CostOfSalesBlock key={`c${it.v.noteNo}`} v={it.v} yTh={yTh} pTh={pTh} />
        })}
      </div>

      {showEditor && (
        <NoteTemplateEditorModal companyId={companyId} fiscalYear={fiscalYear} onClose={() => setShowEditor(false)} />
      )}
    </div>
  )
}

// ── Blocks ────────────────────────────────────────────────────────────────────

function NarrativeBlock({ v }: { v: NoteNarrativeDto }) {
  return (
    <Card className="px-6 py-4">
      <p className="mb-1 text-sm font-semibold text-slate-800">
        {v.noteNo}. {v.title}
        {v.isCompanyOverride && <span className="ml-2 rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-normal text-amber-700">แก้ไขเฉพาะบริษัท</span>}
      </p>
      <div className="space-y-1.5 text-xs leading-relaxed text-gray-700">
        {v.body.split('\n').map((p, i) => <p key={i}>{p}</p>)}
      </div>
    </Card>
  )
}

function YearHead({ yTh, pTh }: { yTh: number; pTh: number }) {
  return (
    <thead className="bg-slate-50 text-gray-600">
      <tr>
        <th className="px-3 py-2 text-left font-medium" />
        <th className="px-3 py-2 text-right font-medium w-40">{yTh}</th>
        <th className="px-3 py-2 text-right font-medium w-40">{pTh}</th>
      </tr>
    </thead>
  )
}

function ScheduleBlock({ v, yTh, pTh }: { v: NoteScheduleDto; yTh: number; pTh: number }) {
  return (
    <Card className="overflow-x-auto">
      <BlockHead noteNo={v.noteNo} title={v.title} />
      <table className="w-full text-xs">
        <YearHead yTh={yTh} pTh={pTh} />
        <tbody>
          {v.rows.map((r, i) => (
            <tr key={i} className="border-t border-gray-100">
              <td className="px-3 py-1.5 text-gray-700">{r.label}</td>
              <td className="px-3 py-1.5 text-right font-mono">{fmt(r.currentYear)}</td>
              <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.priorYear)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
            <td className="px-3 py-2">รวม</td>
            <td className="px-3 py-2 text-right font-mono">{fmt(v.totalCurrent)}</td>
            <td className="px-3 py-2 text-right font-mono text-gray-600">{fmt(v.totalPrior)}</td>
          </tr>
        </tfoot>
      </table>
    </Card>
  )
}

function CostOfSalesBlock({ v, yTh, pTh }: { v: NoteCostOfSalesDto; yTh: number; pTh: number }) {
  return (
    <Card className="overflow-x-auto">
      <BlockHead noteNo={v.noteNo} title={v.title} />
      <table className="w-full text-xs">
        <YearHead yTh={yTh} pTh={pTh} />
        <tbody>
          {v.components.map((r, i) => (
            <tr key={i} className="border-t border-gray-100">
              <td className="px-3 py-1.5 text-gray-700">{r.label}</td>
              <td className="px-3 py-1.5 text-right font-mono">{fmt(r.currentYear)}</td>
              <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.priorYear)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
            <td className="px-3 py-2">รวมต้นทุน</td>
            <td className="px-3 py-2 text-right font-mono">{fmt(v.totalCurrent)}</td>
            <td className="px-3 py-2 text-right font-mono text-gray-600">{fmt(v.totalPrior)}</td>
          </tr>
        </tfoot>
      </table>
      <div className="border-t border-gray-100 px-3 py-2 text-[11px] text-gray-500">
        ข้อมูลประกอบ — สินค้าคงเหลือต้นงวด {fmt(v.openingInventoryCurrent)} / {fmt(v.openingInventoryPrior)} ·
        ปลายงวด {fmt(v.closingInventoryCurrent)} / {fmt(v.closingInventoryPrior)}
      </div>
    </Card>
  )
}

function MovementBlock({ v, yTh, pTh }: { v: NoteMovementDto; yTh: number; pTh: number }) {
  return (
    <Card className="overflow-x-auto">
      <BlockHead noteNo={v.noteNo} title={v.title} />
      <table className="w-full text-xs">
        <thead className="bg-slate-50 text-gray-600">
          <tr>
            <th className="px-3 py-2 text-left font-medium" />
            <th className="px-3 py-2 text-right font-medium">ยอดต้นปี</th>
            <th className="px-3 py-2 text-right font-medium">เพิ่มขึ้น</th>
            <th className="px-3 py-2 text-right font-medium">ลดลง</th>
            <th className="px-3 py-2 text-right font-medium">ยอดปลายปี</th>
          </tr>
        </thead>
        <tbody>
          <SubHeader label="ราคาทุน" />
          {v.costRows.map((r, i) => <MovRow key={`c${i}`} r={r} />)}
          <MovRow r={v.costTotal} bold />
          <SubHeader label="หักค่าเสื่อมราคาสะสม" />
          {v.accumRows.map((r, i) => <MovRow key={`a${i}`} r={r} />)}
          <MovRow r={v.accumTotal} bold />
        </tbody>
        <tfoot>
          <tr className="border-t-2 border-slate-200 bg-slate-50 font-semibold">
            <td className="px-3 py-2">มูลค่าสุทธิ (ต้นปี {yTh} / {pTh})</td>
            <td className="px-3 py-2 text-right font-mono" colSpan={3}>—</td>
            <td className="px-3 py-2 text-right font-mono">{fmt(v.netClosing)}</td>
          </tr>
          <tr className="bg-slate-50">
            <td className="px-3 py-1.5 text-gray-600">ค่าเสื่อมราคา/ค่าตัดจำหน่ายสำหรับปี {yTh}</td>
            <td className="px-3 py-1.5 text-right font-mono" colSpan={3} />
            <td className="px-3 py-1.5 text-right font-mono">{fmt(v.chargeForYear)}</td>
          </tr>
        </tfoot>
      </table>
    </Card>
  )
}

function MovRow({ r, bold }: { r: { label: string; opening: number; additions: number; disposals: number; closing: number }; bold?: boolean }) {
  const cls = bold ? 'border-t border-slate-200 bg-slate-50/60 font-semibold' : 'border-t border-gray-100'
  return (
    <tr className={cls}>
      <td className="px-3 py-1.5 text-gray-700">{r.label}</td>
      <td className="px-3 py-1.5 text-right font-mono">{fmt(r.opening)}</td>
      <td className="px-3 py-1.5 text-right font-mono">{fmt(r.additions)}</td>
      <td className="px-3 py-1.5 text-right font-mono">{fmt(r.disposals)}</td>
      <td className="px-3 py-1.5 text-right font-mono">{fmt(r.closing)}</td>
    </tr>
  )
}

function SubHeader({ label }: { label: string }) {
  return (
    <tr className="bg-white">
      <td className="px-3 pt-2 pb-0.5 text-[11px] font-semibold text-slate-500" colSpan={5}>{label}</td>
    </tr>
  )
}

function BlockHead({ noteNo, title }: { noteNo: string; title: string }) {
  return (
    <div className="flex items-center justify-between border-b px-4 py-2.5">
      <p className="text-sm font-semibold text-slate-800">{noteNo} {title}</p>
      <span className="text-[11px] text-gray-400">หน่วย: บาท</span>
    </div>
  )
}

// ── Export ──────────────────────────────────────────────────────────────────

function buildExportSections(data: NotesToFsDto, yTh: number, pTh: number): ExportSection[] {
  const cols = [
    { key: 'label', header: 'รายการ' },
    { key: 'cur', header: String(yTh), align: 'right' as const },
    { key: 'pri', header: String(pTh), align: 'right' as const },
  ]
  const sections: ExportSection[] = []

  for (const s of data.schedules) {
    sections.push({
      name: `${s.noteNo} ${s.title}`,
      columns: cols,
      rows: [
        ...s.rows.map((r) => ({ label: r.label, cur: r.currentYear, pri: r.priorYear })),
        { label: 'รวม', cur: s.totalCurrent, pri: s.totalPrior },
      ],
    })
  }
  if (data.costOfSales) {
    const c = data.costOfSales
    sections.push({
      name: `${c.noteNo} ${c.title}`,
      columns: cols,
      rows: [
        ...c.components.map((r) => ({ label: r.label, cur: r.currentYear, pri: r.priorYear })),
        { label: 'รวมต้นทุน', cur: c.totalCurrent, pri: c.totalPrior },
      ],
    })
  }
  for (const m of data.movements) {
    sections.push({
      name: `${m.noteNo} ${m.title}`,
      columns: [
        { key: 'label', header: 'รายการ' },
        { key: 'opening', header: 'ยอดต้นปี', align: 'right' as const },
        { key: 'additions', header: 'เพิ่มขึ้น', align: 'right' as const },
        { key: 'disposals', header: 'ลดลง', align: 'right' as const },
        { key: 'closing', header: 'ยอดปลายปี', align: 'right' as const },
      ],
      rows: [
        { label: 'ราคาทุน', opening: '', additions: '', disposals: '', closing: '' },
        ...m.costRows,
        { ...m.costTotal },
        { label: 'หักค่าเสื่อมราคาสะสม', opening: '', additions: '', disposals: '', closing: '' },
        ...m.accumRows,
        { ...m.accumTotal },
        { label: `มูลค่าสุทธิสิ้นปี`, opening: '', additions: '', disposals: '', closing: m.netClosing },
      ],
    })
  }
  return sections
}
