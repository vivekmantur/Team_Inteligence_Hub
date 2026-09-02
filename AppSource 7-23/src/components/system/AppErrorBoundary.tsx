import { Component, ErrorInfo, type ReactNode } from "react";

import { ErrorMessage } from "@/types/AppMessage";

interface AppErrorBoundaryProps { children: ReactNode }
interface AppErrorBoundaryState { hasError: boolean; error?: Error }

/**
 * Simple error boundary for route/page level errors.
 * NOTE(ai): Keeps implementation minimal; extend with logging or user messaging as needed.
 */
export class AppErrorBoundary extends Component<AppErrorBoundaryProps, AppErrorBoundaryState> {
    state: AppErrorBoundaryState = { hasError: false }

    static getDerivedStateFromError(error: Error): AppErrorBoundaryState {
        return { hasError: true, error }
    }

    componentDidCatch(error: Error, errorInfo: ErrorInfo) {
        // TODO(ai): optional instrumentation hook (e.g., memory.put('last-error', ...))
        // NOTE(ai): Avoid console noise in production; this is acceptable for dev scaffolding.
        console.error("Route error boundary caught: ", error, errorInfo)
        
        // Post error message to parent window
        try {
            const message: ErrorMessage = {
                type: 'error',
                source: 'AppErrorBoundary',
                payload: {
                    name: error.name,
                    message: error.message,
                    stack: error.stack,
                    componentStack: errorInfo.componentStack,
                }
            };
            window.parent?.postMessage(message, '*');
        } catch (postMessageError) {
            console.error('Failed to post error message:', postMessageError);
        }
    }

    render() {
        if (this.state.hasError) {
            return (
                <div className="p-6 text-sm space-y-3 max-w-md">
                    <div className="font-semibold">Something went wrong loading this view.</div>
                    <div className="text-muted-foreground break-words">
                        {this.state.error?.message}
                    </div>
                    <button
                        className="inline-flex items-center rounded-md border px-3 py-1.5 text-xs hover:bg-accent hover:text-accent-foreground transition-colors"
                        onClick={() => this.setState({ hasError: false, error: undefined })}
                    >
                        Retry render
                    </button>
                </div>
            )
        }
        return this.props.children
    }
}
