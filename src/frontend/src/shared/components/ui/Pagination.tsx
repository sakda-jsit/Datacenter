import Button from './Button'

interface PaginationProps {
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}

export default function Pagination({ page, totalPages, totalCount, onPageChange }: PaginationProps) {
  return (
    <div className="mt-4 flex items-center justify-between text-sm text-gray-600">
      <span>ทั้งหมด {totalCount} รายการ</span>
      <div className="flex gap-2">
        <Button
          type="button"
          variant="secondary"
          disabled={page <= 1}
          className="px-3 py-1"
          onClick={() => onPageChange(page - 1)}
        >
          ก่อนหน้า
        </Button>
        <span className="px-3 py-1">หน้า {page} / {totalPages}</span>
        <Button
          type="button"
          variant="secondary"
          disabled={page >= totalPages}
          className="px-3 py-1"
          onClick={() => onPageChange(page + 1)}
        >
          ถัดไป
        </Button>
      </div>
    </div>
  )
}
