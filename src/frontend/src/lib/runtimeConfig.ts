export function getApiBaseUrl(): string {
  const meta = document.querySelector('meta[name="vite-api-url"]') as HTMLMetaElement | null
  const value = (meta?.content ?? '').trim()
  if (value === '__VITE_API_URL__') return ''
  return value
}
