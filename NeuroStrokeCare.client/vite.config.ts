import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    host: true,
    proxy: {
      // Forwards /api calls to the local ASP.NET Core API during `npm run dev`,
      // matching the relative '/api' base URL used by src/lib/api.ts. Update the
      // target if your API runs on a different port.
      '/api': {
        target: 'http://localhost:5271',
        changeOrigin: true,
      },
      // Staff ID card photos, served by the API from its uploads folder.
      '/uploads': {
        target: 'http://localhost:5271',
        changeOrigin: true,
      },
    },
  },
})
