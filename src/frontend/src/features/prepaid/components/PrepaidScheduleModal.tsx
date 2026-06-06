import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { usePrepaidDetail } from '../hooks/usePrepaid'

interface Props {
  companyId: number
  fiscalYear: number
  prepaidId: number
  onClose: () => void
}

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function PrepaidScheduleModal({ companyId, fiscalYear, prepaidId, onClose }: Props) {
  const { data, isLoading, isError } = usePrepaidDetail(prepaidId, companyId, fiscalYear)

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-2xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">ตารางตัดจ่าย</h2>
            {data && <p className="text-xs text-gray-500">{data.item.name} · ตั้งต้น {fmt(data.item.totalAmount)} · {data.item.totalDays} วัน</p>}
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>
        <div className="px-6 py-4">
          {isError && <StateMessage tone="error">เกิดข้อผิดพลาด</StateMessage>}
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
          {data && (
            <>
              <p className="mb-2 text-xs text-gray-500">
                {data.item.startDate.slice(0, 10)} – {data.item.endDate.slice(0, 10)} · ตัดจ่ายเส้นตรงตามวัน
              </p>
              <table className="w-full text-sm">
                <thead className="border-b bg-slate-50 text-xs text-gray-600">
                  <tr>
                    <th className="px-3 py-2 text-left font-medium">ปี</th>
                    <th className="px-3 py-2 text-right font-medium">ยกมา</th>
                    <th className="px-3 py-2 text-right font-medium">ตัดจ่ายปีนี้</th>
                    <th className="px-3 py-2 text-right font-medium">สะสม</th>
                    <th className="px-3 py-2 text-right font-medium">คงเหลือ</th>
                  </tr>
                </thead>
                <tbody>
                  {data.schedule.map((r) => (
                    <tr key={r.year} className={`border-b border-gray-100 ${r.year === fiscalYear ? 'bg-sky-50 font-medium' : ''}`}>
                      <td className="px-3 py-1.5">{r.year}</td>
                      <td className="px-3 py-1.5 text-right font-mono text-gray-500">{fmt(r.openingAmortized)}</td>
                      <td className="px-3 py-1.5 text-right font-mono text-sky-700">{fmt(r.charge)}</td>
                      <td className="px-3 py-1.5 text-right font-mono">{fmt(r.closingAmortized)}</td>
                      <td className="px-3 py-1.5 text-right font-mono">{fmt(r.remaining)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <p className="mt-3 text-xs text-gray-500">
                ณ สิ้นปี {fiscalYear}: ตัดจ่ายในปี <b>{fmt(data.asOf.charge)}</b> · คงเหลือ <b>{fmt(data.asOf.remaining)}</b>
                {data.asOf.fullyAmortized && <span className="ml-1 text-green-600">(ตัดจ่ายครบแล้ว)</span>}
              </p>
            </>
          )}
          <div className="mt-5 flex justify-end">
            <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
          </div>
        </div>
      </div>
    </div>
  )
}
