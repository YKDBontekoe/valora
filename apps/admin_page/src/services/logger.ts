// Placeholder for a centralized logging/telemetry service
// In a real application, this would forward errors to Sentry, Application Insights, etc.

export const logger = {
  error: (message: string, error?: unknown) => {
    // In development, log to console
    if (import.meta.env.DEV) {
      console.error(message, error);
    }
    // TODO: Send to secure telemetry service in production
  },
  info: (message: string, data?: unknown) => {
    if (import.meta.env.DEV) {
      console.info(message, data);
    }
  },
  warn: (message: string, data?: unknown) => {
    if (import.meta.env.DEV) {
      console.warn(message, data);
    }
  }
};
