import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'
import { RideProvider } from './context/RideContext.jsx'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <RideProvider>
      <App />
    </RideProvider>
  </React.StrictMode>,
)
