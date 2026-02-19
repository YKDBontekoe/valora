import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'
import GlobalErrorBoundary from './components/GlobalErrorBoundary'
import Toast from './components/Toast'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <GlobalErrorBoundary>
      <App />
      <Toast />
    </GlobalErrorBoundary>
  </React.StrictMode>,
)
