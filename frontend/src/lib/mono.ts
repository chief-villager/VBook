// Launches the Mono Connect widget so a user can pick and authorise their bank.
// The widget runs entirely client-side against Mono using the *public* key; on a
// successful link it hands back a short-lived authorisation code, which the caller
// exchanges for a durable account by POSTing it to the API (the API holds the
// secret key). See Infrastructure/Mono + BankAccountsController on the API side.

import Connect from '@mono.co/connect.js'

const PUBLIC_KEY = import.meta.env.VITE_MONO_PUBLIC_KEY

export class MonoNotConfiguredError extends Error {
  constructor() {
    super('Mono is not configured — set VITE_MONO_PUBLIC_KEY in the frontend .env')
    this.name = 'MonoNotConfiguredError'
  }
}

interface OpenMonoConnectOptions {
  // Called with the authorisation code once the user finishes linking a bank.
  onCode: (code: string) => void
  onClose?: () => void
}

// Opens the widget. Throws MonoNotConfiguredError if the public key is missing so
// the caller can surface a clear message rather than a blank widget.
export function openMonoConnect({ onCode, onClose }: OpenMonoConnectOptions): void {
  if (!PUBLIC_KEY) throw new MonoNotConfiguredError()

  const connect = new Connect({
    key: PUBLIC_KEY,
    onSuccess: ({ code }) => onCode(code),
    onClose,
  })
  connect.setup()
  connect.open()
}
