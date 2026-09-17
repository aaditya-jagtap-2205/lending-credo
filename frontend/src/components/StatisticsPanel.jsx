import { formatMoney, formatPercent } from '../services/format';

export default function StatisticsPanel({ statistics }) {
  const stats = statistics ?? {
    successfulApplicants: 0,
    declinedApplicants: 0,
    totalValueOfLoansWritten: 0,
    meanLtv: 0,
  };

  return (
    <section className="stats">
      <Stat label="Lent to date" value={formatMoney(stats.totalValueOfLoansWritten)} emphasis />
      <Stat label="Successful" value={stats.successfulApplicants} />
      <Stat label="Declined" value={stats.declinedApplicants} />
      <Stat label="Mean LTV, all applications" value={formatPercent(stats.meanLtv)} />
    </section>
  );
}

function Stat({ label, value, emphasis }) {
  return (
    <div className={`stat ${emphasis ? 'stat-lead' : ''}`}>
      <span className="stat-value">{value}</span>
      <span className="stat-label">{label}</span>
    </div>
  );
}
