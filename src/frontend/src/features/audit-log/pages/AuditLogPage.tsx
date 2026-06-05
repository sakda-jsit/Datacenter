import { useMemo, useState } from 'react'
import DataTable from '../../../shared/components/table/DataTable'
import type { DataTableColumn } from '../../../shared/components/table/DataTable'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Pagination from '../../../shared/components/ui/Pagination'
import SearchInput from '../../../shared/components/ui/SearchInput'
import StateMessage from '../../../shared/components/ui/StateMessage'
import StatusBadge from '../../../shared/components/ui/StatusBadge'
import ExportMenu from '../../../shared/components/ui/ExportMenu'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useAuditLogs } from '../hooks/useAuditLog'
import type { AuditLogDto } from '../types/auditLog.types'
import type { ExportSection } from '../../../shared/utils/exportTable'

const PAGE_SIZE = 20

// จับคู่ action กับโทนสีของ badge ให้สื่อความหมาย
function actionTone(action: string): 'gray' | 'blue' | 'green' | 'red' | 'yellow' {
  const a = action.toLowerCase()
  if (a.includes('close') || a.includes('lock')) return 'green'
  if (a.includes('reopen') || a.includes('delete')) return 'red'
  if (a.includes('import') || a.includes('generate')) return 'blue'
  if (a.includes('update') || a.includes('assign')) return 'yellow'
  return 'gray'
}

function fmtDateTime(iso: string) {
  return new Date(iso).toLocaleString('th-TH', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit',
  })
}

export default function AuditLogPage() {
  const { companyId } = useCurrentCompany()
  const [search, setSearch] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [scopeToCompany, setScopeToCompany] = useState(false)
  const [page, setPage] = useState(1)

  const params = useMemo(
    () => ({
      pageNumber: page,
      pageSize: PAGE_SIZE,
      search: search.trim() || undefined,
      fromDate: fromDate || undefined,
      toDate: toDate || undefined,
      clientCompanyId: scopeToCompany && companyId > 0 ? companyId : undefined,
    }),
    [page, search, fromDate, toDate, scopeToCompany, companyId],
  )

  const { data, isLoading, isError } = useAuditLogs(params)

  function resetToFirstPage() {
    setPage(1)
  }

  const columns: DataTableColumn<AuditLogDto>[] = [
    {
      key: 'createdAt',
      header: 'เวลา',
      className: 'whitespace-nowrap text-xs text-slate-500',
      render: (r) => fmtDateTime(r.createdAt),
    },
    {
      key: 'username',
      header: 'ผู้ใช้',
      className: 'whitespace-nowrap text-slate-700',
      render: (r) => r.username || '—',
    },
    {
      key: 'action',
      header: 'การกระทำ',
      render: (r) => <StatusBadge tone={actionTone(r.action)}>{r.action}</StatusBadge>,
    },
    {
      key: 'entity',
      header: 'รายการ',
      className: 'text-slate-600',
      render: (r) => (
        <span>
          {r.entityName}
          {r.entityId && <span className="ml-1 font-mono text-xs text-slate-400">#{r.entityId}</span>}
        </span>
      ),
    },
    {
      key: 'company',
      header: 'บริษัท',
      className: 'text-slate-600',
      render: (r) => r.clientName ?? '—',
    },
    {
      key: 'change',
      header: 'การเปลี่ยนแปลง',
      className: 'text-xs text-slate-500',
      render: (r) =>
        r.beforeValue || r.afterValue ? (
          <span>
            <span className="text-red-500">{r.beforeValue ?? '—'}</span>
            {' → '}
            <span className="text-green-600">{r.afterValue ?? '—'}</span>
          </span>
        ) : (
          '—'
        ),
    },
  ]

  const exportSections = (): ExportSection[] => [{
    name: 'ประวัติการใช้งาน',
    columns: [
      { key: 'createdAt', header: 'เวลา', value: (r) => fmtDateTime(r.createdAt) },
      { key: 'username', header: 'ผู้ใช้', value: (r) => r.username || '' },
      { key: 'action', header: 'การกระทำ' },
      { key: 'entityName', header: 'รายการ' },
      { key: 'entityId', header: 'รหัส' },
      { key: 'clientName', header: 'บริษัท', value: (r) => r.clientName ?? '' },
      { key: 'beforeValue', header: 'ก่อน', value: (r) => r.beforeValue ?? '' },
      { key: 'afterValue', header: 'หลัง', value: (r) => r.afterValue ?? '' },
    ],
    rows: data?.items ?? [],
  }]

  return (
    <div>
      <PageHeader
        title="ประวัติการใช้งาน"
        description="Audit log การกระทำสำคัญทั้งหมดในระบบ"
        action={data && data.items.length > 0 ? (
          <ExportMenu
            meta={{ title: 'ประวัติการใช้งาน (Audit Log)', subtitle: `หน้า ${data.pageNumber}/${data.totalPages}`, fileName: 'audit-log' }}
            getSections={exportSections}
          />
        ) : undefined}
      />

      <div className="mb-4 flex flex-wrap items-end gap-3 rounded-lg bg-white p-4 shadow">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">ค้นหา (ผู้ใช้ / รหัสรายการ)</label>
          <SearchInput
            placeholder="ค้นหา..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); resetToFirstPage() }}
            className="w-64"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">ตั้งแต่วันที่</label>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => { setFromDate(e.target.value); resetToFirstPage() }}
            className="rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">ถึงวันที่</label>
          <input
            type="date"
            value={toDate}
            onChange={(e) => { setToDate(e.target.value); resetToFirstPage() }}
            className="rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
          />
        </div>
        <label className="flex items-center gap-2 pb-2 text-sm text-gray-600">
          <input
            type="checkbox"
            checked={scopeToCompany}
            onChange={(e) => { setScopeToCompany(e.target.checked); resetToFirstPage() }}
            disabled={companyId <= 0}
            className="rounded"
          />
          เฉพาะบริษัทปัจจุบัน
        </label>
      </div>

      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาดในการโหลดข้อมูล</StateMessage>}

      {data && (
        <>
          <DataTable
            rows={data.items}
            columns={columns}
            getRowKey={(r) => r.id}
            emptyMessage="ไม่พบประวัติการใช้งาน"
          />
          <Pagination
            page={data.pageNumber}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}
    </div>
  )
}
