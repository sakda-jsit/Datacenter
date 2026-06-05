/* eslint-disable @typescript-eslint/no-explicit-any */
import * as XLSX from 'xlsx'

/** หนึ่งคอลัมน์สำหรับ export — value() ดึงค่าจาก row (ถ้าไม่ระบุใช้ row[key]) */
export interface ExportColumn {
  key: string
  header: string
  value?: (row: any) => string | number | null | undefined
  align?: 'left' | 'right' | 'center'
}

/** หนึ่งตาราง/ส่วน (รายงานอาจมีหลายส่วน เช่น งบดุล + กำไรขาดทุน) */
export interface ExportSection {
  name: string
  columns: ExportColumn[]
  rows: readonly any[]
}

export interface ExportMeta {
  title: string
  subtitle?: string         // เช่น ชื่อบริษัท / ปีบัญชี
  fileName: string          // ไม่ต้องใส่นามสกุล
}

// ─── helpers ───────────────────────────────────────────────────────────────

function raw(col: ExportColumn, row: any): string | number {
  const v = col.value ? col.value(row) : (row as Record<string, unknown>)[col.key]
  if (v == null) return ''
  return typeof v === 'number' ? v : String(v)
}

function display(v: string | number): string {
  return typeof v === 'number'
    ? v.toLocaleString('th-TH', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
    : v
}

function triggerDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

function esc(html: string): string {
  return html.replace(/[&<>"']/g, (c) =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c] as string))
}

// ─── CSV (UTF-8 + BOM ให้ Excel เปิดไทยถูก) ───────────────────────────────────

export function exportCsv(meta: ExportMeta, sections: ExportSection[]) {
  const lines: string[] = []
  if (meta.title) lines.push(csvRow([meta.title]))
  if (meta.subtitle) lines.push(csvRow([meta.subtitle]))
  if (lines.length) lines.push('')

  for (const s of sections) {
    if (sections.length > 1) lines.push(csvRow([s.name]))
    lines.push(csvRow(s.columns.map((c) => c.header)))
    for (const r of s.rows) lines.push(csvRow(s.columns.map((c) => raw(c, r))))
    lines.push('')
  }

  const csv = '﻿' + lines.join('\r\n')
  triggerDownload(new Blob([csv], { type: 'text/csv;charset=utf-8;' }), `${meta.fileName}.csv`)
}

function csvRow(cells: (string | number)[]): string {
  return cells
    .map((c) => {
      const s = typeof c === 'number' ? String(c) : c
      return /[",\r\n]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s
    })
    .join(',')
}

// ─── Excel (.xlsx) — หนึ่ง section = หนึ่ง sheet ──────────────────────────────

export function exportXlsx(meta: ExportMeta, sections: ExportSection[]) {
  const wb = XLSX.utils.book_new()
  const usedNames = new Set<string>()

  for (const [i, s] of sections.entries()) {
    const aoa: (string | number)[][] = []
    if (meta.subtitle) aoa.push([meta.subtitle])
    aoa.push(s.columns.map((c) => c.header))
    for (const r of s.rows) aoa.push(s.columns.map((c) => raw(c, r)))

    const ws = XLSX.utils.aoa_to_sheet(aoa)
    ws['!cols'] = s.columns.map((c) => ({ wch: Math.max(12, c.header.length + 2) }))

    // ชื่อ sheet ≤ 31 ตัว, ห้ามซ้ำ/อักขระต้องห้าม
    let name = (s.name || `Sheet${i + 1}`).replace(/[\\/?*[\]:]/g, ' ').slice(0, 28).trim() || `Sheet${i + 1}`
    let n = name
    let k = 2
    while (usedNames.has(n)) n = `${name.slice(0, 25)} ${k++}`
    usedNames.add(n)

    XLSX.utils.book_append_sheet(wb, ws, n)
  }

  const buf = XLSX.write(wb, { bookType: 'xlsx', type: 'array' })
  triggerDownload(
    new Blob([buf], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }),
    `${meta.fileName}.xlsx`)
}

// ─── PDF — เปิดหน้าต่างพร้อมพิมพ์ (Save as PDF); ไทยเรนเดอร์ผ่านฟอนต์ระบบ ───────

export function exportPdf(meta: ExportMeta, sections: ExportSection[]) {
  const win = window.open('', '_blank', 'width=1024,height=768')
  if (!win) {
    alert('เบราว์เซอร์บล็อกหน้าต่างใหม่ — อนุญาต pop-up เพื่อสร้าง PDF')
    return
  }

  const body = sections
    .map((s) => {
      const head = s.columns.map((c) => `<th class="${c.align ?? 'left'}">${esc(c.header)}</th>`).join('')
      const rows = s.rows
        .map((r) => {
          const tds = s.columns
            .map((c) => `<td class="${c.align ?? 'left'}">${esc(display(raw(c, r)))}</td>`)
            .join('')
          return `<tr>${tds}</tr>`
        })
        .join('')
      const caption = sections.length > 1 ? `<h2>${esc(s.name)}</h2>` : ''
      return `${caption}<table><thead><tr>${head}</tr></thead><tbody>${rows}</tbody></table>`
    })
    .join('')

  win.document.write(`<!DOCTYPE html><html lang="th"><head><meta charset="utf-8"><title>${esc(meta.title)}</title>
<style>
  * { font-family: 'Sarabun','TH Sarabun New','Leelawadee UI','Tahoma',sans-serif; }
  body { margin: 24px; color: #1e293b; }
  h1 { font-size: 18px; margin: 0 0 2px; }
  h2 { font-size: 14px; margin: 16px 0 6px; }
  .sub { color: #64748b; font-size: 12px; margin-bottom: 12px; }
  table { width: 100%; border-collapse: collapse; font-size: 11px; margin-bottom: 12px; }
  th, td { border: 1px solid #cbd5e1; padding: 4px 6px; }
  th { background: #f1f5f9; text-align: left; }
  td.right, th.right { text-align: right; }
  td.center, th.center { text-align: center; }
  tbody tr:nth-child(even) { background: #f8fafc; }
  @media print { body { margin: 0; } @page { margin: 12mm; } }
</style></head><body>
  <h1>${esc(meta.title)}</h1>
  ${meta.subtitle ? `<div class="sub">${esc(meta.subtitle)}</div>` : ''}
  ${body}
  <script>window.onload = function(){ setTimeout(function(){ window.print(); }, 250); };</script>
</body></html>`)
  win.document.close()
}

export type ExportFormat = 'csv' | 'xlsx' | 'pdf'

export function runExport(format: ExportFormat, meta: ExportMeta, sections: ExportSection[]) {
  if (format === 'csv') return exportCsv(meta, sections)
  if (format === 'xlsx') return exportXlsx(meta, sections)
  return exportPdf(meta, sections)
}
