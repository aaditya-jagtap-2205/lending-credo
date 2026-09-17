import { formatPercent } from '../services/format';

const THRESHOLDS = [60, 80, 90];
const SCALE_MAX = 110;

/**
 * Shows where an application's LTV sits against the 60 / 80 / 90 thresholds that
 * drive the required credit score, so the decision is legible at a glance.
 */
export default function LtvRuler({ ltv }) {
  const position = Math.min(Number(ltv), SCALE_MAX) / SCALE_MAX;

  return (
    <div className="ruler" role="img" aria-label={`Loan to value ${formatPercent(ltv)}`}>
      <div className="ruler-track">
        <span className="ruler-band band-low" style={{ width: `${(60 / SCALE_MAX) * 100}%` }} />
        <span className="ruler-band band-mid" style={{ width: `${(20 / SCALE_MAX) * 100}%` }} />
        <span className="ruler-band band-high" style={{ width: `${(10 / SCALE_MAX) * 100}%` }} />
        <span className="ruler-band band-stop" style={{ width: `${(20 / SCALE_MAX) * 100}%` }} />
        <span className="ruler-marker" style={{ left: `${position * 100}%` }} />
      </div>
      <div className="ruler-scale">
        {THRESHOLDS.map((threshold) => (
          <span key={threshold} className="ruler-tick" style={{ left: `${(threshold / SCALE_MAX) * 100}%` }}>
            {threshold}%
          </span>
        ))}
      </div>
    </div>
  );
}
