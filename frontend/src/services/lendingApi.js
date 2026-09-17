const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5199';

async function request(path, options) {
  let response;
  try {
    response = await fetch(`${BASE_URL}/api${path}`, {
      headers: { 'Content-Type': 'application/json' },
      ...options,
    });
  } catch {
    throw new Error(`Can't reach the API at ${BASE_URL}. Start the backend and try again.`);
  }

  if (!response.ok) {
    throw new Error(await readError(response));
  }

  return response.status === 204 ? null : response.json();
}

// ASP.NET Core returns a ProblemDetails body for validation failures; surface the
// field messages so the user sees what the server objected to, not a status code.
async function readError(response) {
  try {
    const problem = await response.json();
    if (problem?.errors) {
      return Object.values(problem.errors).flat().join(' ');
    }
    return problem?.title ?? `Request failed with status ${response.status}.`;
  } catch {
    return `Request failed with status ${response.status}.`;
  }
}

export const submitApplication = (application) =>
  request('/loans', { method: 'POST', body: JSON.stringify(application) });

export const getHistory = () => request('/loans');

export const getStatistics = () => request('/loans/statistics');
