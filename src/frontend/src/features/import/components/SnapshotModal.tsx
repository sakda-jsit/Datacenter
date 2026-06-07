import { useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import StatusBadge from '../../../shared/components/ui/StatusBadge'
import { importApi } from '../services/importApi'
import { useImportSnapshot } from '../hooks/useImport'
import type { ImportSnapshotStatus } from '../types/import.types'

interface Props {
  batchId: number
  fiscalYear: number
  clientName: string
  onClose: () => void
}

const STATUS_LABEL: Record<ImportSnapshotStatus, string> = { 1: 'เก็บครบ', 2: 'เก็บบางส่วน', 3: 'เก็บไม่สำเร็จ' }
const STATUS_TONE: Record<ImportSnapshotStatus, 'green' | 'yellow' | 'red'> = { 1: 'green', 2: 'yellow', 3: 'red' }

function fmtBytes(n: number): string {
  if (n < 1024) return `${n} B`
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`
  return `${(n / 1024 / 1024).toFixed(2)} MB`
}

function fmtDate(value?: string): string {
  if (!value) return '-'
  return new Date(value).toLocaleString('th-TH', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

async function saveBlob(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = name
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

export default function SnapshotModal({ batchId, fiscalYear, clientName, onClose }: Props) {
  const { data: snapshot, isLoading, isError } = useImportSnapshot(batchId)
  const [busy, setBusy] = useState(false)

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/50 p-4">
      <div className="my-6 flex max-h-[90vh] w-full max-w-4xl flex-col rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-3">
          <div>
            <h2 className="text-lg font-bold text-slate-800">หลักฐานไฟล์ต้นฉบับ (Express DBF Snapshot)</h2>
            <p className="text-xs text-gray-500">{clientName} · ปีบัญชี {fiscalYear} · เก็บถาวร 10 ปี</p>
          </div>
          <div className="flex items-center gap-2">
            {snapshot && (
              <Button
                type="button"
                variant="secondary"
                disabled={busy}
                onClick={async () => {
                  setBusy(true)
                  try { await saveBlob(await importApi.downloadSnapshot(batchId), snapshot.archiveFileName) }
                  finally { setBusy(false) }
                }}
              >
                {busy ? 'กำลังดาวน์โหลด...' : '⬇ ดาวน์โหลด .zip'}
              </Button>
            )}
            <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-6">
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}
          {isError && <StateMessage tone="error">โหลดหลักฐานไม่สำเร็จ</StateMessage>}
          {!isLoading && !isError && !snapshot && (
            <StateMessage tone="warning">
              ยังไม่มีการเก็บหลักฐานสำหรับการนำเข้านี้ (นำเข้าก่อนเปิดใช้งานฟีเจอร์นี้) — นำเข้าใหม่อีกครั้งเพื่อเก็บไฟล์ต้นฉบับ
            </StateMessage>
          )}

          {snapshot && (
            <>
              <div className="mb-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-3">
                <Field label="สถานะ">
                  <StatusBadge tone={STATUS_TONE[snapshot.status]}>{STATUS_LABEL[snapshot.status]}</StatusBadge>
                </Field>
                <Field label="เก็บเมื่อ">{fmtDate(snapshot.capturedAt)}</Field>
                <Field label="เก็บถึง (retention)">{fmtDate(snapshot.retainUntil)}</Field>
                <Field label="จำนวนไฟล์">{snapshot.fileCount.toLocaleString()}</Field>
                <Field label="ขนาดไฟล์ zip">{fmtBytes(snapshot.archiveByteSize)}</Field>
                <Field label="ขนาดต้นฉบับรวม">{fmtBytes(snapshot.totalSourceBytes)}</Field>
                <Field label="ผู้นำเข้า">{snapshot.createdBy}</Field>
                <div className="col-span-2 md:col-span-3">
                  <p className="mb-0.5 text-xs font-medium text-gray-500">โฟลเดอร์ต้นทาง</p>
                  <p className="break-all font-mono text-xs text-slate-700">{snapshot.sourceFolderPath}</p>
                </div>
                <div className="col-span-2 md:col-span-3">
                  <p className="mb-0.5 text-xs font-medium text-gray-500">SHA-256 (ไฟล์ zip)</p>
                  <p className="break-all font-mono text-xs text-slate-700">{snapshot.archiveSha256}</p>
                </div>
              </div>

              {snapshot.note && (
                <div className="mb-3">
                  <StateMessage tone="warning">{snapshot.note}</StateMessage>
                </div>
              )}

              <div className="overflow-x-auto rounded-lg border border-slate-200">
                <table className="min-w-full text-sm">
                  <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                    <tr>
                      <th className="px-3 py-2 text-left">ตาราง</th>
                      <th className="px-3 py-2 text-left">ไฟล์</th>
                      <th className="px-3 py-2 text-right">ระเบียน</th>
                      <th className="px-3 py-2 text-right">ขนาด</th>
                      <th className="px-3 py-2 text-left">SHA-256</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {snapshot.files.map((f) => (
                      <tr key={f.fileName}>
                        <td className="px-3 py-1.5 font-medium text-slate-700">{f.tableName}</td>
                        <td className="px-3 py-1.5 font-mono text-xs text-slate-600">{f.fileName}</td>
                        <td className="px-3 py-1.5 text-right font-mono">{f.rowCount?.toLocaleString() ?? '-'}</td>
                        <td className="px-3 py-1.5 text-right font-mono text-xs">{fmtBytes(f.byteSize)}</td>
                        <td className="px-3 py-1.5 font-mono text-[10px] text-slate-400" title={f.sha256}>{f.sha256.slice(0, 16)}…</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="mb-0.5 text-xs font-medium text-gray-500">{label}</p>
      <div className="text-slate-700">{children}</div>
    </div>
  )
}
