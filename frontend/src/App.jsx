import { useCallback, useEffect, useState } from 'react';
import LoanForm from './components/LoanForm';
import DecisionCard from './components/DecisionCard';
import StatisticsPanel from './components/StatisticsPanel';
import HistoryTable from './components/HistoryTable';
import { getHistory, getStatistics, submitApplication } from './services/lendingApi';

function BrandMark() {
  return (
    <span className="brand-mark" aria-hidden="true">
      <span />
      <span />
      <span />
    </span>
  );
}

export default function App() {
  const [result, setResult] = useState(null);
  const [applications, setApplications] = useState([]);
  const [statistics, setStatistics] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  const refresh = useCallback(async () => {
    try {
      const [history, stats] = await Promise.all([getHistory(), getStatistics()]);
      setApplications(history);
      setStatistics(stats);
      setError(null);
    } catch (problem) {
      setError(problem.message);
    }
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const handleSubmit = async (application) => {
    setIsSubmitting(true);
    try {
      const decision = await submitApplication(application);
      setResult(decision);
      setError(null);
      await refresh();
    } catch (problem) {
      setError(problem.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="#top" aria-label="Credo home">
          <BrandMark />
          <span>credo</span>
        </a>
        <nav className="topnav" aria-label="Main navigation">
          <a className="active" href="#overview">Overview</a>
          <a href="#applications">Applications</a>
          <a href="#policy">Credit policy</a>
        </nav>
        <div className="topbar-status"><span className="status-dot" /> Decision engine live</div>
      </header>

      <main id="top" className="page">
        <section id="overview" className="hero">
          <div>
            <p className="eyebrow">Secured lending / workspace</p>
            <h1>Make confident<br /><em>credit decisions.</em></h1>
            <p className="hero-copy">Credo brings clarity to every secured loan application — from the first number entered to the final decision.</p>
          </div>
          <div className="hero-note">
            <span className="note-icon">↗</span>
            <p><strong>One clear view.</strong><br />Assess risk, understand the outcome, and keep your lending book moving.</p>
          </div>
        </section>

        <section className="metrics-row" aria-label="Portfolio overview">
          <StatisticsPanel statistics={statistics} />
        </section>

        {error && <p className="banner" role="alert"><strong>Something needs attention.</strong> {error}</p>}

        <section className="workspace" aria-label="Application workspace">
          <LoanForm onSubmit={handleSubmit} isSubmitting={isSubmitting} />
          <DecisionCard result={result} />
        </section>

        <section id="applications" className="history-section">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Portfolio activity</p>
              <h2>Recent applications</h2>
            </div>
            <span className="record-count">{applications.length} {applications.length === 1 ? 'record' : 'records'}</span>
          </div>
          <HistoryTable applications={applications} />
        </section>

        <section id="policy" className="policy-strip">
          <div className="policy-symbol">◎</div>
          <div><strong>Built around your credit policy.</strong><span>Loan size, LTV and credit score are assessed together — with every outcome explained.</span></div>
          <span className="policy-link">60 / 80 / 90 LTV bands</span>
        </section>

        <footer className="footnote">Lent to date counts successful applications only. Mean LTV covers every application, including declined ones.</footer>
      </main>
    </div>
  );
}
