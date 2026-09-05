/* เมนูคู่มือ — โครงเดียวใช้ทุกหน้า (แก้ที่ไฟล์นี้ไฟล์เดียว ทุกหน้าอัปเดตตาม)
 *
 * โครงเมนูสะท้อนเมนูจริงของระบบ (src/frontend/src/shared/components/layout/Sidebar.tsx)
 * เพื่อให้ผู้ใช้หาหน้าคู่มือได้ด้วยชื่อเมนูเดียวกับที่เห็นในโปรแกรม
 * รายการที่ยังไม่มี href = ยังไม่ได้เขียนคู่มือ จะแสดงเป็น "เร็วๆ นี้"
 *
 * วิธีใช้ในแต่ละหน้า:
 *   <aside class="sidebar" data-nav></aside>
 *   <script src="assets/nav.js" data-root="" data-current="index.html"></script>
 *   (หน้าใน pages/ ใช้ data-root="../" และ data-current="pages/xxx.html")
 */
(function () {
  var NAV = [
    {
      title: 'เริ่มต้น',
      items: [
        { label: 'ภาพรวมระบบ', href: 'index.html' },
      ],
    },
    {
      title: 'ภาพรวม',
      items: [
        { label: 'Dashboard', href: 'pages/dashboard.html' },
        { label: 'ปฏิทินงาน' },
        { label: 'งาน / มอบหมายงาน' },
      ],
    },
    {
      title: 'ข้อมูลและนำเข้า',
      items: [
        { label: 'ลูกค้า', href: 'pages/clients.html' },
        { label: 'นำเข้าข้อมูล', href: 'pages/import.html' },
      ],
    },
    {
      title: 'บัญชี',
      items: [
        { label: 'งบทดลอง', href: 'pages/trial-balance.html' },
        { label: 'บัญชีแยกประเภท', href: 'pages/general-ledger.html' },
        { label: 'ลูกหนี้', href: 'pages/ar.html' },
        { label: 'เจ้าหนี้', href: 'pages/ap.html' },
        { label: 'สินค้าคงคลัง', href: 'pages/stock.html' },
        { label: 'ธนาคาร / สมุดเงินฝาก', href: 'pages/bank.html' },
      ],
    },
    {
      title: 'เงินเดือน',
      items: [
        { label: 'เงินเดือน' },
        { label: 'ภ.ง.ด.1' },
        { label: 'ประกันสังคม' },
      ],
    },
    {
      title: 'ภาษี',
      items: [
        { label: 'ภาษีมูลค่าเพิ่ม', href: 'pages/vat.html' },
        { label: 'ภ.ง.ด.50' },
        { label: 'หัก ณ ที่จ่าย' },
      ],
    },
    {
      title: 'รายงานและปิดงวด',
      items: [
        { label: 'กระดาษทำการปิดงบ' },
        { label: 'เช่าซื้อ / เงินกู้' },
        { label: 'สินทรัพย์ถาวร' },
        { label: 'ค่าใช้จ่ายจ่ายล่วงหน้า' },
        { label: 'ตรวจนับเงินสด' },
        { label: 'ดอกเบี้ยรับเงินให้กู้' },
        { label: 'ตรวจจ่ายหลังปิดงบ' },
        { label: 'งบการเงิน' },
        { label: 'ปิดรอบบัญชี' },
        { label: 'ชุดรายงานงบ' },
        { label: 'คลังเอกสาร / หลักฐาน' },
      ],
    },
    {
      title: 'ระบบ',
      items: [
        { label: 'ผู้ใช้งานระบบ' },
        { label: 'โปรไฟล์สำนักงานบัญชี' },
        { label: 'ทะเบียนผู้สอบ / ผู้ทำบัญชี' },
        { label: 'ประวัติการใช้งาน' },
      ],
    },
  ]

  var script = document.currentScript
  var root = script.getAttribute('data-root') || ''
  var current = script.getAttribute('data-current') || ''

  var html = [
    '<div class="brand">',
    '<span class="logo">JS</span>',
    '<span><b>Datacenter</b><small>คู่มือการใช้งาน</small></span>',
    '</div>',
  ]

  NAV.forEach(function (group) {
    html.push('<div class="nav-group"><div class="nav-title">' + group.title + '</div>')
    group.items.forEach(function (item) {
      if (!item.href) {
        html.push('<span class="todo">' + item.label + '</span>')
      } else if (item.href === current) {
        html.push('<a href="' + root + item.href + '" class="active">' + item.label + '</a>')
      } else {
        html.push('<a href="' + root + item.href + '">' + item.label + '</a>')
      }
    })
    html.push('</div>')
  })

  var mount = document.querySelector('[data-nav]')
  if (mount) mount.innerHTML = html.join('')
})()
