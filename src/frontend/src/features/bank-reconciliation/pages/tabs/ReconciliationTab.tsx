import { useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import { useAccountList } from '../../../trial-balance/hooks/useTrialBalance'
import { bankApi } from '../../services/bankApi'
import {
  useBankAccounts, useBankReconciliation, useBankStatementImports, useReconMutations,
} from '../../hooks/useBank'
import type { StatementParsePreview } from '../../types/bank.types'

function fmt(n: number) {
  return n.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function d(s?: string | null) {
  if (!s) return '-'
  const iso = /[zZ]|[+-]\d\d:?\d\d$/.test(s) ? s : s + 'Z'
  return new Date(iso).toLocaleDateString('th-TH', { dateStyle: 'short' })
}

interface Props { companyId: number }

export default function ReconciliationTab({ companyId }: Props) {
  const { data: accounts } = useBankAccounts(companyId)
  const { data: glAccounts } = useAccountList(companyId)
  const [accountId, setAccountId] = useState(0)
  const { data: imports } = useBankStatementImports(companyId, accountId || undefined, accountId > 0)

  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<StatementParsePreview | null>(null)
  const [opening, setOpening] = useState<string>('')
  const [closing, setClosing] = useState<string>('')
  const [busy, setBusy] = useState(false)
  const [err, setErr] = useState('')
  const [openImportId, setOpenImportId] = useState(0)

  const { data: recon } = useBankReconciliation(companyId, openImportId, openImportId > 0)
  const m = useReconMutations(companyId)
  const qc = useQueryClient()

  const glOptions = useMemo(
    () => (glAccounts ?? []).filter((a: any) => a.isPostable).map((a: any) => ({ id: a.id, label: `${a.accountCode} — ${a.accountName}` })),
    [glAccounts],
  )

  async function doPreview() {
    if (!file || !accountId) { setErr('เลือกบัญชีและไฟล์ก่อน'); return }
    setErr(''); setBusy(true); setPreview(null)
    try {
      const p = await bankApi.preview(companyId, accountId, file)
      setPreview(p)
      setOpening(String(p.openingBalance || ''))
      setClosing(String(p.closingBalance || ''))
    } catch (e: any) {
      setErr(e?.response?.data?.detail ?? e?.response?.data?.title ?? 'อ่านไฟล์ไม่สำเร็จ')
    } finally { setBusy(false) }
  }

  async function doUpload() {
    if (!file || !accountId) return
    if (preview?.accountMatches === false) {
      setErr(`เลขบัญชีใน statement (${preview.accountNo ?? '-'}) ไม่ตรงกับบัญชีบริษัทที่เลือก (${preview.expectedAccountNo ?? '-'}) — โปรดเลือกบัญชีให้ถูกต้องก่อนบันทึก`)
      return
    }
    setErr(''); setBusy(true)
    try {
      const res = await bankApi.upload(companyId, accountId, file,
        opening ? Number(opening) : undefined, closing ? Number(closing) : undefined)
      setFile(null); setPreview(null); setOpening(''); setClosing('')
      await qc.invalidateQueries({ queryKey: ['bank-stmt-imports', companyId] })
      setOpenImportId(res.id)
    } catch (e: any) {
      setErr(e?.response?.data?.detail ?? e?.response?.data?.title ?? 'บันทึกไม่สำเร็จ')
    } finally { setBusy(false) }
  }

  async function downloadTemplate() {
    const blob = await bankApi.template()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'bank-statement-template.xlsx'; a.click()
    URL.revokeObjectURL(url)
  }

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  return (
    <div>
      {/* ตั้งค่า + อัปโหลด */}
      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">บัญชีธนาคาร</label>
            <select value={accountId} onChange={(e) => { setAccountId(Number(e.target.value)); setOpenImportId(0) }}
              className="w-72 rounded border border-gray-300 px-3 py-2 text-sm">
              <option value={0}>-- เลือกบัญชี --</option>
              {(accounts ?? []).map((a) => (
                <option key={a.id} value={a.id}>{a.bankName} {a.accountNumber ?? a.bankAccountCode}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ไฟล์ statement (PDF / Excel / CSV)</label>
            <input type="file" accept=".pdf,.xlsx,.xls,.csv"
              onChange={(e) => { setFile(e.target.files?.[0] ?? null); setPreview(null) }}
              className="text-sm" />
          </div>
          <Button type="button" variant="secondary" onClick={doPreview} disabled={!file || !accountId || busy}>
            {busy ? 'กำลังอ่าน...' : 'อ่าน/พรีวิว'}
          </Button>
          <button type="button" onClick={downloadTemplate}
            className="rounded border border-slate-300 px-3 py-2 text-xs text-slate-600 hover:bg-slate-50">
            ⬇ ดาวน์โหลดเทมเพลต Excel
          </button>
        </div>
        {err && <p className="mt-2 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{err}</p>}
        <p className="mt-2 text-xs text-gray-400">
          รองรับ PDF ของ SCB / KBANK / TTB / กรุงศรี-BAY (อ่านอัตโนมัติ) — ธนาคารอื่น/สแกน (GSB/KTB/BBL) ใช้เทมเพลต Excel/CSV หรือกรอกมือ
        </p>
      </Card>

      {/* preview ก่อนบันทึก */}
      {preview && (
        <Card className="mb-4 p-4">
          <div className="mb-2 flex items-center justify-between">
            <p className="text-sm font-semibold text-slate-800">
              พรีวิว: ธนาคาร <span className="font-mono">{preview.bankCode}</span>
              {preview.accountNo && <span className="ml-2 text-gray-500">บัญชี {preview.accountNo}</span>}
              <span className="ml-2 text-gray-500">{preview.lines.length} รายการ</span>
            </p>
          </div>
          <div className={`mb-3 rounded-lg border px-3 py-2 text-sm ${preview.balanceCheckPasses ? 'border-green-200 bg-green-50 text-green-700' : 'border-amber-200 bg-amber-50 text-amber-700'}`}>
            {preview.balanceCheckPasses
              ? `✓ ตรวจยอดผ่าน: ยอดต้น ${fmt(preview.openingBalance)} + ฝาก − ถอน = ยอดปลาย ${fmt(preview.closingBalance)} (ตรงกับ statement)`
              : `⚠ ${preview.warning ?? 'ตรวจยอดไม่ผ่าน — โปรดตรวจ/แก้ยอดต้น-ปลาย'} (คำนวณได้ ${fmt(preview.computedClosing)})`}
          </div>
          {preview.accountMatches === false && (
            <div className="mb-3 rounded-lg border border-red-300 bg-red-50 px-3 py-2 text-sm text-red-700">
              ⚠ เลขบัญชีไม่ตรงกัน: statement = <span className="font-mono">{preview.accountNo ?? '-'}</span> แต่บัญชีบริษัทที่เลือก = <span className="font-mono">{preview.expectedAccountNo ?? '-'}</span>
              <span className="block text-xs text-red-500">บันทึกไม่ได้จนกว่าจะเลือกบัญชีบริษัทให้ตรงกับ statement</span>
            </div>
          )}
          {preview.accountMatches === true && (
            <div className="mb-3 rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-700">
              ✓ เลขบัญชีตรงกับบัญชีบริษัท (<span className="font-mono">{preview.accountNo}</span>)
            </div>
          )}
          <div className="mb-3 flex flex-wrap gap-3">
            <label className="text-xs text-gray-600">ยอดต้นงวด
              <input value={opening} onChange={(e) => setOpening(e.target.value)} className="ml-2 w-36 rounded border border-gray-300 px-2 py-1 text-right font-mono text-sm" />
            </label>
            <label className="text-xs text-gray-600">ยอดปลายงวด
              <input value={closing} onChange={(e) => setClosing(e.target.value)} className="ml-2 w-36 rounded border border-gray-300 px-2 py-1 text-right font-mono text-sm" />
            </label>
            <Button type="button" onClick={doUpload} disabled={busy || preview.accountMatches === false}>{busy ? 'กำลังบันทึก...' : 'ยืนยันบันทึก + จับคู่อัตโนมัติ'}</Button>
          </div>
          <div className="max-h-60 overflow-auto rounded border border-gray-100">
            <table className="w-full text-xs">
              <thead className="sticky top-0 bg-slate-50 text-gray-600"><tr>
                <th className="px-2 py-1 text-left">วันที่</th><th className="px-2 py-1 text-left">รายละเอียด</th>
                <th className="px-2 py-1 text-right">ถอน</th><th className="px-2 py-1 text-right">ฝาก</th><th className="px-2 py-1 text-right">คงเหลือ</th>
              </tr></thead>
              <tbody>
                {preview.lines.map((l, i) => (
                  <tr key={i} className="border-t border-gray-100">
                    <td className="px-2 py-1">{d(l.date)}</td>
                    <td className="px-2 py-1 text-gray-600">{l.description}</td>
                    <td className="px-2 py-1 text-right font-mono">{l.withdrawal ? fmt(l.withdrawal) : ''}</td>
                    <td className="px-2 py-1 text-right font-mono text-sky-700">{l.deposit ? fmt(l.deposit) : ''}</td>
                    <td className="px-2 py-1 text-right font-mono text-gray-500">{l.balance != null ? fmt(l.balance) : ''}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {/* รายการรอบนำเข้า */}
      {accountId > 0 && (
        <Card className="mb-4 overflow-hidden">
          <div className="border-b px-4 py-2 text-sm font-semibold text-slate-800">รอบนำเข้า statement</div>
          {(imports ?? []).length === 0 ? (
            <div className="px-4 py-3 text-xs text-gray-400">ยังไม่มี — อัปโหลด statement ด้านบน</div>
          ) : (
            <table className="w-full text-xs">
              <thead className="bg-slate-50 text-gray-600"><tr>
                <th className="px-3 py-2 text-left">ธนาคาร</th><th className="px-3 py-2 text-left">งวด</th>
                <th className="px-3 py-2 text-right">รายการ</th><th className="px-3 py-2 text-right">จับคู่</th>
                <th className="px-3 py-2 text-center">ตรวจยอด</th><th className="px-3 py-2"></th>
              </tr></thead>
              <tbody>
                {(imports ?? []).map((im) => (
                  <tr key={im.id} className={`border-t border-gray-100 ${openImportId === im.id ? 'bg-sky-50' : ''}`}>
                    <td className="px-3 py-1.5 font-mono">
                      {im.bankCode}
                      {im.accountMatches === false && (
                        <span className="ml-1 text-red-500" title={`เลขบัญชีไม่ตรง: statement ${im.statementAccountNo ?? '-'} ≠ บริษัท ${im.expectedAccountNo ?? '-'}`}>⚠</span>
                      )}
                    </td>
                    <td className="px-3 py-1.5">{d(im.periodStart)}–{d(im.periodEnd)}</td>
                    <td className="px-3 py-1.5 text-right">{im.lineCount}</td>
                    <td className="px-3 py-1.5 text-right">{im.matchedCount}/{im.lineCount}</td>
                    <td className="px-3 py-1.5 text-center">{im.parsedOk ? '✓' : '⚠'}</td>
                    <td className="px-3 py-1.5 text-right">
                      <button type="button" onClick={() => setOpenImportId(openImportId === im.id ? 0 : im.id)} className="rounded border border-slate-200 px-2 py-0.5 text-sky-600 hover:bg-slate-100">
                        {openImportId === im.id ? 'ปิด' : 'กระทบยอด'}
                      </button>
                      <button type="button" onClick={() => { if (confirm('ลบรอบนี้?')) { m.remove.mutate(im.id); if (openImportId === im.id) setOpenImportId(0) } }} className="ml-2 text-red-500 hover:text-red-600">ลบ</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Card>
      )}

      {recon && openImportId > 0 && (
        <ReconView recon={recon} glOptions={glOptions} mutations={m} />
      )}
    </div>
  )
}

function ReconView({ recon, glOptions, mutations }: { recon: any; glOptions: { id: number; label: string }[]; mutations: any }) {
  const [selStmt, setSelStmt] = useState<number>(0) // statement line เลือกไว้สำหรับจับคู่เอง
  const [genIds, setGenIds] = useState<Set<number>>(new Set())
  const [bankGl, setBankGl] = useState(0)
  const [counterpart, setCounterpart] = useState(0)
  const [msg, setMsg] = useState('')
  const fiscalYear = new Date(recon.periodEnd).getFullYear()

  function toggleGen(id: number) {
    setGenIds((p) => { const n = new Set(p); n.has(id) ? n.delete(id) : n.add(id); return n })
  }
  async function generate() {
    setMsg('')
    try {
      const adj = await mutations.generateAdjustment.mutateAsync({
        importId: recon.importId, fiscalYear, statementLineIds: [...genIds], bankGlAccountId: bankGl, counterpartAccountId: counterpart,
      })
      setMsg(`สร้างรายการปรับปรุง ${adj.documentNo} แล้ว — ดูที่หน้า "กระดาษทำการปิดงบ"`)
      setGenIds(new Set())
    } catch (e: any) {
      setMsg(e?.response?.data?.detail ?? e?.response?.data?.title ?? 'สร้างไม่สำเร็จ')
    }
  }

  return (
    <>
      <div className={`mb-4 rounded-lg border px-4 py-3 text-sm ${recon.isBalanced ? 'border-green-200 bg-green-50 text-green-700' : 'border-amber-200 bg-amber-50 text-amber-700'}`}>
        <div className="flex flex-wrap gap-x-6 gap-y-1">
          <span>ยอดปลายสมุด (book): <b className="font-mono">{fmt(recon.bookClosingBalance)}</b></span>
          <span>ยอดปลาย statement: <b className="font-mono">{fmt(recon.statementClosingBalance)}</b></span>
          <span>ผลต่างหลังกระทบยอด: <b className="font-mono">{fmt(recon.reconciledDifference)}</b></span>
          <span className="font-semibold">{recon.isBalanced ? '✓ กระทบยอดลงตัว' : '⚠ ยังไม่ลงตัว — ตรวจรายการที่ยังไม่จับคู่'}</span>
        </div>
      </div>

      {/* matched */}
      <Card className="mb-4 overflow-hidden">
        <div className="border-b px-4 py-2 text-sm font-semibold text-slate-800">จับคู่แล้ว ({recon.matchedCount}) — รวม {fmt(recon.matchedAmount)}</div>
        <div className="max-h-60 overflow-auto">
          <table className="w-full text-xs">
            <thead className="sticky top-0 bg-slate-50 text-gray-600"><tr>
              <th className="px-3 py-1.5 text-left">วันที่ statement</th><th className="px-3 py-1.5 text-left">รายละเอียด</th>
              <th className="px-3 py-1.5 text-right">จำนวน</th><th className="px-3 py-1.5 text-left">คู่ในสมุด</th><th className="px-3 py-1.5"></th>
            </tr></thead>
            <tbody>
              {recon.matched.map((mm: any) => (
                <tr key={mm.statementLineId} className="border-t border-gray-100">
                  <td className="px-3 py-1.5">{d(mm.date)}</td>
                  <td className="px-3 py-1.5 text-gray-600">{mm.description}</td>
                  <td className={`px-3 py-1.5 text-right font-mono ${mm.isDeposit ? 'text-sky-700' : ''}`}>{mm.isDeposit ? '+' : '−'}{fmt(mm.amount)}</td>
                  <td className="px-3 py-1.5 text-gray-500">{d(mm.bookDate)} {mm.bookCounterparty}</td>
                  <td className="px-3 py-1.5 text-right"><button type="button" onClick={() => mutations.unmatch.mutate({ importId: recon.importId, statementLineId: mm.statementLineId })} className="text-amber-600 hover:underline">ปลดคู่</button></td>
                </tr>
              ))}
              {recon.matched.length === 0 && <tr><td colSpan={5} className="px-3 py-2 text-gray-400">— ไม่มี</td></tr>}
            </tbody>
          </table>
        </div>
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* unmatched statement */}
        <Card className="overflow-hidden">
          <div className="border-b px-4 py-2 text-sm font-semibold text-slate-800">ในธนาคาร ไม่อยู่ในสมุด ({recon.unmatchedStatementCount})</div>
          <p className="px-4 py-1 text-[11px] text-gray-400">เช่น ค่าธรรมเนียม/ดอกเบี้ย → เลือกเพื่อสร้างรายการปรับปรุง</p>
          <div className="max-h-60 overflow-auto">
            <table className="w-full text-xs">
              <tbody>
                {recon.unmatchedStatement.map((s: any) => (
                  <tr key={s.statementLineId} className={`border-t border-gray-100 ${selStmt === s.statementLineId ? 'bg-sky-50' : ''}`}>
                    <td className="px-2 py-1.5"><input type="checkbox" checked={genIds.has(s.statementLineId)} onChange={() => toggleGen(s.statementLineId)} /></td>
                    <td className="px-2 py-1.5">{d(s.date)}</td>
                    <td className="px-2 py-1.5 text-gray-600">{s.description}</td>
                    <td className={`px-2 py-1.5 text-right font-mono ${s.deposit ? 'text-sky-700' : ''}`}>{s.deposit ? '+' + fmt(s.deposit) : '−' + fmt(s.withdrawal)}</td>
                    <td className="px-2 py-1.5 text-right"><button type="button" onClick={() => setSelStmt(selStmt === s.statementLineId ? 0 : s.statementLineId)} className="text-sky-600 hover:underline">{selStmt === s.statementLineId ? 'เลือกอยู่' : 'เลือกจับคู่'}</button></td>
                  </tr>
                ))}
                {recon.unmatchedStatement.length === 0 && <tr><td className="px-3 py-2 text-gray-400">— ไม่มี</td></tr>}
              </tbody>
            </table>
          </div>
          {/* generate adjustment */}
          <div className="border-t px-4 py-3">
            <p className="mb-2 text-xs font-semibold text-slate-700">สร้างรายการปรับปรุง ({genIds.size} รายการที่เลือก)</p>
            <div className="flex flex-col gap-2">
              <select value={bankGl} onChange={(e) => setBankGl(Number(e.target.value))} className="rounded border border-gray-300 px-2 py-1 text-xs">
                <option value={0}>-- บัญชี GL ธนาคาร --</option>
                {glOptions.map((o) => <option key={o.id} value={o.id}>{o.label}</option>)}
              </select>
              <select value={counterpart} onChange={(e) => setCounterpart(Number(e.target.value))} className="rounded border border-gray-300 px-2 py-1 text-xs">
                <option value={0}>-- บัญชีคู่ (ค่าธรรมเนียม/ดอกเบี้ยรับ) --</option>
                {glOptions.map((o) => <option key={o.id} value={o.id}>{o.label}</option>)}
              </select>
              <Button type="button" onClick={generate} disabled={genIds.size === 0 || !bankGl || !counterpart || mutations.generateAdjustment.isPending}>
                สร้างรายการปรับปรุงเข้า TB
              </Button>
            </div>
            {msg && <p className="mt-2 rounded bg-slate-50 px-2 py-1 text-xs text-slate-700">{msg}</p>}
          </div>
        </Card>

        {/* unmatched book */}
        <Card className="overflow-hidden">
          <div className="border-b px-4 py-2 text-sm font-semibold text-slate-800">ในสมุด ไม่อยู่ใน statement ({recon.unmatchedBookCount})</div>
          <p className="px-4 py-1 text-[11px] text-gray-400">เช่น เงินฝากระหว่างทาง/เช็คค้างจ่าย {selStmt > 0 && '— กด "จับคู่" เพื่อจับกับรายการ statement ที่เลือก'}</p>
          <div className="max-h-60 overflow-auto">
            <table className="w-full text-xs">
              <tbody>
                {recon.unmatchedBook.map((b: any) => (
                  <tr key={b.bankTransactionId} className="border-t border-gray-100">
                    <td className="px-2 py-1.5">{d(b.date)}</td>
                    <td className="px-2 py-1.5 text-gray-600">{b.counterparty ?? b.remark}</td>
                    <td className={`px-2 py-1.5 text-right font-mono ${b.deposit ? 'text-sky-700' : ''}`}>{b.deposit ? '+' + fmt(b.deposit) : '−' + fmt(b.withdrawal)}</td>
                    <td className="px-2 py-1.5 text-right">
                      {selStmt > 0 && <button type="button" onClick={() => { mutations.match.mutate({ importId: recon.importId, statementLineId: selStmt, bankTransactionId: b.bankTransactionId }); setSelStmt(0) }} className="text-green-600 hover:underline">จับคู่</button>}
                    </td>
                  </tr>
                ))}
                {recon.unmatchedBook.length === 0 && <tr><td className="px-3 py-2 text-gray-400">— ไม่มี</td></tr>}
              </tbody>
            </table>
          </div>
        </Card>
      </div>
    </>
  )
}
