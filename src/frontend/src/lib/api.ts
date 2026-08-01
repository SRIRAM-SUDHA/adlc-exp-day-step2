import { getApiBaseUrl } from './runtimeConfig'

export type ConversionRequest = {
  amount: number
  fromCurrency: string
  toCurrency: string
}

export type ConversionResponse = {
  auditId: string
  sourceAmount: number
  sourceCurrency: string
  targetCurrency: string
  convertedAmount: number
  rate: number
  providerMarker?: string | null
  backendExecutedAtUtc: string
}

export type ConversionAuditSummary = {
  auditId: string
  convertedAmount: number
  sourceCurrency: string
  targetCurrency: string
  rate: number
  providerMarker?: string | null
  backendExecutedAtUtc: string
}

const jsonHeaders = {
  'Content-Type': 'application/json'
}

function withApiBase(path: string): string {
  const base = getApiBaseUrl()
  if (!base) return path
  return base.replace(/\/$/, '') + path
}

export async function convertCurrency(request: ConversionRequest): Promise<ConversionResponse> {
  const res = await fetch(withApiBase('/api/conversions'), {
    method: 'POST',
    headers: jsonHeaders,
    body: JSON.stringify(request)
  })

  if (!res.ok) {
    const maybeProblem = await res.json().catch(() => null)
    throw new Error(maybeProblem?.detail || `Conversion failed (${res.status})`)
  }

  return res.json()
}

export async function listConversions(limit: number): Promise<ConversionAuditSummary[]> {
  const url = withApiBase(`/api/conversions?limit=${encodeURIComponent(limit)}`)
  const res = await fetch(url)
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(text || `Audit list failed (${res.status})`)
  }

  return res.json()
}

export async function getConversion(auditId: string): Promise<ConversionResponse> {
  const url = withApiBase(`/api/conversions/${encodeURIComponent(auditId)}`)
  const res = await fetch(url)
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(text || `Audit retrieval failed (${res.status})`)
  }

  return res.json()
}
