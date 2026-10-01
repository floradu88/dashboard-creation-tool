import { Measured } from './dashboard-api';

export function measuredText(value: Measured): string {
  if (value.state === 'Value' || value.state === 'Zero') {
    const amount = value.value ?? 0;
    if (value.unit === 'Percent') return `${amount}%`;
    if (value.unit === 'Day') return `${amount} days`;
    if (value.unit === 'Count') return `${amount}`;
    if (value.unit === 'Ratio') return `${amount}`;
    if (value.unit && /^[A-Z]{3}$/.test(value.unit)) return new Intl.NumberFormat(undefined, { style: 'currency', currency: value.unit, maximumFractionDigits: 2 }).format(amount);
    return value.unit ? `${amount} ${value.unit}` : `${amount}`;
  }
  if (value.state === 'NotApplicable') return 'N/A';
  if (value.state === 'Unavailable') return 'Unavailable';
  return 'Missing';
}
