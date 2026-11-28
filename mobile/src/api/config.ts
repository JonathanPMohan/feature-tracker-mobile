// Where the API lives (M2, rule B15).
//
// EXPO_PUBLIC_API_URL comes from mobile/.env.local, which is gitignored
// because it holds your Mac's address on your Wi-Fi. Copy .env.example to
// .env.local and fill it in.
//
// How it works: Expo reads .env files when it bundles the app and replaces
// `process.env.EXPO_PUBLIC_...` in the code with the value. Only names that
// start with EXPO_PUBLIC_ are replaced, and they're visible to anyone who
// has the app, so never put a secret in one. Next.js had the same rule with
// NEXT_PUBLIC_. The replacement only works when written exactly as
// `process.env.EXPO_PUBLIC_API_URL` (no destructuring, no
// `process.env[name]`), because Expo looks for that literal text.

/**
 * The API's base URL without a trailing slash, for example
 * `http://192.168.1.20:5080`, or `null` when it isn't set.
 */
export function getApiUrl(): string | null {
  const url = process.env.EXPO_PUBLIC_API_URL?.trim();
  if (!url) {
    return null;
  }
  // Drop trailing slashes so `${url}/health` never becomes `...5080//health`.
  return url.replace(/\/+$/, "");
}
