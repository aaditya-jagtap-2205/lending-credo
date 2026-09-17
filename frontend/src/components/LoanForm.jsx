import { useState } from 'react';
import { formatPercent } from '../services/format';

const EMPTY = { loanAmount: '', assetValue: '', creditScore: '' };

// Client-side checks are a courtesy so the user gets instant feedback.
// The backend re-validates everything and remains the authority.
function validate({ loanAmount, assetValue, creditScore }) {
  const errors = {};
  const loan = Number(loanAmount);
  const asset = Number(assetValue);
  const score = Number(creditScore);

  if (loanAmount === '' || Number.isNaN(loan)) errors.loanAmount = 'Enter the loan amount.';
  else if (loan <= 0) errors.loanAmount = 'The loan amount must be more than £0.';

  if (assetValue === '' || Number.isNaN(asset)) errors.assetValue = 'Enter the asset value.';
  else if (asset <= 0) errors.assetValue = 'The asset value must be more than £0, otherwise LTV cannot be calculated.';

  if (creditScore === '' || Number.isNaN(score)) errors.creditScore = 'Enter the credit score.';
  else if (!Number.isInteger(score) || score < 1 || score > 999)
    errors.creditScore = 'The credit score must be a whole number between 1 and 999.';

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

    onSubmit({
      loanAmount: Number(values.loanAmount),
      assetValue: Number(values.assetValue),
      creditScore: Number(values.creditScore),
    });
  };

  return (
    <form className="panel form" onSubmit={handleSubmit} noValidate>
      <h2>New application</h2>

      <Field
        id="loanAmount"
        label="Loan amount"
        prefix="£"
        value={values.loanAmount}
        onChange={update('loanAmount')}
        error={errors.loanAmount}
        placeholder="750000"
      />

      <Field
        id="assetValue"
        label="Value of the asset securing the loan"
        prefix="£"
        value={values.assetValue}
        onChange={update('assetValue')}
        error={errors.assetValue}
        placeholder="1000000"
      />

      <Field
        id="creditScore"
        label="Applicant credit score"
        hint="1 to 999"
        value={values.creditScore}
        onChange={update('creditScore')}
        error={errors.creditScore}
        placeholder="820"
      />

      <p className="preview" aria-live="polite">
        {previewLtv === null ? 'LTV appears once both amounts are entered.' : `Loan to value ${formatPercent(previewLtv)}`}
      </p>

      <div className="form-actions">
        <button type="submit" className="primary" disabled={isSubmitting}>
          {isSubmitting ? 'Checking…' : 'Get a decision'}
        </button>
        <button
          type="button"
          className="quiet"
          onClick={() => {
            setValues(EMPTY);
            setErrors({});
          }}
        >
          Clear
        </button>
      </div>
    </form>
  );
}

function Field({ id, label, hint, prefix, value, onChange, error, placeholder }) {
  return (
    <div className="field">
      <label htmlFor={id}>
        {label}
        {hint && <span className="hint"> {hint}</span>}
      </label>
      <div className={`input-shell ${error ? 'has-error' : ''}`}>
        {prefix && <span className="prefix">{prefix}</span>}
        <input
          id={id}
          name={id}
          type="number"
          inputMode="decimal"
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${id}-error` : undefined}
        />
      </div>
      {error && (
        <p className="error" id={`${id}-error`}>
          {error}
        </p>
      )}
    </div>
  );
}
