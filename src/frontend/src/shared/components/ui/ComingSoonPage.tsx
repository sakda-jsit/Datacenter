import PageHeader from './PageHeader'
import StateMessage from './StateMessage'

interface ComingSoonPageProps {
  title: string
  description?: string
}

export default function ComingSoonPage({ title, description = 'โมดูลนี้อยู่ระหว่างการพัฒนา' }: ComingSoonPageProps) {
  return (
    <div>
      <PageHeader title={title} />
      <StateMessage>{description}</StateMessage>
    </div>
  )
}
