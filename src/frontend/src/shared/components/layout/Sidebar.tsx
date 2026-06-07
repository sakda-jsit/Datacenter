import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, NavLink, useLocation } from 'react-router-dom'

type IconName =
  | 'overview'
  | 'calendar'
  | 'database'
  | 'users'
  | 'upload'
  | 'calculator'
  | 'scale'
  | 'building'
  | 'book'
  | 'receipt'
  | 'bill'
  | 'bank'
  | 'payroll'
  | 'employeeTax'
  | 'shield'
  | 'tax'
  | 'percent'
  | 'fileText'
  | 'chart'
  | 'lock'
  | 'history'

interface NavItem {
  to: string
  icon: IconName
  label: string
  desc: string
}

interface NavGroup {
  title: string
  icon: IconName
  desc: string
  items: NavItem[]
}

const navGroups: NavGroup[] = [
  {
    title: 'ภาพรวม',
    icon: 'overview',
    desc: 'Dashboard และงานครบกำหนด',
    items: [
      { to: '/dashboard', icon: 'overview', label: 'Dashboard', desc: 'ภาพรวมสำนักงาน' },
      { to: '/compliance', icon: 'calendar', label: 'ปฏิทินงาน', desc: 'ภ.พ.30, ภ.ง.ด., SSO' },
    ],
  },
  {
    title: 'ข้อมูลและนำเข้า',
    icon: 'database',
    desc: 'ลูกค้าและแหล่งข้อมูล',
    items: [
      { to: '/clients', icon: 'users', label: 'ลูกค้า', desc: 'จัดการบริษัทลูกค้า' },
      { to: '/import', icon: 'upload', label: 'นำเข้าข้อมูล', desc: 'Express, Excel, CSV' },
    ],
  },
  {
    title: 'บัญชี',
    icon: 'calculator',
    desc: 'GL, AR/AP, Bank',
    items: [
      { to: '/trial-balance', icon: 'scale', label: 'งบทดลอง', desc: 'ยอดยกมาและ movement' },
      { to: '/general-ledger', icon: 'book', label: 'บัญชีแยกประเภท', desc: 'รายการบัญชีและ movement' },
      { to: '/ar', icon: 'receipt', label: 'ลูกหนี้', desc: 'ใบแจ้งหนี้และรับชำระ' },
      { to: '/ap', icon: 'bill', label: 'เจ้าหนี้', desc: 'บิลและการจ่ายเงิน' },
      { to: '/stock', icon: 'database', label: 'สินค้าคงคลัง', desc: 'มูลค่าคงเหลือ + เทียบ GL' },
      { to: '/bank-reconciliation', icon: 'bank', label: 'ธนาคาร / สมุดเงินฝาก', desc: 'สมุดเงินฝาก + เดินบัญชี' },
    ],
  },
  {
    title: 'เงินเดือน',
    icon: 'payroll',
    desc: 'Payroll, ภ.ง.ด.1, SSO',
    items: [
      { to: '/payroll', icon: 'payroll', label: 'เงินเดือน', desc: 'พนักงานและรอบเงินเดือน' },
      { to: '/payroll?section=pnd1', icon: 'employeeTax', label: 'ภ.ง.ด.1', desc: 'ภาษีหัก ณ ที่จ่ายพนักงาน' },
      { to: '/payroll?section=sso', icon: 'shield', label: 'ประกันสังคม', desc: 'นำส่งประกันสังคม' },
    ],
  },
  {
    title: 'ภาษี',
    icon: 'tax',
    desc: 'VAT และ WHT',
    items: [
      { to: '/vat', icon: 'percent', label: 'ภาษีมูลค่าเพิ่ม', desc: 'ภ.พ.30, ภาษีซื้อ/ขาย' },
      { to: '/pnd50', icon: 'fileText', label: 'ภ.ง.ด.50', desc: 'ภาษีเงินได้นิติบุคคล' },
      { to: '/wht', icon: 'receipt', label: 'หัก ณ ที่จ่าย', desc: 'ภ.ง.ด.3 และ ภ.ง.ด.53' },
      { to: '/tax-report', icon: 'fileText', label: 'รายงานภาษี', desc: 'รวมแบบและสถานะยื่น' },
    ],
  },
  {
    title: 'รายงานและปิดงวด',
    icon: 'chart',
    desc: 'งบการเงินและปิดรอบ',
    items: [
      { to: '/adjustments', icon: 'scale', label: 'กระดาษทำการปิดงบ', desc: 'งบทดลองหลังปรับปรุง' },
      { to: '/leasing', icon: 'bank', label: 'เช่าซื้อ / เงินกู้', desc: 'ตารางตัดบัญชี + ปรับปรุง' },
      { to: '/fixed-assets', icon: 'building', label: 'สินทรัพย์ถาวร', desc: 'ค่าเสื่อม 2 ชุด + จำหน่าย' },
      { to: '/prepaid', icon: 'calendar', label: 'ค่าใช้จ่ายจ่ายล่วงหน้า', desc: 'ตัดจ่ายตามงวด + ปรับปรุง' },
      { to: '/cash-count', icon: 'calculator', label: 'ตรวจนับเงินสด', desc: 'นับเงินสด + เทียบ GL' },
      { to: '/interest-income', icon: 'percent', label: 'ดอกเบี้ยรับเงินให้กู้', desc: 'คำนวณดอกเบี้ย + ปรับปรุง' },
      { to: '/financial-statement', icon: 'chart', label: 'งบการเงิน', desc: 'กำไรขาดทุนและฐานะการเงิน' },
      { to: '/closing-period', icon: 'lock', label: 'ปิดรอบบัญชี', desc: 'ตรวจสอบและ lock period' },
      { to: '/report-packages', icon: 'fileText', label: 'ชุดรายงานงบ', desc: 'เวอร์ชัน + ล็อกงบที่ยื่น' },
    ],
  },
  {
    title: 'ระบบ',
    icon: 'history',
    desc: 'ตั้งค่ากลาง · Audit',
    items: [
      { to: '/settings/payroll-rates', icon: 'shield', label: 'อัตราเงินสมทบ ปกส.', desc: 'ค่ากลาง ปกส./กองทุนทดแทน' },
      { to: '/audit-log', icon: 'history', label: 'ประวัติการใช้งาน', desc: 'Audit log' },
    ],
  },
]

interface SidebarProps {
  collapsed: boolean
  open: boolean
  onToggleCollapsed: () => void
  onCloseMobile: () => void
}

function SidebarIcon({ name, className = 'h-8 w-8' }: { name: IconName; className?: string }) {
  const common = {
    fill: 'none',
    stroke: 'currentColor',
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
    strokeWidth: 1.9,
  }

  const paths: Record<IconName, JSX.Element> = {
    overview: (
      <>
        <rect x="3" y="3" width="7" height="7" rx="1.8" {...common} />
        <rect x="14" y="3" width="7" height="5" rx="1.8" {...common} />
        <rect x="14" y="12" width="7" height="9" rx="1.8" {...common} />
        <rect x="3" y="14" width="7" height="7" rx="1.8" {...common} />
      </>
    ),
    calendar: (
      <>
        <rect x="4" y="5" width="16" height="16" rx="3" {...common} />
        <path d="M8 3v4M16 3v4M4 10h16" {...common} />
      </>
    ),
    database: (
      <>
        <ellipse cx="12" cy="5.5" rx="7" ry="3" {...common} />
        <path d="M5 5.5v6c0 1.7 3.1 3 7 3s7-1.3 7-3v-6M5 11.5v6c0 1.7 3.1 3 7 3s7-1.3 7-3v-6" {...common} />
      </>
    ),
    users: (
      <>
        <circle cx="9" cy="8" r="3" {...common} />
        <path d="M3.8 20c.6-3 2.5-5 5.2-5s4.6 2 5.2 5M16 11a2.5 2.5 0 1 0 0-5M16.8 15.2c2 .5 3.3 2.1 3.7 4.8" {...common} />
      </>
    ),
    upload: (
      <>
        <path d="M12 15V4M8 8l4-4 4 4" {...common} />
        <path d="M4 15v3a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-3" {...common} />
      </>
    ),
    calculator: (
      <>
        <rect x="5" y="3" width="14" height="18" rx="3" {...common} />
        <path d="M8 7h8M8 11h2M12 11h2M16 11h.01M8 15h2M12 15h2M16 15h.01M8 18.5h2M12 18.5h4" {...common} />
      </>
    ),
    scale: (
      <>
        <path d="M12 4v17M5 7h14M7 7l-4 7h8L7 7ZM17 7l-4 7h8l-4-7Z" {...common} />
      </>
    ),
    building: (
      <>
        <rect x="4" y="3" width="16" height="18" rx="1.5" {...common} />
        <path d="M8 7h2M14 7h2M8 11h2M14 11h2M8 15h2M14 15h2M10 21v-3h4v3" {...common} />
      </>
    ),
    book: (
      <>
        <path d="M5 4.5A2.5 2.5 0 0 1 7.5 2H20v17H7.5A2.5 2.5 0 0 0 5 21.5v-17Z" {...common} />
        <path d="M5 4.5A2.5 2.5 0 0 1 7.5 7H20" {...common} />
      </>
    ),
    receipt: (
      <>
        <path d="M6 3h12v18l-2-1.2-2 1.2-2-1.2-2 1.2-2-1.2L6 21V3Z" {...common} />
        <path d="M9 8h6M9 12h6M9 16h3" {...common} />
      </>
    ),
    bill: (
      <>
        <rect x="4" y="4" width="16" height="16" rx="3" {...common} />
        <path d="M8 9h8M8 13h8M8 17h5" {...common} />
      </>
    ),
    bank: (
      <>
        <path d="M3 10h18L12 4 3 10ZM5 10v8M9 10v8M15 10v8M19 10v8M4 20h16" {...common} />
      </>
    ),
    payroll: (
      <>
        <rect x="3" y="6" width="18" height="13" rx="3" {...common} />
        <path d="M7 10h5M7 14h3M16 10.5v4M18 12.5h-4" {...common} />
      </>
    ),
    employeeTax: (
      <>
        <circle cx="8" cy="7.5" r="3" {...common} />
        <path d="M3.5 20c.5-3 2.1-5 4.5-5 1.5 0 2.7.8 3.5 2" {...common} />
        <path d="M15 8h6M15 16h6M16 18l4-12" {...common} />
      </>
    ),
    shield: (
      <>
        <path d="M12 3 5 6v5c0 4.4 2.8 8.2 7 10 4.2-1.8 7-5.6 7-10V6l-7-3Z" {...common} />
        <path d="m9 12 2 2 4-5" {...common} />
      </>
    ),
    tax: (
      <>
        <rect x="5" y="3" width="14" height="18" rx="3" {...common} />
        <path d="M9 8h6M9 12h2M9 16h6M15.5 10.5l-4 4M11.5 10.5h.01M15.5 14.5h.01" {...common} />
      </>
    ),
    percent: (
      <>
        <path d="m19 5-14 14" {...common} />
        <circle cx="7" cy="7" r="2" {...common} />
        <circle cx="17" cy="17" r="2" {...common} />
      </>
    ),
    fileText: (
      <>
        <path d="M7 3h7l4 4v14H7a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Z" {...common} />
        <path d="M14 3v5h5M8 12h8M8 16h8" {...common} />
      </>
    ),
    chart: (
      <>
        <path d="M4 19V5M4 19h16" {...common} />
        <path d="M8 15v-4M12 15V8M16 15v-6" {...common} />
      </>
    ),
    lock: (
      <>
        <rect x="5" y="10" width="14" height="11" rx="2.5" {...common} />
        <path d="M8 10V7a4 4 0 0 1 8 0v3" {...common} />
      </>
    ),
    history: (
      <>
        <path d="M4 12a8 8 0 1 0 2.4-5.7L4 8.7" {...common} />
        <path d="M4 4v4.7h4.7M12 8v5l3 2" {...common} />
      </>
    ),
  }

  return (
    <svg className={className} viewBox="0 0 24 24" aria-hidden="true">
      {paths[name]}
    </svg>
  )
}

export default function Sidebar({ collapsed, open, onToggleCollapsed, onCloseMobile }: SidebarProps) {
  const location = useLocation()
  const sidebarRef = useRef<HTMLElement | null>(null)
  const currentUrl = `${location.pathname}${location.search}`

  const activeGroupTitle = useMemo(() => {
    return navGroups.find((group) =>
      group.items.some((item) => item.to === currentUrl || item.to.split('?')[0] === location.pathname),
    )?.title
  }, [currentUrl, location.pathname])

  const [openGroup, setOpenGroup] = useState('')
  const visibleOpenGroup = openGroup
  const selectedGroupTitle = visibleOpenGroup || activeGroupTitle

  useEffect(() => {
    if (!openGroup) return

    function handlePointerDown(event: PointerEvent) {
      const target = event.target
      if (!(target instanceof Node)) return
      if (sidebarRef.current?.contains(target)) return
      setOpenGroup('')
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setOpenGroup('')
    }

    document.addEventListener('pointerdown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('pointerdown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [openGroup])

  function toggleGroup(title: string) {
    setOpenGroup((current) => (current === title ? '' : title))
  }

  return (
    <aside
      ref={sidebarRef}
      className={`fixed inset-y-0 left-0 z-40 flex h-screen w-[292px] flex-col overflow-y-auto border-r border-slate-200/90 bg-white/90 shadow-[12px_0_35px_rgba(15,23,42,0.04)] backdrop-blur-xl transition-all duration-200 md:sticky md:top-0 ${
        collapsed ? 'md:w-[76px] md:overflow-visible' : 'md:w-[292px]'
      } ${open ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}`}
    >
      <div className={`flex items-center gap-3 px-4 pb-3 pt-5 ${collapsed ? 'md:flex-col md:px-2' : ''}`}>
        <Link
          to="/dashboard"
          onClick={onCloseMobile}
          className={`flex min-w-0 flex-1 items-center gap-3 rounded-xl px-1 py-2 text-slate-900 no-underline ${
            collapsed ? 'md:justify-center' : ''
          }`}
        >
          <span className={`grid flex-none place-items-center bg-gradient-to-br from-sky-400 to-sky-300 font-extrabold tracking-wider text-white shadow-[0_12px_28px_rgba(56,189,248,0.30)] ${
            collapsed ? 'md:h-9 md:w-9 md:rounded-xl md:text-xs' : 'h-11 w-11 rounded-[14px] text-sm'
          }`}>
            JS
          </span>
          <span className={`min-w-0 leading-tight ${collapsed ? 'md:hidden' : ''}`}>
            <span className="block text-[17px] font-extrabold">Datacenter</span>
            <span className="block text-xs font-medium tracking-wider text-slate-500">ACCOUNTING OFFICE</span>
          </span>
        </Link>
        <button
          type="button"
          onClick={onToggleCollapsed}
          className={`hidden flex-none items-center justify-center rounded-xl border border-slate-200 bg-white font-bold text-slate-600 shadow-sm transition hover:bg-sky-50 md:flex ${
            collapsed ? 'h-8 w-8 text-base' : 'h-9 w-9 text-lg'
          }`}
          aria-label="ย่อหรือขยายเมนู"
        >
          {collapsed ? '›' : '‹'}
        </button>
      </div>

      <nav className={`flex flex-1 flex-col gap-3 px-3 pb-4 ${collapsed ? 'md:gap-2 md:px-2' : ''}`} aria-label="เมนูหลัก">
        {navGroups.map((group) => (
          <div key={group.title} className="relative flex flex-col gap-1">
            <button
              type="button"
              onClick={() => toggleGroup(group.title)}
              className={`group relative flex items-start gap-3 rounded-[14px] px-3 py-3 text-left text-sm text-slate-700 transition hover:translate-x-0.5 hover:bg-sky-50 hover:text-sky-700 ${
                selectedGroupTitle === group.title ? 'bg-gradient-to-br from-sky-50 to-white text-sky-700 ring-1 ring-sky-100' : ''
              } ${collapsed ? 'md:justify-center md:rounded-xl md:px-1.5 md:py-1.5' : ''}`}
              title={group.title}
              aria-expanded={visibleOpenGroup === group.title}
            >
              {selectedGroupTitle === group.title && <span className="absolute bottom-3 left-0 top-3 w-1 rounded-r-full bg-sky-400" />}
              <span className={`grid place-items-center bg-slate-50 text-sky-700 ring-1 ring-slate-100 group-hover:bg-white ${
                collapsed ? 'md:h-10 md:min-w-10 md:rounded-xl' : 'h-12 min-w-12 rounded-2xl'
              }`}>
                <SidebarIcon name={group.icon} className={collapsed ? 'h-6 w-6 md:h-7 md:w-7' : 'h-8 w-8'} />
              </span>
              <span className={`min-w-0 flex-1 leading-tight ${collapsed ? 'md:hidden' : ''}`}>
                <strong className="block truncate font-bold">{group.title}</strong>
                <small className="mt-1 block truncate text-[11px] text-slate-500">{group.desc}</small>
              </span>
              <span className={`mt-1 text-xs font-bold text-slate-400 transition ${visibleOpenGroup === group.title ? 'rotate-90' : ''} ${collapsed ? 'md:hidden' : ''}`}>
                ›
              </span>
            </button>

            {visibleOpenGroup === group.title && (
              <div className={`flex flex-col gap-1 border-sky-100 ${
                collapsed
                  ? 'md:absolute md:left-[64px] md:top-0 md:z-50 md:min-w-[220px] md:rounded-2xl md:border md:bg-white md:p-2 md:shadow-[0_18px_45px_rgba(15,23,42,0.14)]'
                  : 'ml-6 border-l pl-3'
              }`}>
                <div className={`px-3 pb-2 pt-1 ${collapsed ? 'hidden md:block' : 'hidden'}`}>
                  <p className="text-sm font-bold text-sky-700">{group.title}</p>
                  <p className="mt-0.5 truncate text-[11px] text-sky-500">{group.desc}</p>
                </div>
                {group.items.map((item) => {
                  const isItemActive = item.to === currentUrl || (item.to === location.pathname && !location.search)
                  return (
                    <NavLink
                      key={`${group.title}-${item.label}`}
                      to={item.to}
                      onClick={() => {
                        onCloseMobile()
                      }}
                      title={item.label}
                      className={`relative flex items-center rounded-xl px-3 py-2 text-sm font-bold text-slate-600 transition hover:bg-sky-50 hover:text-sky-700 ${
                        isItemActive ? 'bg-sky-50 text-sky-700' : ''
                      }`}
                    >
                      {isItemActive && <span className="mr-2 h-1.5 w-1.5 rounded-full bg-sky-500" />}
                      <span className="block min-w-0 truncate">{item.label}</span>
                    </NavLink>
                  )
                })}
              </div>
            )}
          </div>
        ))}
      </nav>

      <div className={`mx-4 mb-5 flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-500 ${
        collapsed ? 'md:hidden' : ''
      }`}>
        <span className="h-2 w-2 rounded-full bg-green-500 shadow-[0_0_0_4px_rgba(22,163,74,0.10)]" />
        <span>Multi-company workspace</span>
      </div>
    </aside>
  )
}
