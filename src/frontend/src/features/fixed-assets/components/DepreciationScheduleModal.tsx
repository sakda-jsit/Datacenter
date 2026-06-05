import { useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useFixedAsset } from '../hooks/useFixedAssets'
import { STATUS_LABEL } from '../types/fixedAsset.types'
import type { DepreciationAsOf, DepreciationYear } from '../types/fixedAsset.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface Props {
  companyId: number
  fiscalYear: number
  assetId: number
  onClose: () => void
}

type SetTab = 'book' | 'tax'

export default function DepreciationScheduleModal({ companyId, fiscalYear, assetId, onClose }: Props) {
  const { data, isLoading, isError } = useFixedAsset(assetId, companyId, fiscalYear)
  const [setTab, setSetTab] = useState<SetTab>('book')

  const schedule = setTab === 'book' ? data?.bookSchedule : data?.taxSchedule
  const asOf = setTab === 'book' ? data?.book : data?.tax

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-4xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">
              ตารางค่าเสื่อมราคา {data ? `· ${data.asset.assetCode}` : ''}
            </h2>
            {data && (
              <p className="text-xs text-gray-500">
                {data.asset.assetName} · ราคาทุน {fmt(data.asset.cost)} · อัตรา บัญชี {data.asset.bookRatePct}% / ภาษี {data.asset.taxRatePct}%
                {' · '}สถานะ {STATUS_LABEL[data.asset.status]} · ปีบัญชี {fiscalYear}
              </p>
            )}
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="px-6 py-4">
          {isError && <StateMessage tone="error">เกิดข้อผิดพลาด</StateMessage>}
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

          {data && (
            <>
              {/* tab ชุดบัญชี/ภาษี */}
              <div className="mb-4 inline-flex rounded-lg border border-slate-200 p-0.5 text-sm">
                <button
                  type="button" onClick={() => setSetTab('book')}
                  className={`rounded-md px-4 py-1.5 ${setTab === 'book' ? 'bg-slate-800 text-white' : 'text-slate-600'}`}
                >ชุดบัญชี</button>
                <button
                  type="button" onClick={() => setSetTab('tax')}
                  className={`rounded-md px-4 py-1.5 ${setTab === 'tax' ? 'bg-slate-800 text-white' : 'text-slate-600'}`}
                >ชุดภาษี</button>
              </div>

              {/* สรุป ณ สิ้นปีบัญชี */}
              {asOf && <AsOfCard fiscalYear={fiscalYear} cost={data.asset.cost} asOf={asOf} />}

              {/* การจำหน่าย */}
              {data.disposal && (
                <div className="my-4 rounded border border-amber-200 bg-amber-50 px-4 py-3 text-sm">
                  <p className="font-semibold text-amber-800">ผลการจำหน่าย ({STATUS_LABEL[data.disposal.status]})</p>
                  <div className="mt-1 grid grid-cols-2 gap-x-6 gap-y-1 text-xs text-amber-900 sm:grid-cols-4">
                    <span>วันที่จำหน่าย: {data.disposal.disposalDate.slice(0, 10)}</span>
                    <span>ราคาขาย: <b>{fmt(data.disposal.proceeds)}</b></span>
                    <span>มูลค่าสุทธิ ณ วันจำหน่าย: {fmt(data.disposal.netBookValueAtDisposal)}</span>
                    <span className={data.disposal.gainLoss >= 0 ? 'text-green-700' : 'text-red-600'}>
                      {data.disposal.gainLoss >= 0 ? 'กำไร' : 'ขาดทุน'}: <b>{fmt(Math.abs(data.disposal.gainLoss))}</b>
                    </span>
                  </div>
                </div>
              )}

              {/* ตารางค่าเสื่อมรายปี */}
              <p className="mb-2 mt-4 text-sm font-semibold text-slate-700">
                ตารางค่าเสื่อมรายปี ({setTab === 'book' ? 'ชุดบัญชี' : 'ชุดภาษี'}) — {schedule?.length ?? 0} ปี
              </p>
              {schedule && schedule.length > 0 ? (
                <div className="max-h-80 overflow-auto rounded border border-gray-200">
                  <table className="w-full text-xs">
                    <thead className="sticky top-0 bg-slate-50 text-gray-600">
                      <tr>
                        <th className="px-3 py-2 text-right font-medium">ปี</th>
                        <th className="px-3 py-2 text-right font-medium">ค่าเสื่อมสะสมต้นปี</th>
                        <th className="px-3 py-2 text-right font-medium">ค่าเสื่อมปีนี้</th>
                        <th className="px-3 py-2 text-right font-medium">ค่าเสื่อมสะสมสิ้นปี</th>
                        <th className="px-3 py-2 text-right font-medium">มูลค่าสุทธิ</th>
                      </tr>
                    </thead>
                    <tbody>
                      {schedule.map((r: DepreciationYear) => (
                        <tr key={r.year} className={`border-t border-gray-100 ${r.year === fiscalYear ? 'bg-sky-50/60' : ''}`}>
                          <td className="px-3 py-1 text-right text-gray-600">{r.year}</td>
                          <td className="px-3 py-1 text-right font-mono text-gray-500">{fmt(r.openingAccumulated)}</td>
                          <td className="px-3 py-1 text-right font-mono">{fmt(r.charge)}</td>
                          <td className="px-3 py-1 text-right font-mono">{fmt(r.closingAccumulated)}</td>
                          <td className="px-3 py-1 text-right font-mono font-semibold">{fmt(r.netBookValue)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="rounded border border-gray-200 px-4 py-3 text-xs text-gray-400">
                  ไม่มีค่าเสื่อม (อัตรา 0 เช่น ที่ดิน)
                </div>
              )}
              <p className="mt-2 text-xs text-gray-400">แถวพื้นฟ้า = ปีบัญชี {fiscalYear}</p>
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

function AsOfCard({ fiscalYear, cost, asOf }: { fiscalYear: number; cost: number; asOf: DepreciationAsOf }) {
  return (
    <div className="grid grid-cols-2 gap-3 rounded border border-gray-200 bg-slate-50/50 p-3 text-sm sm:grid-cols-5">
      <Stat label="ราคาทุน" value={cost} />
      <Stat label="ค่าเสื่อมสะสมต้นปี" value={asOf.openingAccumulated} muted />
      <Stat label={`ค่าเสื่อมปี ${fiscalYear}`} value={asOf.charge} accent />
      <Stat label="ค่าเสื่อมสะสมสิ้นปี" value={asOf.closingAccumulated} />
      <Stat label="มูลค่าสุทธิสิ้นปี" value={asOf.netBookValue} bold />
    </div>
  )
}

function Stat({ label, value, muted, accent, bold }: { label: string; value: number; muted?: boolean; accent?: boolean; bold?: boolean }) {
  return (
    <div>
      <p className="text-[11px] text-gray-500">{label}</p>
      <p className={`font-mono ${accent ? 'text-sky-700' : muted ? 'text-gray-500' : 'text-slate-800'} ${bold ? 'font-bold' : ''}`}>
        {fmt(value)}
      </p>
    </div>
  )
}
