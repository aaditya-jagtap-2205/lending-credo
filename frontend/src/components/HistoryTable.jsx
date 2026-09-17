import { formatDate, formatMoney, formatPercent } from '../services/format';

export default function HistoryTable({ applications }) {
  return (
    <div className="panel history">
      {applications.length === 0 ? <p className="empty-copy">Submitted applications will appear here, newest first.</p> : (
        <div className="table-scroll"><table><thead><tr><th scope="col">Reference</th><th scope="col" className="numeric">Loan</th><th scope="col" className="numeric">Asset</th><th scope="col" className="numeric">Score</th><th scope="col" className="numeric">LTV</th><th scope="col">Decision</th><th scope="col">Submitted</th></tr></thead><tbody>
          {applications.map((application) => <tr key={application.id}><td className="ref">CR-{String(application.id).padStart(4, '0')}</td><td className="numeric">{formatMoney(application.loanAmount)}</td><td className="numeric">{formatMoney(application.assetValue)}</td><td className="numeric">{application.creditScore}</td><td className="numeric">{formatPercent(application.ltv)}</td><td><span className={`pill ${application.decision === 'Successful' ? 'pill-yes' : 'pill-no'}`}><span />{application.decision}</span></td><td className="muted">{formatDate(application.createdAt)}</td></tr>)}
        </tbody></table></div>
      )}
    </div>
  );
}
