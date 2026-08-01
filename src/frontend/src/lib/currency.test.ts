import { describe, expect, it } from 'vitest'
import { normalizeCurrencyCode, roundToTwo } from './currency'

describe('currency helpers', () => {
  it('normalizes to uppercase 3-letter codes', () => {
    expect(normalizeCurrencyCode('usd')).toBe('USD')
  })

  it('rounds to 2 decimals', () => {
    expect(roundToTwo(1.234)).toBe(1.23)
    expect(roundToTwo(1.236)).toBe(1.24)
  })
})
