import type { ClientListDto } from '../../../features/clients/types/client.types'
import Button from '../ui/Button'
import SearchableSelect from '../ui/SearchableSelect'

const MONTHS = [
  '', 'ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.',
  'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.',
]

interface Props {
  clients: ClientListDto[]
  clientId: number
  year: number
  monthFrom: number
  monthTo: number
  onClientChange: (id: number) => void
  onYearChange: (y: number) => void
  onMonthFromChange: (m: number) => void
  onMonthToChange: (m: number) => void
  onSearch: () => void
  loading?: boolean
  extra?: React.ReactNode
  showCompanySelect?: boolean
}

export default function ReportFilterBar({
  clients, clientId, year, monthFrom, monthTo,
  onClientChange, onYearChange, onMonthFromChange, onMonthToChange,
  onSearch, loading, extra, showCompanySelect = false,
}: Props) {
  return (
    <div className="bg-white rounded-lg shadow p-4 mb-5">
      <div className="flex flex-wrap gap-3 items-end">
        {/* Company */}
        {showCompanySelect && (
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">บริษัทลูกค้า</label>
            <SearchableSelect
              value={clientId}
              onChange={(nextValue) => onClientChange(Number(nextValue))}
              placeholder="-- เลือกบริษัท --"
              searchPlaceholder="ค้นหาชื่อลูกค้าหรือรหัส..."
              className="w-64"
              options={[
                { value: 0, label: '-- เลือกบริษัท --' },
                ...clients
                  .filter((client) => client.isActive)
                  .map((client) => ({
                    value: client.id,
                    label: client.name,
                    searchText: `${client.code} ${client.name} ${client.taxId}`,
                  })),
              ]}
            />
          </div>
        )}

        {/* Year */}
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">ปีบัญชี (AD)</label>
          <input
            type="number"
            value={year}
            onChange={(e) => onYearChange(Number(e.target.value))}
            min={2000} max={2100}
            className="border border-gray-300 rounded px-3 py-2 text-sm w-24 focus:outline-none focus:ring-2 focus:ring-slate-400"
          />
        </div>

        {/* Month From */}
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">ตั้งแต่เดือน</label>
          <select
            value={monthFrom}
            onChange={(e) => onMonthFromChange(Number(e.target.value))}
            className="border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
          >
            {MONTHS.slice(1).map((m, i) => (
              <option key={i + 1} value={i + 1}>{m}</option>
            ))}
          </select>
        </div>

        {/* Month To */}
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">ถึงเดือน</label>
          <select
            value={monthTo}
            onChange={(e) => onMonthToChange(Number(e.target.value))}
            className="border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
          >
            {MONTHS.slice(1).map((m, i) => (
              <option key={i + 1} value={i + 1} disabled={i + 1 < monthFrom}>{m}</option>
            ))}
          </select>
        </div>

        {extra}

        <Button
          onClick={onSearch}
          disabled={!clientId || loading}
        >
          {loading ? 'กำลังโหลด...' : 'แสดงรายงาน'}
        </Button>
      </div>
    </div>
  )
}
