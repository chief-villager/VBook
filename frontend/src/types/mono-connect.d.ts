// Ambient types for `@mono.co/connect.js` (v2.x), which ships no type definitions.
// Covers only the surface we use: constructing the widget, opening it, and the
// account-linked success payload that carries the authorisation `code`.

declare module '@mono.co/connect.js' {
  export interface MonoConnectSuccessData {
    // Short-lived authorisation code to exchange server-side for a durable account.
    code: string
    [key: string]: unknown
  }

  export interface MonoConnectOptions {
    key: string
    onSuccess: (data: MonoConnectSuccessData) => void
    onClose?: () => void
    onLoad?: () => void
    onEvent?: (eventName: string, data: unknown) => void
    scope?: string
    data?: Record<string, unknown>
  }

  export default class Connect {
    constructor(options: MonoConnectOptions)
    setup(config?: Record<string, unknown>): void
    open(): void
    close(): void
    reauthorise(accountId: string): void
  }
}
