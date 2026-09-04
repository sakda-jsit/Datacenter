import { chromium } from 'playwright'

const BASE = 'http://localhost:5173'

const browser = await chromium.launch()
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } })
const page = await ctx.newPage()

await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' })
await page.fill('input[type="text"]', 'admin')
await page.fill('input[type="password"]', 'admin1234')
await page.click('button[type="submit"]')
await page.waitForURL('**/dashboard', { timeout: 15000 })

const token = await page.evaluate(() => localStorage.getItem('token'))

// list companies
const clients = await page.evaluate(async (tok) => {
  const r = await fetch('/api/v1/clients?pageNumber=1&pageSize=200', {
    headers: { Authorization: `Bearer ${tok}` },
  })
  return r.json()
}, token)

const items = clients.items ?? clients
console.log('TOTAL', items.length)

// probe each company for data richness via trial-balance available years
const results = []
for (const c of items) {
  const info = await page.evaluate(async ({ tok, id }) => {
    async function get(url) {
      try {
        const r = await fetch(url, { headers: { Authorization: `Bearer ${tok}`, 'X-Company-Id': String(id) } })
        if (!r.ok) return null
        return r.json()
      } catch { return null }
    }
    const years = await get(`/api/v1/import/available-years`)
    const accounts = await get(`/api/v1/accounts?pageNumber=1&pageSize=1`)
    return { years, accountsTotal: accounts?.totalCount ?? accounts?.total ?? (Array.isArray(accounts) ? accounts.length : null) }
  }, { tok: token, id: c.id })
  results.push({ id: c.id, code: c.code, name: c.name, isActive: c.isActive, taxId: c.taxId, ...info })
}

console.log(JSON.stringify(results, null, 2))
await browser.close()
