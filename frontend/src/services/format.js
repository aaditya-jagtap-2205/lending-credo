const gbp = new Intl.NumberFormat('en-GB', {
  style: 'currency',
  currency: 'GBP',
  maximumFractionDigits: 0,
});

export const formatMoney = (value) => gbp.format(Number(value ?? 0));

export const formatPercent = (value) => `${Number(value ?? 0).toFixed(2)}%`;

export const formatDate = (value) =>
  new Date(value).toLocaleString('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
