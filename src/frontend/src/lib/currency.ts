export function normalizeCurrencyCode(input: string): string {
  const trimmed = input.trim()
  if (!trimmed) throw new Error('Currency code is required')
  const code = trimmed.toUpperCase()
  if (code.length !== 3) throw new Error('Currency code must be 3 letters')
  return code
}

export function roundToTwo(value: number): number {
  return Math.round(value * 100) / 100
}
