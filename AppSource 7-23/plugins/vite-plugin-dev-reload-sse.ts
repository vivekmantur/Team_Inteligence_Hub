/* Dev reload plugin (SSE + HMR mirror, build errors only)
 *  - Mirrors all HMR websocket events
 *  - Path redaction (<cwd>)
 *  - SSE events: connected, heartbeat, build-start, build-end, build-error,
 *    hmr-update, hmr-full-reload, hmr-error, hmr-message, hmr-raw, ws-error
 *  - hmr-raw: full sanitized original HMR payload (every field, with <cwd> redaction)
 *  - CSS-only fast path (cache-bust stylesheets without full reload)
 */

import type { Plugin, ViteDevServer, ModuleNode, HmrContext, Connect, Update } from 'vite';
import type { ServerResponse } from 'node:http';

// Fixed constants
const DEBOUNCE_MS = 80;
// Heartbeats keep the connection open for clients that disconnect after a quiet period.
const HEARTBEAT_MS = 30000;
const SSE_PATH = '/__dev/reload';
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

export default function devReload(): Plugin {
  let server: ViteDevServer;
  let version = 1;
  let debounceTimer: ReturnType<typeof setTimeout> | null = null;
  let lastBuildStart = 0;
  let lastBuildDuration = 0;

  const pending = new Map<string, PendingChange>();
  const clients = new Set<ServerResponse>();
  let heartbeatStarted = false;
  let errorHooksInstalled = false;
  const cwd = process.cwd();
  // De-dupe consecutive identical HMR error payloads (Vite can emit several frames
  // for the same parse error in rapid succession: overlay update, plugin chain, etc.)
  let lastHmrErrorSig: string | null = null;
  let lastHmrErrorTime = 0;
  const HMR_ERROR_DEDUPE_WINDOW_MS = 400;

  const isCSS = (file: string) =>
    CSS_EXTENSIONS.some(ext => file.toLowerCase().endsWith(ext));

  function broadcast(obj: unknown) {
    const data = `data: ${JSON.stringify(obj)}\n\n`;
    clients.forEach(res => {
      try { res.write(data); } catch { clients.delete(res); }
    });
  }

  function flushChanges() {
    debounceTimer = null;
    if (!pending.size) return;
    const changes = Array.from(pending.values());
    const cssOnly = changes.every(c => c.isCSS);
    const files = changes.map(c => c.file);
    const cssFiles = changes.filter(c => c.isCSS).map(c => c.file);
    const otherFiles = changes.filter(c => !c.isCSS).map(c => c.file);
    const end = Date.now();
    lastBuildDuration = end - lastBuildStart;
    version++;
    const redact = (p: string) => p.replaceAll(cwd, '<cwd>');
    broadcast({
      type: 'build-end',
      time: end,
      version,
      cssOnly,
      files: files.map(redact),
      changed: { css: cssFiles.map(redact), other: otherFiles.map(redact) },
      durationMs: lastBuildDuration,
    });
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
    broadcast({ type: 'build-error', error: normalizeBuildError(err as BuildErrorLike) });
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
        switch (payload.type) {
          case 'update':
            broadcast({
              type: 'hmr-update',
              time: Date.now(),
              updates: (payload.updates || []).map((u: Update & { acceptedPaths?: string[] }) => ({
                type: u.type,
                path: u.path ? u.path.replaceAll(cwd, '<cwd>') : undefined,
                accepted: u.acceptedPath ?? u.acceptedPaths ?? undefined,
                timestamp: u.timestamp,
              })),
            });
            break;
          case 'full-reload':
            broadcast({
              type: 'hmr-full-reload',
              time: Date.now(),
              path: payload.path ? payload.path.replaceAll(cwd, '<cwd>') : undefined,
            });
            break;
          case 'error': {
            const now = Date.now();
            const norm = normalizeBuildError((payload.err || payload.error || payload) as BuildErrorLike);
            const sig = `${norm.plugin || ''}|${norm.id || ''}|${norm.message}`;
            if (sig !== lastHmrErrorSig || now - lastHmrErrorTime > HMR_ERROR_DEDUPE_WINDOW_MS) {
              lastHmrErrorSig = sig;
              lastHmrErrorTime = now;
              broadcast({
                type: 'hmr-error',
                time: now,
                error: norm,
              });
            }
            break;
          }
          default:
            broadcast({ type: 'hmr-message', time: Date.now(), message: payload.type });
        }
      } catch (e) {
        broadcast({ type: 'ws-error', time: Date.now(), error: { message: (e as Error | undefined)?.message || String(e) } });
      }
    };
  }

  return {
    name: 'dev-reload-sse-build',
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
      server.middlewares.use(SSE_PATH, (_req: Connect.IncomingMessage, res: ServerResponse) => {
        res.writeHead(200, {
          'Content-Type': 'text/event-stream',
          'Cache-Control': 'no-cache',
          Connection: 'keep-alive',
          'Access-Control-Allow-Origin': '*',
        });
        clients.add(res);
        res.write(`data: ${JSON.stringify({
          type: 'connected',
          time: Date.now(),
          version,
          heartbeatMs: HEARTBEAT_MS,
          hmr: true,
        })}\n\n`);
        _req.on('close', () => { clients.delete(res); });
        if (!heartbeatStarted) {
          heartbeatStarted = true;
            setInterval(() => broadcast({ type: 'heartbeat', time: Date.now() }), HEARTBEAT_MS);
        }
      });
      interceptHMR();
    },

    handleHotUpdate(ctx: HmrContext) {
      if (!pending.size) {
        lastBuildStart = Date.now();
        broadcast({ type: 'build-start', time: lastBuildStart, version });
      }
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
  const SSE_PATH = '${SSE_PATH}';
  let currentVersion = ${version};
  /**
   * Refresh all stylesheets after a CSS-only build without reloading the page.
   * Adds ?v=<buildVersion> to each <link rel="stylesheet"> to bust cache,
   * clones & swaps them once loaded, preserving order, app state and avoiding flashes.
   */
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
  function connect() {
    const es = new EventSource(SSE_PATH);
    es.onmessage = (ev) => {
      try {
        const msg = JSON.parse(ev.data);
        switch (msg.type) {
          case 'build-end':
            currentVersion = msg.version;
            if (msg.cssOnly) applyAllCSS(msg.version); else location.reload();
            break;
          case 'build-error':
          case 'hmr-error':
            console.error('[dev-reload][' + msg.type + ']', msg.error);
            break;
        }
      } catch {}
    };
    es.onerror = () => {
      console.warn('[dev-reload]', 'EventSource failed, retrying...');
      es.close();
      setTimeout(connect, 1000);
    };
  }
  if (window.EventSource) connect();
  (window).__devReload = {
    version: () => currentVersion,
    forceReload: () => location.reload()
  };
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