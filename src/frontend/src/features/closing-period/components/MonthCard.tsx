import StatusBadge from '../../../shared/components/ui/StatusBadge'
import type { ClosingPeriodMonthDto, PeriodStatusValue } from '../types/closingPeriod.types'

const MONTH_NAMES = [
  '', 'มกราคม', 'กุมภาพันธ์', 'มีนาคม', 'เมษายน', 'พฤษภาคม', 'มิถุนายน',
  'กรกฎาคม', 'สิงหาคม', 'กันยายน', 'ตุลาคม', 'พฤศจิกายน', 'ธันวาคม',
]

const STATUS_TONE: Record<PeriodStatusValue, 'gray' | 'green' | 'blue'> = {
  0: 'gray',
  1: 'green',
  2: 'blue',
}

export function monthName(month: number) {
  return MONTH_NAMES[month] ?? String(month)
}

interface Props {
  data: ClosingPeriodMonthDto
  selected: boolean
  onSelect: (month: number) => void
}

export default function MonthCard({ data, selected, onSelect }: Props) {
  return (
    <button
      type="button"
      onClick={() => onSelect(data.month)}
      className={`flex flex-col items-start gap-2 rounded-lg border p-4 text-left transition hover:border-sky-300 hover:bg-sky-50 ${
        selected ? 'border-sky-400 bg-sky-50 ring-1 ring-sky-200' : 'border-slate-200 bg-white'
      }`}
    >
      <div className="flex w-full items-center justify-between">
        <span className="text-sm font-semibold text-slate-700">{monthName(data.month)}</span>
        <StatusBadge tone={STATUS_TONE[data.status]}>{data.statusName}</StatusBadge>
      </div>
      {data.endDate && (
        <span className="text-[11px] text-slate-500">
          สิ้นงวด {new Date(data.endDate).toLocaleDateString('th-TH')}
        </span>
      )}
      {data.sourceLocked && (
        <span className="text-[11px] text-amber-600">ล็อกจาก Express</span>
      )}
      {data.closedAt && (
        <span className="text-[11px] text-slate-400">
          ปิดโดย {data.closedByName ?? '—'} · {new Date(data.closedAt).toLocaleDateString('th-TH')}
        </span>
      )}
    </button>
  )
}
