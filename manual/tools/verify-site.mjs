import { chromium } from 'playwright'
import { pathToFileURL } from 'node:url'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'
const __dirname = dirname(fileURLToPath(import.meta.url))
const site = join(__dirname, '..', 'site')
const OUT = join(__dirname, '..', '_preview')
import { mkdir } from 'node:fs/promises'
await mkdir(OUT, { recursive: true })

const browser = await chromium.launch()
const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 }, deviceScaleFactor: 1 })).newPage()
const pageErrs = []
page.on('pageerror', e => pageErrs.push(String(e)))

for (const [name, rel] of [['overview', 'index.html'], ['import', 'pages/import.html']]) {
  await page.goto(pathToFileURL(join(site, rel)).href, { waitUntil: 'networkidle' })
  await page.waitForTimeout(600)
  await page.screenshot({ path: join(OUT, `render-${name}.png`), fullPage: true })
  // check broken images
  const broken = await page.evaluate(() => Array.from(document.images).filter(i => !i.complete || i.naturalWidth === 0).map(i => i.getAttribute('src')))
  console.log(name, 'brokenImages=', JSON.stringify(broken))
}
console.log('pageErrors=', pageErrs)
await browser.close()
