import { useParams, Link } from 'react-router-dom'
import DataTable, { type DataTableColumn } from '../../../shared/components/table/DataTable'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useImportValidation } from '../hooks/useImport'
import type { ImportBatchDetailDto } from '../types/import.types'

export default function ImportValidationPage() {
  const { id } = useParams<{ id: string }>()
  const batchId = Number(id)
  const { data, isLoading, isError } = useImportValidation(batchId)

  if (isLoading) return <StateMessage>กำลังโหลด...</StateMessage>
  if (isError || !data) return <StateMessage tone="error">ไม่พบข้อมูลการนำเข้า</StateMessage>

  const passRate = data.totalRows > 0
    ? Math.round((data.validRows / data.totalRows) * 100)
    : 0

  const errorColumns: DataTableColumn<ImportBatchDetailDto>[] = [
    {
      key: 'rowNumber',
      header: 'แถวที่',
      render: (error) => <span className="font-mono text-gray-500">{error.rowNumber}</span>,
      sortValue: (error) => error.rowNumber,
      sortable: true,
      headerClassName: 'w-16',
    },
    {
      key: 'accountCode',
      header: 'รหัสบัญชี',
      render: (error) => <span className="font-mono text-slate-700">{error.accountCode ?? '—'}</span>,
      sortValue: (error) => error.accountCode ?? '',
      sortable: true,
      headerClassName: 'w-32',
    },
    {
      key: 'errorMessage',
      header: 'ข้อผิดพลาด',
      render: (error) => <span className="text-red-600">{error.errorMessage}</span>,
      sortValue: (error) => error.errorMessage ?? '',
      sortable: true,
    },
    {
      key: 'rawData',
      header: 'ข้อมูลดิบ',
      render: (error) => (
        <span className="block max-w-xs truncate font-mono text-xs text-gray-400" title={error.rawData}>
          {error.rawData}
        </span>
      ),
      sortValue: (error) => error.rawData,
      sortable: true,
    },
  ]

  return (
    <div>
      <PageHeader
        title={`ผลการตรวจสอบ Batch #${batchId}`}
        action={
          <Link to="/import" className="text-sm text-slate-500 hover:text-slate-700">
            ← กลับ
          </Link>
        }
      />

      {/* Summary Cards */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4 mb-6">
        <SummaryCard label="รวมทั้งหมด" value={data.totalRows.toLocaleString()} color="text-slate-700" />
        <SummaryCard label="ผ่านการตรวจ" value={data.validRows.toLocaleString()} color="text-green-600" />
        <SummaryCard label="มีข้อผิดพลาด" value={data.invalidRows.toLocaleString()} color="text-red-600" />
        <SummaryCard label="อัตราผ่าน" value={`${passRate}%`} color={passRate === 100 ? 'text-green-600' : 'text-yellow-600'} />
      </div>

      {data.invalidRows === 0 ? (
        <div className="rounded-lg border border-green-200 bg-green-50 p-6 text-center">
          <p className="text-green-700 font-medium text-lg">ข้อมูลผ่านการตรวจสอบทั้งหมด</p>
          <p className="text-green-600 text-sm mt-1">ไม่พบข้อผิดพลาด พร้อมใช้งาน</p>
        </div>
      ) : (
        <>
          <h2 className="font-semibold text-gray-700 mb-3">รายการที่มีข้อผิดพลาด ({data.invalidRows} รายการ)</h2>
          <DataTable
            rows={data.errors}
            columns={errorColumns}
            getRowKey={(error) => error.id}
            defaultSortKey="rowNumber"
            emptyMessage="ไม่พบข้อผิดพลาด"
            rowClassName={() => 'hover:bg-red-50'}
          />
        </>
      )}
    </div>
  )
}

function SummaryCard({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <Card className="p-4">
      <p className="text-xs text-gray-500 mb-1">{label}</p>
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
    </Card>
  )
}
