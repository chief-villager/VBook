// Placeholder for the "Vbook Onboarding" screen from Claude Design.
// TODO: implement from the design once the Claude Design files are available:
//   - Vbook Onboarding.dc.html
//   - _ds/industry-428b0a48-.../_ds_bundle.js  (component bundle)
//   - _ds/industry-428b0a48-.../styles.css     (tokens + component CSS)
//   - support.js
// The design-system bundle is expected to expose components on a window global;
// wire it up here (or under src/ds/) when the files land.

export default function Onboarding() {
  return (
    <main style={{ maxWidth: 640, margin: '4rem auto', padding: '0 1.5rem' }}>
      <h1>VBook Onboarding</h1>
      <p>
        Frontend scaffold is ready. This screen is a placeholder for the
        <code> Vbook Onboarding.dc.html </code> design.
      </p>
      <p>
        The API client lives in <code>src/lib/api.ts</code> and the token store in{' '}
        <code>src/lib/auth.ts</code>. Set <code>VITE_API_URL</code> in <code>.env</code>{' '}
        to point at the running Bookkeeping API.
      </p>
    </main>
  )
}
