import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: "../Arona.ApiService/wwwroot/",
    assetsDir: "./assets",
    emptyOutDir: true,
  }
})
