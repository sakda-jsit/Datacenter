import { BrowserRouter } from 'react-router-dom'
import AppRouter from './routes/AppRouter'
import { CurrentCompanyProvider } from './shared/hooks/useCurrentCompany'

export default function App() {
  return (
    <BrowserRouter>
      <CurrentCompanyProvider>
        <AppRouter />
      </CurrentCompanyProvider>
    </BrowserRouter>
  )
}
