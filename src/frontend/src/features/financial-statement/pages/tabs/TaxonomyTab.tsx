import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../../shared/components/ui/ExportMenu'
import type { ExportSection } from '../../../../shared/utils/exportTable'
import { useStatementTaxonomy } from '../../hooks/useFinancialStatement'
import type { StatementTaxonomyLine } from '../../types/financialStatement.types'

const SECTION_ORDER = ['A', 'L', 'E', 'I', 'X'] as const
const SECTION_LABEL: Record<string, string> = {
  A: 'สินทรัพย์',
  L: 'หนี้สิน',
  E: 'ส่วนของเจ้าของ',
  I: 'รายได้',
  X: 'ค่าใช้จ่าย',
}

interface Props {
  companyId: number
}

export default function TaxonomyTab({ companyId }: Props) {
  const { data, isLoading, isError } = useStatementTaxonomy(companyId)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  if (isError) return <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>
  if (isLoading || !data) return <StateMessage>กำลังโหลด...</StateMessage>

  const bySection = SECTION_ORDER.map((sec) => ({
    section: sec,
    lines: data.lines.filter((l) => l.section === sec),
  })).filter((g) => g.lines.length > 0)

  const exportSections = (): ExportSection[] => [
    {
      name: 'ผังมาตรฐาน DBD',
      columns: [
        { key: 'section', header: 'หมวด' },
        { key: 'refCode', header: 'รหัสกลุ่ม' },
        { key: 'lineName', header: 'ชื่อบรรทัดในงบ' },
        { key: 'mappedAccountCount', header: 'จำนวนบัญชีที่ map', align: 'right' },
      ],
      rows: data.lines.map((l) => ({ ...l, section: SECTION_LABEL[l.section] ?? l.section })),
    },
  ]

  return (
    <div>
      <div className="mb-3 rounded-lg border border-sky-200 bg-sky-50 px-4 py-2 text-xs text-sky-800">
        📚 ผังมาตรฐานงบการเงิน (รหัสกลุ่ม DBD/NPAE) — เป็น <b>master taxonomy ใช้ร่วมทุกบริษัท</b> และเป็นชุดรหัสที่
        การแมพบัญชี (แท็บ "จัดการ Mapping") ต้องอ้างอิง. ตัวเลข "จำนวนบัญชีที่ map" คือของบริษัทที่เลือกไว้
      </div>

      <div className="mb-4 grid grid-cols-3 gap-3">
        <SummaryCard label="บรรทัดมาตรฐานทั้งหมด" value={`${data.totalLines}`} />
        <SummaryCard label="บรรทัดที่มีบัญชี map" value={`${data.usedLines} / ${data.totalLines}`} />
        <SummaryCard label="บัญชีที่ map รวม" value={`${data.mappedAccounts}`} />
      </div>

      <Card className="overflow-hidden">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <p className="text-sm font-semibold text-slate-800">รหัสกลุ่มมาตรฐาน (group-code taxonomy)</p>
          <ExportMenu
            meta={{ title: 'ผังมาตรฐานงบการเงิน (DBD taxonomy)', fileName: `statement-taxonomy-${companyId}` }}
            getSections={exportSections}
          />
        </div>
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-xs text-gray-600">
            <tr>
              <th className="px-4 py-2 text-left font-medium w-24">รหัสกลุ่ม</th>
              <th className="px-4 py-2 text-left font-medium">ชื่อบรรทัดในงบ</th>
              <th className="px-4 py-2 text-right font-medium w-40">บัญชีที่ map (บริษัทนี้)</th>
            </tr>
          </thead>
          <tbody>
            {bySection.map((g) => (
              <SectionRows key={g.section} section={g.section} lines={g.lines} />
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  )
}

function SectionRows({ section, lines }: { section: string; lines: StatementTaxonomyLine[] }) {
  return (
    <>
      <tr className="bg-slate-100/70">
        <td colSpan={3} className="px-4 py-1.5 text-xs font-semibold text-slate-600">
          {SECTION_LABEL[section] ?? section}
        </td>
      </tr>
      {lines.map((l) => (
        <tr key={l.refCode} className="border-b border-gray-100 hover:bg-slate-50">
          <td className="px-4 py-2 font-mono font-medium text-slate-600">{l.refCode}</td>
          <td className="px-4 py-2 text-gray-700">{l.lineName}</td>
          <td className="px-4 py-2 text-right">
            {l.mappedAccountCount > 0 ? (
              <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700">
                {l.mappedAccountCount} บัญชี
              </span>
            ) : (
              <span className="text-xs text-gray-300">ยังไม่มีบัญชี</span>
            )}
          </td>
        </tr>
      ))}
    </>
  )
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white px-4 py-3">
      <p className="text-[11px] text-gray-500">{label}</p>
      <p className="mt-1 text-lg font-bold text-slate-700">{value}</p>
    </div>
  )
}
