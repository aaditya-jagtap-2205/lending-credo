import LtvRuler from './LtvRuler';
import { formatMoney, formatPercent } from '../services/format';

export default function DecisionCard({ result }) {
  if (!result) {
    return <section className="panel decision empty"><div className="decision-placeholder"><span className="placeholder-orbit">✦</span><p className="eyebrow">Your assessment</p><h2>Decision awaits</h2><p className="empty-copy">Complete the application details and your outcome will appear here, with the reasoning behind it.</p></div></section>;
  }
  const successful = result.decision === 'Successful';
  return (
    <section className={`panel decision ${successful ? 'is-successful' : 'is-declined'}`} aria-live="polite">
      <div className="decision-topline"><span className="eyebrow">Assessment result</span><span className={`decision-icon ${successful ? 'success-icon' : 'decline-icon'}`}>{successful ? '✓' : '×'}</span></div>
      <p className="verdict">{successful ? 'Successful' : 'Declined'}</p>
      <p className="verdict-reason">{result.reason}</p>
      <div className="ltv-readout"><span className="ltv-value">{formatPercent(result.ltv)}</span><span className="ltv-label">loan to value</span></div>
      <LtvRuler ltv={result.ltv} />
      <dl className="decision-facts">
        <div><dt>Loan amount</dt><dd>{formatMoney(result.loanAmount)}</dd></div>
        <div><dt>Asset value</dt><dd>{formatMoney(result.assetValue)}</dd></div>
        <div><dt>Credit score</dt><dd>{result.creditScore}</dd></div>
      </dl>
    </section>
  );
}
