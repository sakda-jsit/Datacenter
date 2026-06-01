import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import DataTable, { type DataTableColumn } from '../../../shared/components/table/DataTable'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Pagination from '../../../shared/components/ui/Pagination'
import StateMessage from '../../../shared/components/ui/StateMessage'
import StatusBadge from '../../../shared/components/ui/StatusBadge'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import { useClientList } from '../../clients/hooks/useClients'
import { useImportHistory, useStartExpressImport } from '../hooks/useImport'
import type { ImportBatchListDto, ImportStatus } from '../types/import.types'

const STATUS_LABEL: Record<ImportStatus, string> = {
  Pending: 'รอดำเนินการ',
  Running: 'กำลังนำเข้า',
  Success: 'สำเร็จ',
  Failed: 'มีข้อผิดพลาด',
  Cancelled: 'ยกเลิก',
}

const STATUS_TONE: Record<ImportStatus, 'yellow' | 'blue' | 'green' | 'red' | 'gray'> = {
  Pending: 'yellow',
  Running: 'blue',
  Success: 'green',
  Failed: 'red',
  Cancelled: 'gray',
}

export default function ImportPage() {
  const { companyId } = useCurrentCompany()
  const [filterYear, setFilterYear] = useState<number | undefined>()
  const [page, setPage] = useState(1)
  const [showForm, setShowForm] = useState(false)
  const [formYear, setFormYear] = useState(String(new Date().getFullYear()))

  const { data: clients } = useClientList({ pageSize: 200 })
  const selectedClient = clients?.items.find((client) => client.id === companyId)
  const { data: history, isLoading } = useImportHistory({
    clientCompanyId: companyId || undefined,
    fiscalYear: filterYear,
    pageNumber: page,
    pageSize: 20,
  })
  const startImport = useStartExpressImport()

  useEffect(() => {
    setPage(1)
  }, [companyId])

  async function handleStartImport(e: React.FormEvent) {
    e.preventDefault()
    if (!companyId) return

    try {
      await startImport.mutateAsync({
        clientCompanyId: companyId,
        fiscalYear: Number(formYear),
      })
      setShowForm(false)
    } catch {
      // Mutation state shows the error.
    }
  }

  const columns: DataTableColumn<ImportBatchListDto>[] = [
    {
      key: 'client',
      header: 'บริษัท',
      render: (batch) => (
        <>
          <span className="font-mono text-xs text-slate-700">{batch.clientCode}</span>
          <span className="ml-1 text-xs text-gray-500">{batch.clientName}</span>
        </>
      ),
      sortValue: (batch) => batch.clientName,
      sortable: true,
    },
    {
      key: 'fiscalYear',
      header: 'ปีบัญชี',
      render: (batch) => <span className="font-mono">{batch.fiscalYear}</span>,
      sortValue: (batch) => batch.fiscalYear,
      sortable: true,
    },
    {
      key: 'importType',
      header: 'ประเภท',
      render: (batch) => <span className="text-gray-600">{batch.importType}</span>,
      sortValue: (batch) => batch.importType,
      sortable: true,
    },
    {
      key: 'status',
      header: 'สถานะ',
      render: (batch) => (
        <StatusBadge tone={STATUS_TONE[batch.status]}>
          {STATUS_LABEL[batch.status]}
        </StatusBadge>
      ),
      sortValue: (batch) => batch.status,
      sortable: true,
    },
    {
      key: 'totalRows',
      header: 'รวม',
      align: 'right',
      render: (batch) => <span className="font-mono">{batch.totalRows.toLocaleString()}</span>,
      sortValue: (batch) => batch.totalRows,
      sortable: true,
    },
    {
      key: 'successRows',
      header: 'สำเร็จ',
      align: 'right',
      render: (batch) => <span className="font-mono text-green-700">{batch.successRows.toLocaleString()}</span>,
      sortValue: (batch) => batch.successRows,
      sortable: true,
    },
    {
      key: 'errorRows',
      header: 'ผิดพลาด',
      align: 'right',
      render: (batch) => (
        <span className="font-mono text-red-600">
          {batch.errorRows > 0 ? batch.errorRows.toLocaleString() : '-'}
        </span>
      ),
      sortValue: (batch) => batch.errorRows,
      sortable: true,
    },
    {
      key: 'createdBy',
      header: 'นำเข้าโดย',
      render: (batch) => <span className="text-xs text-gray-500">{batch.createdBy}</span>,
      sortValue: (batch) => batch.createdBy,
      sortable: true,
    },
    {
      key: 'createdAt',
      header: 'เวลา',
      render: (batch) => <span className="whitespace-nowrap text-xs text-gray-500">{formatDateTime(batch.createdAt)}</span>,
      sortValue: (batch) => batch.createdAt,
      sortable: true,
    },
    {
      key: 'actions',
      header: '',
      align: 'right',
      render: (batch) => <ImportBatchAction batch={batch} />,
    },
  ]

  return (
    <div>
      <PageHeader
        title="นำเข้าข้อมูล"
        action={(
          <Button type="button" onClick={() => setShowForm((value) => !value)}>
            + นำเข้าข้อมูลใหม่
          </Button>
        )}
      />

      {showForm && (
        <Card className="mb-6 border-l-4 border-slate-500 p-5">
          <h2 className="mb-4 font-semibold text-slate-800">นำเข้าข้อมูลจาก Express DBF</h2>
          <form onSubmit={handleStartImport} className="flex flex-wrap items-end gap-4">
            <div>
              <p className="mb-1 text-sm font-medium text-gray-700">บริษัทลูกค้า *</p>
              <p className="min-w-64 rounded border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700">
                {selectedClient ? `${selectedClient.code} — ${selectedClient.name}` : 'เลือกบริษัทที่ header ก่อน'}
              </p>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">ปีบัญชี (AD) *</label>
              <input
                type="number"
                value={formYear}
                onChange={(e) => setFormYear(e.target.value)}
                min={2000}
                max={2100}
                required
                className="w-32 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </div>

            <div className="flex gap-2">
              <Button type="submit" disabled={!companyId || startImport.isPending}>
                {startImport.isPending ? 'กำลังนำเข้า...' : 'เริ่มนำเข้า'}
              </Button>
              <Button type="button" variant="secondary" onClick={() => setShowForm(false)}>
                ยกเลิก
              </Button>
            </div>
          </form>
          {startImport.isError && (
            <div className="mt-2">
              <StateMessage tone="error">เกิดข้อผิดพลาด: กรุณาตรวจสอบการตั้งค่า Express path</StateMessage>
            </div>
          )}
        </Card>
      )}

      <div className="mb-4 flex gap-3">
        <input
          type="number"
          placeholder="ปีบัญชี"
          value={filterYear ?? ''}
          onChange={(e) => { setFilterYear(e.target.value ? Number(e.target.value) : undefined); setPage(1) }}
          className="w-28 rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
        />
      </div>

      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {history && (
        <>
          <DataTable
            rows={history.items}
            columns={columns}
            getRowKey={(batch) => batch.id}
            emptyMessage="ยังไม่มีประวัติการนำเข้า"
            defaultSortKey="createdAt"
            defaultSortDirection="desc"
          />

          <Pagination
            page={page}
            totalPages={history.totalPages}
            totalCount={history.totalCount}
            onPageChange={setPage}
          />
        </>
      )}
    </div>
  )
}

function ImportBatchAction({ batch }: { batch: ImportBatchListDto }) {
  if (batch.errorRows > 0) {
    return (
      <Link to={`/import/${batch.id}/validation`} className="text-xs font-medium text-red-600 hover:text-red-800">
        ดูข้อผิดพลาด
      </Link>
    )
  }

  if (batch.status === 'Success') {
    return (
      <Link to={`/import/${batch.id}/validation`} className="text-xs font-medium text-blue-600 hover:text-blue-800">
        รายละเอียด
      </Link>
    )
  }

  return null
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('th-TH', {
    day: '2-digit',
    month: '2-digit',
    year: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}
