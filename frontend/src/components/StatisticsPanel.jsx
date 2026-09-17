import { formatMoney, formatPercent } from '../services/format';

export default function StatisticsPanel({ statistics }) {
  const stats = statistics ?? { successfulApplicants: 0, declinedApplicants: 0, totalValueOfLoansWritten: 0, meanLtv: 0 };
  const total = stats.successfulApplicants + stats.declinedApplicants;
  return (
    <div className="stats">
      <Stat label="Lent to date" value={formatMoney(stats.totalValueOfLoansWritten)} emphasis meta="successful loans" />
      <Stat label="Applications" value={total} meta="all assessments" />
      <Stat label="Success rate" value={total ? `${Math.round((stats.successfulApplicants / total) * 100)}%` : '—'} meta={`${stats.successfulApplicants} approved`} />
      <Stat label="Mean LTV" value={formatPercent(stats.meanLtv)} meta="across all applications" />
    </div>
  );
}

function Stat({ label, value, emphasis, meta }) {
  return <div className={`stat ${emphasis ? 'stat-lead' : ''}`}><span className="stat-label">{label}</span><span className="stat-value">{value}</span><span className="stat-meta">{meta}</span></div>;
}
