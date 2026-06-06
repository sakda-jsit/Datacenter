import PageHeader from '../../../shared/components/ui/PageHeader'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import EmployeesTab from './tabs/EmployeesTab'

export default function PayrollPage() {
  const { companyId } = useCurrentCompany()

  return (
    <div>
      <PageHeader
        title="เงินเดือน / ประกันสังคม"
        description="ทะเบียนพนักงาน + คลังหลักฐาน (รูปบัตร/หลักฐานแจ้ง ปกส.) + แจ้งเข้า-ออกประกันสังคม — ข้อมูลส่วนบุคคล (PDPA)"
      />
      {!companyId ? (
        <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
      ) : (
        <EmployeesTab companyId={companyId} />
      )}
    </div>
  )
}
