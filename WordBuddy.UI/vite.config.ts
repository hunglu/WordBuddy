import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Path-based proxy mirroring the ingress routing planned for Phase 4 (one origin, routed by
// path prefix to each independent microservice). Only Identity is implemented as of Phase 3 —
// the others will 502/ECONNREFUSED until their services exist and run on these dev ports.
const IDENTITY_URL = 'http://localhost:5080'
const CONTENT_URL = 'http://localhost:5081'
const QUIZ_URL = 'http://localhost:5082'
const PROGRESS_URL = 'http://localhost:5083'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api/auth': { target: IDENTITY_URL, changeOrigin: true },
      '/api/lessons': { target: CONTENT_URL, changeOrigin: true },
      '/api/media': { target: CONTENT_URL, changeOrigin: true },
      '/api/quiz': { target: QUIZ_URL, changeOrigin: true },
      '/api/progress': { target: PROGRESS_URL, changeOrigin: true },
    },
  },
})
