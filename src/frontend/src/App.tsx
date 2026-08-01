import { type FormEvent, useEffect, useMemo, useState } from 'react'
import {
  type ConversionAuditSummary,
  type ConversionRequest,
  type ConversionResponse,
  convertCurrency,
  getConversion,
  listConversions
} from './lib/api'
import { normalizeCurrencyCode } from './lib/currency'

export default function App() {
  const currencies = useMemo(
    () => [
      'USD',
      'EUR',
      'GBP',
      'JPY',
      'CAD',
      'AUD',
      'CHF',
      'CNY',
      'INR',
      'BRL',
      'SGD',
      'NZD'
    ],
    [],
  )

  const [amount, setAmount] = useState<string>('')
  const [fromCurrency, setFromCurrency] = useState<string>('USD')
  const [toCurrency, setToCurrency] = useState<string>('EUR')

  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<ConversionResponse | null>(null)

  const [limit] = useState(10)
  const [audits, setAudits] = useState<ConversionAuditSummary[]>([])
  const [selected, setSelected] = useState<ConversionResponse | null>(null)
  const [auditIdQuery, setAuditIdQuery] = useState('')

  async function refreshList() {
    const list = await listConversions(limit)
    setAudits(list)
  }

  useEffect(() => {
    refreshList().catch((e) => setError(e instanceof Error ? e.message : String(e)))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function onSubmitConversion(e: FormEvent) {
    e.preventDefault()
    setError(null)

    const amt = Number(amount)
    if (!Number.isFinite(amt) || amt <= 0) {
      setError('amount must be a positive number')
      return
    }

    const req: ConversionRequest = {
      amount: amt,
      fromCurrency: normalizeCurrencyCode(fromCurrency),
      toCurrency: normalizeCurrencyCode(toCurrency)
    }

    setBusy(true)
    try {
      const r = await convertCurrency(req)
      setResult(r)
      await refreshList().catch(() => {})
      setSelected(null)
      setAuditIdQuery(r.auditId)
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setBusy(false)
    }
  }

  async function fetchSelected(auditId: string) {
    setError(null)
    setSelected(null)
    setBusy(true)
    try {
      const r = await getConversion(auditId)
      setSelected(r)
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err))
    } finally {
      setBusy(false)
    }
  }

  function formatMoney(value: number) {
    return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  }

  return (
    <div className="app">
      <div className="container">
        <div className="header">
          <div>
            <div className="title">Real-Time Currency Conversion & Audit Trail</div>
            <div className="subtitle">Instant quotes, immutable stored evidence for audits.</div>
          </div>
          <div className="pill">
            <span className="mono">POST /api/conversions</span>
          </div>
        </div>

        <div className="grid">
          <div className="card">
            <div className="cardTitle">New Conversion</div>

            <form onSubmit={onSubmitConversion}>
              <div className="field">
                <label>Amount</label>
                <input
                  inputMode="decimal"
                  placeholder="100.00"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                />
              </div>

              <div className="row">
                <div className="field">
                  <label>From</label>
                  <input list="currency-codes" value={fromCurrency} onChange={(e) => setFromCurrency(e.target.value)} />
                </div>
                <div className="field">
                  <label>To</label>
                  <input list="currency-codes" value={toCurrency} onChange={(e) => setToCurrency(e.target.value)} />
                </div>
              </div>

              <datalist id="currency-codes">
                {currencies.map((c) => (
                  <option key={c} value={c} />
                ))}
              </datalist>

              <div className="actions">
                <button disabled={busy} type="submit">
                  {busy ? 'Processing…' : 'Convert'}
                </button>
                <div className="small">Converted amounts are stored for audit reconstruction.</div>
              </div>
            </form>

            {error ? <div className="error">{error}</div> : null}

            {result ? (
              <div style={{ marginTop: 14 }} className="result">
                <div className="cardTitle">Conversion Result</div>
                <div className="pill">
                  <span>Converted:</span>
                  <span className="mono">
                    {formatMoney(result.convertedAmount)} {result.targetCurrency}
                  </span>
                </div>
                <div className="pill">
                  <span>Rate:</span>
                  <span className="mono">{result.rate}</span>
                </div>
                <div className="pill">
                  <span>Provider marker:</span>
                  <span className="mono">{result.providerMarker ?? 'N/A'}</span>
                </div>
                <div className="pill">
                  <span>Backend executed at (UTC):</span>
                  <span className="mono">{new Date(result.backendExecutedAtUtc).toISOString()}</span>
                </div>
                <div className="field" style={{ marginBottom: 0 }}>
                  <label>Audit ID</label>
                  <input className="mono" value={auditIdQuery || result.auditId} onChange={(e) => setAuditIdQuery(e.target.value)} />
                </div>
                <div className="actions" style={{ marginTop: 10 }}>
                  <button disabled={busy} type="button" onClick={() => fetchSelected(result.auditId)}>
                    View stored record
                  </button>
                  <div className="small">Evidence is immutable and not recalculated from live rates.</div>
                </div>
              </div>
            ) : null}
          </div>

          <div className="card">
            <div className="cardTitle">Audit Lookup</div>

            <div className="field">
              <label>Audit ID</label>
              <input
                className="mono"
                value={auditIdQuery}
                placeholder="e.g. 7b3a…"
                onChange={(e) => setAuditIdQuery(e.target.value)}
              />
            </div>

            <button disabled={busy || !auditIdQuery} type="button" onClick={() => fetchSelected(auditIdQuery)}>
              {busy ? 'Loading…' : 'Retrieve'}
            </button>

            {selected ? (
              <div style={{ marginTop: 14 }} className="result">
                <div className="cardTitle">Stored Record</div>
                <div className="pill">
                  <span>Converted:</span>
                  <span className="mono">
                    {formatMoney(selected.convertedAmount)} {selected.targetCurrency}
                  </span>
                </div>
                <div className="pill">
                  <span>Rate:</span>
                  <span className="mono">{selected.rate}</span>
                </div>
                <div className="pill">
                  <span>Provider marker:</span>
                  <span className="mono">{selected.providerMarker ?? 'N/A'}</span>
                </div>
                <div className="pill">
                  <span>Backend executed at (UTC):</span>
                  <span className="mono">{new Date(selected.backendExecutedAtUtc).toISOString()}</span>
                </div>
              </div>
            ) : null}

            <div style={{ marginTop: 16 }} className="cardTitle">
              Recent conversions
              <div className="small">(latest {limit})</div>
            </div>

            <div className="list">
              {audits.length === 0 ? <div className="small">No audit records yet.</div> : null}
              {audits.map((a) => (
                <div className="listItem" key={a.auditId}>
                  <div className="listTop">
                    <div>
                      <div className="mono">{formatMoney(a.convertedAmount)} {a.targetCurrency}</div>
                      <div className="small">
                        {a.sourceCurrency} → {a.targetCurrency} • rate {a.rate}
                      </div>
                    </div>
                    <button className="linkButton" disabled={busy} type="button" onClick={() => fetchSelected(a.auditId)}>
                      View
                    </button>
                  </div>
                  <div className="small mono">{a.auditId}</div>
                  <div className="small">{new Date(a.backendExecutedAtUtc).toISOString()}</div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
