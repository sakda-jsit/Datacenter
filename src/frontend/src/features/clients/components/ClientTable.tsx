import { Link } from 'react-router-dom'
import DataTable, { type DataTableColumn } from '../../../shared/components/table/DataTable'
import StatusBadge from '../../../shared/components/ui/StatusBadge'
import type { ClientListDto } from '../types/client.types'

interface ClientTableProps {
  clients: ClientListDto[]
  deactivatePending: boolean
  onDeactivate: (id: number, name: string) => void
}

export default function ClientTable({ clients, deactivatePending, onDeactivate }: ClientTableProps) {
  const columns: DataTableColumn<ClientListDto>[] = [
    {
      key: 'name',
      header: 'ชื่อบริษัท',
      render: (client) => <span className="text-gray-800">{client.name}</span>,
      sortValue: (client) => client.name,
      sortable: true,
    },
    {
      key: 'taxId',
      header: 'เลขประจำตัวผู้เสียภาษี',
      render: (client) => <span className="font-mono text-gray-600">{client.taxId}</span>,
      sortValue: (client) => client.taxId,
      sortable: true,
    },
    {
      key: 'isActive',
      header: 'สถานะ',
      render: (client) => (
        <StatusBadge tone={client.isActive ? 'green' : 'gray'}>
          {client.isActive ? 'ใช้งาน' : 'ปิดใช้งาน'}
        </StatusBadge>
      ),
      sortValue: (client) => client.isActive,
      sortable: true,
    },
    {
      key: 'actions',
      header: '',
      align: 'right',
      render: (client) => (
        <div className="space-x-3">
          <Link
            to={`/clients/${client.id}/edit`}
            className="text-blue-600 hover:text-blue-800 text-xs font-medium"
          >
            แก้ไข
          </Link>
          {client.isActive && (
            <button
              onClick={() => onDeactivate(client.id, client.name)}
              disabled={deactivatePending}
              className="text-red-500 hover:text-red-700 text-xs font-medium disabled:opacity-50"
            >
              ปิดใช้งาน
            </button>
          )}
        </div>
      ),
    },
  ]

  return (
    <DataTable
      rows={clients}
      columns={columns}
      getRowKey={(client) => client.id}
      defaultSortKey="name"
      emptyMessage="ไม่พบข้อมูล"
    />
  )
}
