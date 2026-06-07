import { useState } from 'react'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import Button from '../../../../shared/components/ui/Button'
import AttachmentUploadModal from '../../components/AttachmentUploadModal'
import { useEvidenceCompleteness } from '../../hooks/useAttachments'
import type { AttachmentCategory } from '../../types/attachment.types'

interface Props {
  companyId: number
  fiscalYear: number
}

export default function EvidenceChecklistTab({ companyId, fiscalYear }: Props) {
  const { data, isLoading, isError } = useEvidenceCompleteness(companyId, fiscalYear)
  const [uploadFor, setUploadFor] = useState<AttachmentCategory | null>(null)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
  if (isError) return <StateMessage tone="error">เกิดข้อผิดพลาดในการโหลดข้อมูล</StateMessage>
  if (isLoading || !data) return <StateMessage>กำลังโหลด...</StateMessage>

  return (
    <div>
      {/* สรุปความครบถ้วน */}
      <Card className={`mb-4 px-6 py-4 ${data.isComplete ? 'border-l-4 border-green-400' : 'border-l-4 border-amber-400'}`}>
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <p className="text-sm font-semibold text-slate-800">
              {data.isComplete
                ? '✓ หลักฐานครบถ้วน — พร้อมปิดงบ/สร้างชุดรายงาน'
                : `⚠ หลักฐานยังไม่ครบ — ขาดเอกสารบังคับ ${data.requiredMissingCount} หมวด`}
            </p>
            <p className="mt-0.5 text-xs text-slate-500">
              ปีบัญชี {fiscalYear} · แนบแล้วทั้งหมด {data.totalAttachments} ไฟล์
            </p>
          </div>
        </div>
      </Card>

      <Card className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="border-b bg-slate-50">
            <tr className="text-xs text-gray-600">
              <th className="px-4 py-3 text-left font-medium">หมวดหลักฐาน</th>
              <th className="px-4 py-3 text-center font-medium w-24">บังคับ</th>
              <th className="px-4 py-3 text-right font-medium w-24">แนบแล้ว</th>
              <th className="px-4 py-3 text-right font-medium w-24">ตรวจแล้ว</th>
              <th className="px-4 py-3 text-center font-medium w-28">สถานะ</th>
              <th className="px-4 py-3 text-right font-medium w-28">จัดการ</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((it) => {
              const ok = it.present
              const warn = it.required && !it.present
              return (
                <tr key={it.category} className="border-b border-gray-100 hover:bg-slate-50">
                  <td className="px-4 py-2.5 text-gray-800">{it.label}</td>
                  <td className="px-4 py-2.5 text-center">
                    {it.required ? <span className="text-xs font-medium text-rose-600">บังคับ</span> : <span className="text-xs text-slate-400">—</span>}
                  </td>
                  <td className="px-4 py-2.5 text-right font-mono text-slate-700">{it.count}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-slate-500">{it.verifiedCount}</td>
                  <td className="px-4 py-2.5 text-center">
                    {ok ? (
                      <span className="inline-block rounded-full bg-green-50 px-2 py-0.5 text-xs font-medium text-green-700">มีแล้ว</span>
                    ) : warn ? (
                      <span className="inline-block rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">ขาด</span>
                    ) : (
                      <span className="inline-block rounded-full bg-slate-50 px-2 py-0.5 text-xs text-slate-400">ไม่มี</span>
                    )}
                  </td>
                  <td className="px-4 py-2.5 text-right">
                    <Button type="button" variant="ghost" onClick={() => setUploadFor(it.category)} className="px-2 py-1 text-xs text-sky-600">+ แนบ</Button>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </Card>

      <p className="mt-3 text-xs text-slate-400">
        เอกสารที่ไม่ระบุปีบัญชี (ใช้ได้ทุกปี) จะถูกนับรวมในทุกปี · หมวด "บังคับ" ที่ยังไม่มีเอกสารจะทำให้สถานะหลักฐานไม่ครบ
      </p>

      {uploadFor !== null && (
        <AttachmentUploadModal
          companyId={companyId}
          fiscalYear={fiscalYear}
          defaultCategory={uploadFor}
          onClose={() => setUploadFor(null)}
        />
      )}
    </div>
  )
}
