import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Button from '../../../shared/components/ui/Button'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Pagination from '../../../shared/components/ui/Pagination'
import SearchInput from '../../../shared/components/ui/SearchInput'
import StateMessage from '../../../shared/components/ui/StateMessage'
import ClientTable from '../components/ClientTable'
import ExportMenu from '../../../shared/components/ui/ExportMenu'
import { useClientList, useDeactivateClient } from '../hooks/useClients'
import type { ExportSection } from '../../../shared/utils/exportTable'

export default function ClientListPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 20

  const { data, isLoading, isError } = useClientList({ pageNumber: page, pageSize, search: search || undefined })
  const deactivate = useDeactivateClient()

  function handleDeactivate(id: number, name: string) {
    if (!confirm(`ยืนยันการปิดใช้งานลูกค้า "${name}" ?`)) return
    deactivate.mutate(id)
  }

  return (
    <div>
      <PageHeader
        title="จัดการลูกค้า"
        action={(
          <div className="flex items-center gap-2">
            {data && data.items.length > 0 && (
              <ExportMenu
                meta={{ title: 'รายชื่อลูกค้า', fileName: 'clients' }}
                getSections={(): ExportSection[] => [{
                  name: 'ลูกค้า',
                  columns: [
                    { key: 'name', header: 'ชื่อบริษัท' },
                    { key: 'taxId', header: 'เลขภาษี' },
                    { key: 'isActive', header: 'สถานะ', value: (c) => (c.isActive ? 'ใช้งาน' : 'ปิด') },
                  ],
                  rows: data.items,
                }]}
              />
            )}
            <Button type="button" onClick={() => navigate('/clients/new')}>
              + เพิ่มลูกค้าใหม่
            </Button>
          </div>
        )}
      />

      <div className="mb-4">
        <SearchInput
          placeholder="ค้นหาชื่อบริษัทหรือเลขภาษี..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1) }}
          className="w-80"
        />
      </div>

      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
      {isError && <StateMessage tone="error">เกิดข้อผิดพลาดในการโหลดข้อมูล</StateMessage>}

      {data && (
        <>
          <ClientTable
            clients={data.items}
            deactivatePending={deactivate.isPending}
            onDeactivate={handleDeactivate}
          />

          <Pagination
            page={page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}
    </div>
  )
}
