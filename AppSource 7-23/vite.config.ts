import path from 'path';
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// Simple dev-only request logger to help debug localhost issues
function requestLogger() {
  return {
    name: 'vite:request-logger',
    apply: 'serve' as const,
    configureServer(server: any) {
      server.middlewares.use((req: any, _res: any, next: any) => {
        try {
          const now = new Date().toISOString();
          // eslint-disable-next-line no-console
          console.log(`${now} [vite:req] ${req.method} ${req.url}`);
        } catch (e) {}
        return next();
      });
    },
  };
}

// https://vite.dev/config/
export default defineConfig({
  base: './',
  // Temporarily disable devReload and propagateQueryPlugin to isolate 404 cause
  plugins: [requestLogger(), react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    rollupOptions: {
      output: {
        // https://rollupjs.org/configuration-options/#output-inlinedynamicimports
        // will inline dynamic imports instead of creating new chunks to create a single bundle
        inlineDynamicImports: true,
      },
    },
  },
  optimizeDeps: {
    include: [
      '@azure/msal-browser',
      '@azure/msal-browser/redirect-bridge',
      '@azure/msal-react',
      '@dnd-kit/core',
      '@dnd-kit/sortable',
      '@dnd-kit/utilities',
      '@microsoft/power-apps/app',
      '@radix-ui/react-accordion',
      '@radix-ui/react-aspect-ratio',
      '@radix-ui/react-avatar',
      '@radix-ui/react-checkbox',
      '@radix-ui/react-collapsible',
      '@radix-ui/react-context-menu',
      '@radix-ui/react-dialog',
      '@radix-ui/react-dropdown-menu',
      '@radix-ui/react-label',
      '@radix-ui/react-menubar',
      '@radix-ui/react-navigation-menu',
      '@radix-ui/react-progress',
      '@radix-ui/react-radio-group',
      '@radix-ui/react-scroll-area',
      '@radix-ui/react-separator',
      '@radix-ui/react-slot',
      '@radix-ui/react-switch',
      '@radix-ui/react-toggle',
      '@tanstack/react-query',
      '@tanstack/react-table',
      'class-variance-authority',
      'clsx',
      'cmdk',
      'd3',
      'date-fns',
      'embla-carousel-react',
      'framer-motion',
      'jotai',
      'lucide-react',
      'next-themes',
      'react-day-picker',
      'react-dom/client',
      'react-resizable-panels',
      'react-router-dom',
      'recharts',
      'tailwind-merge',
      'vaul',
    ],
  },
  server: {
    port: 5173,
    host: true,
    hmr: {
      overlay: false,
    },
    watch: {
      ignored: ['**/.app-builder/**']
    }
  },
});
