import { useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Card from '../../../shared/components/ui/Card'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import ExportMenu from '../../../shared/components/ui/ExportMenu'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import {
  useCreateReportPackage,
  useDeleteReportPackage,
  useReportPackages,
  useSetReportPackageStatus,
} from '../hooks/useReportPackages'
import { RP_STATUS, RP_STATUS_CLASS, RP_STATUS_LABEL } from '../types/reportPackage.types'
import type { ReportPackage } from '../types/reportPackage.types'
import type { ExportSection } from '../../../shared/utils/exportTable'

function fmt(n?: number) {
  return n == null ? '—' : n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function fmtDt(s?: string) {
  return s ? new Date(s).toLocaleString('th-TH', { dateStyle: 'short', timeStyle: 'short' }) : ''
}

function apiErr(err: unknown) {
  const m = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
  return m?.detail ?? m?.title ?? 'ดำเนินการไม่สำเร็จ'
}

export default function ReportPackagesPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const [newYear, setNewYear] = useState(currentYear)
  const [error, setError] = useState('')

  const { data, isLoading, isError } = useReportPackages(companyId)
  const create = useCreateReportPackage(companyId)
  const setStatus = useSetReportPackageStatus(companyId)
  const remove = useDeleteReportPackage(companyId)

  const rows = data ?? []

  async function run(fn: () => Promise<unknown>) {
    setError('')
    try { await fn() } catch (e) { setError(apiErr(e)) }
  }

  if (!companyId) {
    return (
      <div>
        <PageHeader title="ชุดรายงานงบการเงิน" />
        <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
      </div>
    )
  }

  return (
    <div>
      <PageHeader
        title="ชุดรายงานงบการเงิน"
        description="จัดเวอร์ชันงบการเงิน + เวิร์กโฟลว์ ร่าง → รอตรวจ → อนุมัติ → ล็อก (ยื่นแล้ว); finalize เก็บ snapshot ชื่อบริษัท + ยอดงบ"
      />

      <Card className="mb-5 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ปีบัญชี (AD)</label>
            <input
              type="number" value={newYear} min={2000} max={2100}
              onChange={(e) => setNewYear(Number(e.target.value))}
              className="w-28 rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <Button type="button" onClick={() => run(() => create.mutateAsync({ fiscalYear: newYear }))} disabled={create.isPending} className="self-end">
            {create.isPending ? 'กำลังสร้าง...' : '+ สร้างชุดรายงาน / เวอร์ชันใหม่'}
          </Button>
          <p className="self-end pb-2 text-xs text-gray-400">เปิดปีเดิมซ้ำ = ได้เวอร์ชันถัดไป (ยื่นเพิ่มเติม)</p>
        </div>
      </Card>

      {error && <StateMessage tone="error">{error}</StateMessage>}
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาด กรุณาลองใหม่</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {data && rows.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีชุดรายงาน — สร้างชุดแรกด้านบน</StateMessage></Card>
      )}

      {rows.length > 0 && (
        <Card className="overflow-x-auto">
          <div className="flex items-start justify-between border-b px-4 py-3">
            <p className="text-sm font-semibold text-slate-800">ชุดรายงานงบ ({rows.length})</p>
            <ExportMenu
              meta={{ title: 'ชุดรายงานงบการเงิน', fileName: `report-packages-${companyId}` }}
              getSections={(): ExportSection[] => [{
                name: 'ชุดรายงาน',
                columns: [
                  { key: 'fiscalYear', header: 'ปีงบ' },
                  { key: 'version', header: 'เวอร์ชัน' },
                  { key: 'status', header: 'สถานะ', value: (r) => RP_STATUS_LABEL[r.status] },
                  { key: 'totalAssets', header: 'สินทรัพย์', align: 'right' },
                  { key: 'totalEquity', header: 'ส่วนผู้ถือหุ้น', align: 'right' },
                  { key: 'netProfit', header: 'กำไรสุทธิ', align: 'right' },
                  { key: 'finalizedBy', header: 'ผู้อนุมัติ' },
                  { key: 'lockedBy', header: 'ผู้ล็อก' },
                ],
                rows,
              }]}
            />
          </div>
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-left font-medium">ปีงบ / เวอร์ชัน</th>
                <th className="px-3 py-2 text-left font-medium">สถานะ</th>
                <th className="px-3 py-2 text-right font-medium">สินทรัพย์</th>
                <th className="px-3 py-2 text-right font-medium">ส่วนผู้ถือหุ้น</th>
                <th className="px-3 py-2 text-right font-medium">กำไรสุทธิ</th>
                <th className="px-3 py-2 text-left font-medium">อนุมัติ / ล็อก</th>
                <th className="px-3 py-2 text-left font-medium">ดำเนินการ</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.id} className="border-t border-gray-100 align-top hover:bg-slate-50">
                  <td className="px-3 py-2">
                    <span className="font-semibold text-slate-800">ปี {r.fiscalYear}</span>
                    <span className="ml-1 rounded bg-slate-100 px-1.5 text-[10px] text-slate-500">v{r.version}</span>
                    {r.snapshotCompanyName && <span className="block text-[10px] text-gray-400">{r.snapshotCompanyName}</span>}
                  </td>
                  <td className="px-3 py-2">
                    <span className={`inline-block rounded px-2 py-0.5 text-[11px] ${RP_STATUS_CLASS[r.status]}`}>{RP_STATUS_LABEL[r.status]}</span>
                  </td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(r.totalAssets)}</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(r.totalEquity)}</td>
                  <td className="px-3 py-2 text-right font-mono">{fmt(r.netProfit)}</td>
                  <td className="px-3 py-2 text-[10px] text-gray-500">
                    {r.finalizedBy && <div>อนุมัติ: {r.finalizedBy} · {fmtDt(r.finalizedAt)}</div>}
                    {r.lockedBy && <div className="text-red-500">ล็อก: {r.lockedBy} · {fmtDt(r.lockedAt)}</div>}
                    {!r.finalizedBy && !r.lockedBy && '—'}
                  </td>
                  <td className="px-3 py-2">
                    <div className="flex flex-wrap gap-1">
                      <RowActions pkg={r} busy={setStatus.isPending || remove.isPending}
                        onStatus={(t) => run(() => setStatus.mutateAsync({ id: r.id, targetStatus: t }))}
                        onDelete={() => run(() => remove.mutateAsync(r.id))} />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}

function ActBtn({ label, tone, onClick, busy }: { label: string; tone?: 'primary' | 'secondary' | 'danger'; onClick: () => void; busy?: boolean }) {
  const cls = tone === 'danger' ? 'border-red-200 text-red-600 hover:bg-red-50'
    : tone === 'primary' ? 'border-sky-300 bg-sky-50 text-sky-700 hover:bg-sky-100'
    : 'border-gray-200 text-gray-600 hover:bg-gray-50'
  return (
    <button type="button" onClick={onClick} disabled={busy}
      className={`rounded border px-2 py-1 text-[11px] disabled:opacity-50 ${cls}`}>{label}</button>
  )
}

function RowActions({ pkg, onStatus, onDelete, busy }: {
  pkg: ReportPackage
  onStatus: (target: number) => void
  onDelete: () => void
  busy: boolean
}) {
  switch (pkg.status) {
    case RP_STATUS.Draft:
      return <>
        <ActBtn label="ส่งตรวจ" tone="primary" onClick={() => onStatus(RP_STATUS.Review)} busy={busy} />
        <ActBtn label="ลบ" tone="danger" onClick={onDelete} busy={busy} />
      </>
    case RP_STATUS.Review:
      return <>
        <ActBtn label="อนุมัติ (Final)" tone="primary" onClick={() => onStatus(RP_STATUS.Final)} busy={busy} />
        <ActBtn label="ตีกลับร่าง" onClick={() => onStatus(RP_STATUS.Draft)} busy={busy} />
      </>
    case RP_STATUS.Final:
      return <>
        <ActBtn label="ล็อก (ยื่นแล้ว)" tone="primary" onClick={() => onStatus(RP_STATUS.Locked)} busy={busy} />
        <ActBtn label="แก้ไข (กลับรอตรวจ)" onClick={() => onStatus(RP_STATUS.Review)} busy={busy} />
      </>
    case RP_STATUS.Locked:
      return <ActBtn label="ปลดล็อก" tone="danger" onClick={() => onStatus(RP_STATUS.Final)} busy={busy} />
    default:
      return null
  }
}
