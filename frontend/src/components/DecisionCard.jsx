import LtvRuler from './LtvRuler';
import { formatMoney, formatPercent } from '../services/format';

export default function DecisionCard({ result }) {
  if (!result) {
    return (
      <section className="panel decision empty">
        <h2>Decision</h2>
        <p className="empty-copy">
          Enter a loan amount, the value of the asset it is secured against, and the applicant&rsquo;s
          credit score. The decision and LTV appear here.
        </p>
      </section>
    );
  }

  const successful = result.decision === 'Successful';

  return (
    <section className={`panel decision ${successful ? 'is-successful' : 'is-declined'}`} aria-live="polite">
      <h2>Decision</h2>
      <p className="verdict">{successful ? 'Successful' : 'Declined'}</p>
      <p className="verdict-reason">{result.reason}</p>

      <div className="ltv-readout">
        <span className="ltv-value">{formatPercent(result.ltv)}</span>
        <span className="ltv-label">loan to value</span>
      </div>

      <LtvRuler ltv={result.ltv} />

      <dl className="decision-facts">
        <div>
          <dt>Loan</dt>
          <dd>{formatMoney(result.loanAmount)}</dd>
        </div>
        <div>
          <dt>Asset</dt>
          <dd>{formatMoney(result.assetValue)}</dd>
        </div>
        <div>
          <dt>Credit score</dt>
          <dd>{result.creditScore}</dd>
        </div>
      </dl>
    </section>
  );
}
