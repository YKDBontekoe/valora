import * as Sentry from '@sentry/react';

// Centralized logging/telemetry service
// Forwards errors and logs to Sentry in production.

export const logger = {
  error: (message: string, error?: unknown) => {
    // In development, log to console
    if (import.meta.env.DEV) {
      console.error(message, error);
    }

    // Send to secure telemetry service in production
    if (import.meta.env.PROD) {
      if (error instanceof Error) {
        Sentry.captureException(error, { extra: { message } });
      } else if (error !== undefined) {
        Sentry.captureMessage(`${message}: ${JSON.stringify(error)}`, 'error');
      } else {
        Sentry.captureMessage(message, 'error');
      }
    }
  },
  info: (message: string, data?: unknown) => {
    if (import.meta.env.DEV) {
      console.info(message, data);
    }

    if (import.meta.env.PROD) {
      Sentry.captureMessage(message, {
        level: 'info',
        extra: data !== undefined ? { data } : undefined,
      });
    }
  },
  warn: (message: string, data?: unknown) => {
    if (import.meta.env.DEV) {
      console.warn(message, data);
    }

    if (import.meta.env.PROD) {
      Sentry.captureMessage(message, {
        level: 'warning',
        extra: data !== undefined ? { data } : undefined,
      });
    }
  }
};
