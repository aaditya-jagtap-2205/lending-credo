import { useCallback, useEffect, useState } from 'react';
import LoanForm from './components/LoanForm';
import DecisionCard from './components/DecisionCard';
import StatisticsPanel from './components/StatisticsPanel';
import HistoryTable from './components/HistoryTable';
import { getHistory, getStatistics, submitApplication } from './services/lendingApi';

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
    <div className="shell">
      <header className="masthead">
        <div>
          <p className="wordmark">Lending Platform</p>
          <p className="strapline">Secured lending decisions, scored against the current credit policy.</p>
        </div>
        <StatisticsPanel statistics={statistics} />
      </header>

      {error && (
        <p className="banner" role="alert">
          {error}
        </p>
      )}

      <main className="workspace">
        <LoanForm onSubmit={handleSubmit} isSubmitting={isSubmitting} />
        <DecisionCard result={result} />
      </main>

      <HistoryTable applications={applications} />

      <footer className="footnote">
        Lent to date counts successful applications only. Mean LTV covers every application, including declined ones.
      </footer>
    </div>
  );
}
