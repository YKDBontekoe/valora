export const rowVariants = {
  hidden: { opacity: 0, x: -20, scale: 0.98 },
  visible: {
    opacity: 1,
    x: 0,
    scale: 1,
    transition: { duration: 0.5, ease: [0.22, 1, 0.36, 1] as const }
  },
  exit: { opacity: 0, x: 20, scale: 0.98, transition: { duration: 0.3 } }
} as const;

export const getStatusBadge = (status: string) => {
  const base = "px-5 py-2 rounded-2xl text-[11px] font-black uppercase tracking-widest border flex items-center gap-2.5 transition-all duration-500 group-hover:shadow-md group-hover:translate-y-[-2px]";
  switch (status) {
    case 'Completed': return `${base} bg-success-50 text-success-700 border-success-200 shadow-glow-success group-hover:bg-white`;
    case 'Failed': return `${base} bg-error-50 text-error-700 border-error-200 shadow-glow-error group-hover:bg-white`;
    case 'Processing': return `${base} bg-primary-50 text-primary-700 border-primary-200 shadow-glow-primary group-hover:bg-white`;
    default: return `${base} bg-brand-50 text-brand-700 border-brand-200`;
  }
};
