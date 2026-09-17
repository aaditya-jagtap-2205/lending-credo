import { useState } from 'react';
import { formatPercent } from '../services/format';

const EMPTY = { loanAmount: '', assetValue: '', creditScore: '' };

function validate({ loanAmount, assetValue, creditScore }) {
  const errors = {};
  const loan = Number(loanAmount);
  const asset = Number(assetValue);
  const score = Number(creditScore);

  if (loanAmount === '' || Number.isNaN(loan)) errors.loanAmount = 'Enter the loan amount.';
  else if (loan <= 0) errors.loanAmount = 'The loan amount must be more than £0.';
  if (assetValue === '' || Number.isNaN(asset)) errors.assetValue = 'Enter the asset value.';
  else if (asset <= 0) errors.assetValue = 'The asset value must be more than £0.';
  if (creditScore === '' || Number.isNaN(score)) errors.creditScore = 'Enter the credit score.';
  else if (!Number.isInteger(score) || score < 1 || score > 999) errors.creditScore = 'Use a whole number between 1 and 999.';
  return errors;
}

export default function LoanForm({ onSubmit, isSubmitting }) {
  const [values, setValues] = useState(EMPTY);
  const [errors, setErrors] = useState({});
  const loan = Number(values.loanAmount);
  const asset = Number(values.assetValue);
  const previewLtv = loan > 0 && asset > 0 ? (loan / asset) * 100 : null;

  const update = (field) => (event) => {
    setValues((current) => ({ ...current, [field]: event.target.value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    const found = validate(values);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    onSubmit({ loanAmount: Number(values.loanAmount), assetValue: Number(values.assetValue), creditScore: Number(values.creditScore) });
  };

  return (
    <form className="panel form" onSubmit={handleSubmit} noValidate>
      <div className="panel-heading">
        <div className="step-badge">01</div>
        <div><p className="eyebrow">Start here</p><h2>New application</h2></div>
      </div>
      <p className="form-intro">Tell us about the loan and the asset securing it. We’ll handle the policy checks.</p>

      <Field id="loanAmount" label="Loan amount" prefix="£" value={values.loanAmount} onChange={update('loanAmount')} error={errors.loanAmount} placeholder="750,000" />
      <Field id="assetValue" label="Secured asset value" prefix="£" value={values.assetValue} onChange={update('assetValue')} error={errors.assetValue} placeholder="1,000,000" />
      <Field id="creditScore" label="Applicant credit score" hint="1–999" value={values.creditScore} onChange={update('creditScore')} error={errors.creditScore} placeholder="820" />

      <div className={`preview ${previewLtv !== null ? 'has-value' : ''}`} aria-live="polite">
        <span className="preview-label">Indicative LTV</span>
        <strong>{previewLtv === null ? '—' : formatPercent(previewLtv)}</strong>
        <span>{previewLtv === null ? 'Enter both values to preview' : 'before the full policy assessment'}</span>
      </div>

      <div className="form-actions">
        <button type="submit" className="primary" disabled={isSubmitting}>{isSubmitting ? 'Assessing application…' : 'Assess application →'}</button>
        <button type="button" className="quiet" onClick={() => { setValues(EMPTY); setErrors({}); }}>Clear</button>
      </div>
    </form>
  );
}

function Field({ id, label, hint, prefix, value, onChange, error, placeholder }) {
  return (
    <div className="field">
      <label htmlFor={id}>{label} {hint && <span className="hint">· {hint}</span>}</label>
      <div className={`input-shell ${error ? 'has-error' : ''}`}>
        {prefix && <span className="prefix">{prefix}</span>}
        <input id={id} name={id} type="number" inputMode="decimal" value={value} onChange={onChange} placeholder={placeholder} aria-invalid={Boolean(error)} aria-describedby={error ? `${id}-error` : undefined} />
      </div>
      {error && <p className="error" id={`${id}-error`}>{error}</p>}
    </div>
  );
}
