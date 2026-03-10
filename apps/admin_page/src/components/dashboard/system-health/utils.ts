export const item = {
  hidden: { opacity: 0, y: 40, scale: 0.98 },
  show: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { duration: 0.7, ease: [0.22, 1, 0.36, 1] as const }
  }
} as const;

export const getStatusColor = (status: string) => {
  switch (status) {
    case 'Healthy': return 'text-success-500 bg-success-50 border-success-100 shadow-glow-success';
    case 'Degraded': return 'text-warning-500 bg-warning-50 border-warning-100 shadow-glow-warning';
    case 'Unhealthy': return 'text-error-500 bg-error-50 border-error-100 shadow-glow-error';
    default: return 'text-brand-400 bg-brand-50 border-brand-100';
  }
};

export const getLatencyStatus = (latency: number) => {
  if (latency < 100) return { label: 'Optimal', color: 'text-success-600 bg-success-50 border-success-100 ring-4 ring-success-500/10' };
  if (latency < 500) return { label: 'Fair', color: 'text-warning-600 bg-warning-50 border-warning-100 ring-4 ring-warning-500/10' };
  return { label: 'High', color: 'text-error-600 bg-error-50 border-error-100 ring-4 ring-error-500/10' };
};
