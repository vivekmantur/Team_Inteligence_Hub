/* Dev reload plugin (Polling + HMR mirror, build errors only)
 *  - Mirrors all HMR websocket events
 *  - Path redaction (<cwd>)
 *  - Polling endpoint for status checks (proxy-friendly)
 *  - CSS-only fast path (cache-bust stylesheets without full reload)
 */

import type { Plugin, ViteDevServer, ModuleNode, HmrContext, Connect } from 'vite';
import type { ServerResponse } from 'node:http';

// Fixed constants
const DEBOUNCE_MS = 80;
const POLL_INTERVAL_MS = 2000; // Poll every 2 seconds
const STATUS_PATH = '/__dev/reload/status';
const CSS_EXTENSIONS = ['.css', '.scss', '.sass', '.less', '.styl', '.pcss'];

/** The fields read from a build or HMR error; any of them may be missing. */
type BuildErrorLike = { message?: string; stack?: string; plugin?: string; id?: string };

/** A build error with the project path replaced by `<cwd>`. */
interface NormalizedBuildError {
  message: string;
  stack: string;
  plugin?: string;
  id?: string;
}

interface PendingChange {
  file: string;
  isCSS: boolean;
  modules: ModuleNode[];
}

interface BuildStatus {
  version: number;
  lastBuildTime: number;
  cssOnly: boolean;
  error?: NormalizedBuildError;
}

export default function devReload(): Plugin {
  let server: ViteDevServer;
  let version = 1;
  let debounceTimer: ReturnType<typeof setTimeout> | null = null;

  const pending = new Map<string, PendingChange>();
  let errorHooksInstalled = false;
  const cwd = process.cwd();
  
  // Store the latest build status
  let latestStatus: BuildStatus = {
    version: 1,
    lastBuildTime: Date.now(),
    cssOnly: false,
  };

  let lastHmrErrorSig: string | null = null;
  let lastHmrErrorTime = 0;
  const HMR_ERROR_DEDUPE_WINDOW_MS = 400;

  const isCSS = (file: string) =>
    CSS_EXTENSIONS.some(ext => file.toLowerCase().endsWith(ext));

  function flushChanges() {
    debounceTimer = null;
    if (!pending.size) return;
    const changes = Array.from(pending.values());
    const cssOnly = changes.every(c => c.isCSS);
    const end = Date.now();
    version++;
    
    // Update the status that clients will poll
    latestStatus = {
      version,
      lastBuildTime: end,
      cssOnly,
    };
    
    pending.clear();
  }

  /** Redacts the project path from an error and keeps only the fields the client logs. */
  function normalizeBuildError(err: BuildErrorLike | null | undefined): NormalizedBuildError {
    return {
      message: (err?.message || String(err)).replaceAll(cwd, '<cwd>'),
      stack: (err?.stack || '').split('\n')
        .map((line: string) => line.replaceAll(cwd, '<cwd>'))
        .join('\n'),
      plugin: err?.plugin,
      id: err?.id ? err.id.replaceAll(cwd, '<cwd>') : undefined,
    };
  }

  // Receives whatever was thrown or rejected, so it is treated as error-like.
  function recordBuildError(err: unknown) {
    latestStatus.error = normalizeBuildError(err as BuildErrorLike);
  }

  function interceptHMR() {
    // `send` is overloaded (an HMR payload object or a custom event name) and is
    // reassigned below, so the patched server is typed loosely here.
    const ws: any = server.ws;
    if (!ws || ws.__devReloadPatched) return;
    ws.__devReloadPatched = true;
    const originalSend = ws.send.bind(ws);
    ws.send = (payload: any, clientsArg?: unknown) => {
      try { originalSend(payload, clientsArg); } catch {
        // Vite's own send failing must not stop the mirror below.
      }
      try {
        if (!payload || !payload.type) return;
        if (payload.type === 'error') {
          const now = Date.now();
          const norm = normalizeBuildError((payload.err || payload.error || payload) as BuildErrorLike);
          const sig = `${norm.plugin || ''}|${norm.id || ''}|${norm.message}`;
          if (sig !== lastHmrErrorSig || now - lastHmrErrorTime > HMR_ERROR_DEDUPE_WINDOW_MS) {
            lastHmrErrorSig = sig;
            lastHmrErrorTime = now;
            latestStatus.error = norm;
          }
        }
      } catch {
        // Mirroring is best-effort; a malformed payload must not break HMR.
      }
    };
  }

  return {
    name: 'dev-reload-polling',
    apply: 'serve',

    configureServer(_server: ViteDevServer) {
      server = _server;
      if (!errorHooksInstalled) {
        errorHooksInstalled = true;
        try {
          process.on('uncaughtException', recordBuildError);
          process.on('unhandledRejection', recordBuildError);
        } catch {
          // Hooks are optional; without them build errors are simply not mirrored.
        }
      }
      
      // Polling endpoint - returns current build status
      server.middlewares.use(STATUS_PATH, (_req: Connect.IncomingMessage, res: ServerResponse) => {
        res.writeHead(200, {
          'Content-Type': 'application/json',
          'Cache-Control': 'no-cache',
          'Access-Control-Allow-Origin': '*',
        });
        res.end(JSON.stringify(latestStatus));
      });
      
      interceptHMR();
    },

    handleHotUpdate(ctx: HmrContext) {
      pending.set(ctx.file, {
        file: ctx.file,
        isCSS: isCSS(ctx.file),
        modules: ctx.modules,
      });
      if (debounceTimer) clearTimeout(debounceTimer);
      debounceTimer = setTimeout(flushChanges, DEBOUNCE_MS);
      return ctx.modules;
    },

    transformIndexHtml(html: string) {
      const script = `
(() => {
  const STATUS_PATH = '${STATUS_PATH}';
  const POLL_INTERVAL = ${POLL_INTERVAL_MS};
  let currentVersion = ${version};
  let isPolling = false;
  
  function applyAllCSS(v) {
    document.querySelectorAll('link[rel="stylesheet"]').forEach(link => {
      const url = new URL(link.href, location.origin);
      url.searchParams.set('v', v);
      const clone = link.cloneNode();
      clone.href = url.toString();
      clone.addEventListener('load', () => link.remove(), { once: true });
      link.after(clone);
    });
  }
  
  async function pollStatus() {
    if (isPolling) return; // Prevent overlapping polls
    isPolling = true;
    
    try {
      const response = await fetch(STATUS_PATH, {
        cache: 'no-cache',
        headers: { 'Accept': 'application/json' }
      });
      
      if (!response.ok) {
        console.warn('[dev-reload] Poll failed:', response.status);
        return;
      }
      
      const status = await response.json();
      
      // Check for build errors
      if (status.error) {
        console.error('[dev-reload][build-error]', status.error);
        return;
      }
      
      // Check if version changed
      if (status.version > currentVersion) {
        console.log('[dev-reload] New build detected, version:', status.version);
        currentVersion = status.version;
        
        if (status.cssOnly) {
          console.log('[dev-reload] CSS-only update, refreshing stylesheets');
          applyAllCSS(status.version);
        } else {
          console.log('[dev-reload] Full reload required');
          location.reload();
        }
      }
    } catch (error) {
      console.warn('[dev-reload] Poll error:', error);
    } finally {
      isPolling = false;
    }
  }
  
  // Start polling
  const pollTimer = setInterval(pollStatus, POLL_INTERVAL);
  
  // Initial poll
  pollStatus();
  
  // Expose API
  (window).__devReload = {
    version: () => currentVersion,
    forceReload: () => location.reload(),
  };
  
  console.log('[dev-reload] Polling started with interval:', POLL_INTERVAL, 'ms');
})();`;
      return {
        html,
        tags: [
          {
            tag: 'script',
            injectTo: 'head',
            attrs: { type: 'module' },
            children: script,
          },
        ],
      };
    },
  };
}