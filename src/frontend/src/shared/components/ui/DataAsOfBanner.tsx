interface Props {
  /** เวลานำเข้าล่าสุด (ISO จาก backend, CreatedAt = UTC) */
  dataAsOf?: string | null
  /** คำเรียกข้อมูล เช่น "สินค้าคงคลัง", "ภาษีซื้อ-ขาย" — ต่อท้าย "ข้อมูล…เป็น snapshot ณ ตอนนำเข้า" */
  noun?: string
}

function fmtDateTime(s?: string | null) {
  if (!s) return null
  const iso = /[zZ]|[+-]\d\d:?\d\d$/.test(s) ? s : s + 'Z' // CreatedAt เป็น UTC
  return new Date(iso).toLocaleString('th-TH', { dateStyle: 'medium', timeStyle: 'short' })
}

/**
 * แถบแจ้งความสดของข้อมูล — ข้อมูลทุกโมดูลเป็น snapshot ณ ตอนนำเข้าจาก Express (ไม่ real-time)
 * แสดง "นำเข้าล่าสุด {วันเวลา}" + ลิงก์ไปหน้านำเข้าข้อมูล
 */
export default function DataAsOfBanner({ dataAsOf, noun = 'ชุดนี้' }: Props) {
  const when = fmtDateTime(dataAsOf)
  return (
    <div className="mb-3 flex flex-wrap items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-xs text-amber-800">
      <span>📌 ข้อมูล{noun}เป็น snapshot ณ ตอนนำเข้า — </span>
      <span className="font-semibold">{when ? `นำเข้าล่าสุด ${when}` : 'ยังไม่มีข้อมูลนำเข้า'}</span>
      <a
        href="/import"
        className="ml-auto rounded border border-amber-300 bg-white px-2 py-0.5 text-amber-700 no-underline hover:bg-amber-100"
      >
        นำเข้าข้อมูลใหม่
      </a>
    </div>
  )
}
