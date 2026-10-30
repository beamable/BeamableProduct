import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Builds the page `beam local management` serves into cli/cli/LocalWeb, which cli.csproj embeds in the CLI.
export default defineConfig({
  plugins: [react()],
  base: './',
  build: {
    outDir: '../cli/LocalWeb',
    emptyOutDir: true,
  },
})
