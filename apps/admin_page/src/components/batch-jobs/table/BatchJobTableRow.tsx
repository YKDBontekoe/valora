import React from 'react';
import { motion } from 'framer-motion';
import { AlertCircle, Activity, Info, ChevronRight, Clock, Database } from 'lucide-react';
import type { BatchJob } from '../../../types';

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

interface BatchJobTableRowProps {
  job: BatchJob;
  openDetails: (jobId: string) => void;
}

export const BatchJobTableRow: React.FC<BatchJobTableRowProps> = ({ job, openDetails }) => {
  const clampedProgress = Math.min(Math.max(job.progress || 0, 0), 100);

  return (
    <motion.tr
      variants={rowVariants}
      whileHover={{ x: 12, backgroundColor: 'var(--color-brand-50)', transition: { duration: 0.3 } }}
      className="group cursor-pointer relative transition-colors duration-500"
      onClick={() => openDetails(job.id)}
    >
      <td className="px-12 py-10 whitespace-nowrap">
        <div className="flex flex-col gap-2">
          <div className="flex items-center gap-3">
            <span className="text-base font-black text-brand-900 group-hover:text-primary-700 transition-colors tracking-tight">{job.type}</span>
            <div className="p-1.5 bg-brand-50 rounded-lg opacity-0 group-hover:opacity-100 transition-all duration-500">
              <Activity size={12} className="text-primary-500" />
            </div>
          </div>
          <span className="text-[10px] text-brand-300 font-black uppercase tracking-[0.2em]">ID: {job.id.slice(0, 12)}</span>
        </div>
      </td>
      <td className="px-12 py-10 whitespace-nowrap">
        <div className="flex items-center gap-3">
          <Database size={16} className="text-brand-200 group-hover:text-primary-400 transition-colors" />
          <span className="text-base font-black text-brand-600 group-hover:text-brand-900 transition-colors">{job.target}</span>
        </div>
      </td>
      <td className="px-12 py-10 whitespace-nowrap">
        <div className="flex items-center gap-4">
          <span className={getStatusBadge(job.status)}>{job.status}</span>
        </div>
      </td>
      <td className="px-12 py-10 whitespace-nowrap">
        <div className="flex flex-col gap-4">
          <div className="w-full bg-brand-50 rounded-full h-4 min-w-[200px] overflow-hidden relative border border-brand-100/50 shadow-inner">
            <motion.div
              className={`h-full rounded-full relative z-10 ${job.status === 'Failed' ? 'bg-error-500 shadow-glow-error' : 'bg-linear-to-r from-primary-500 to-primary-600 shadow-glow-primary'}`}
              initial={{ width: 0 }}
              animate={{ width: `${clampedProgress}%` }}
              transition={{ duration: 1.5, ease: [0.22, 1, 0.36, 1] }}
            >
              {job.status === 'Processing' && (
                <motion.div
                  className="absolute inset-0 bg-linear-to-r from-transparent via-white/50 to-transparent skew-x-[-20deg]"
                  animate={{ x: ['-200%', '300%'] }}
                  transition={{ duration: 2.5, repeat: Infinity, ease: "linear" }}
                />
              )}
            </motion.div>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-[10px] text-brand-400 font-black tracking-[0.25em]">{clampedProgress}% SYNCED</span>
            {job.status === 'Processing' && (
              <div className="flex gap-1">
                {[1, 2, 3].map(i => (
                  <motion.div
                    key={i}
                    animate={{ opacity: [0.2, 1, 0.2] }}
                    transition={{ duration: 1.5, repeat: Infinity, delay: i * 0.2 }}
                    className="w-1 h-1 rounded-full bg-primary-500"
                  />
                ))}
              </div>
            )}
          </div>
        </div>
      </td>
      <td className="px-12 py-10 whitespace-nowrap">
        <div className="flex items-center gap-4 text-brand-500 text-sm font-bold max-w-[280px] truncate group-hover:text-brand-800 transition-colors">
          <div className={`p-2 rounded-xl transition-colors ${job.error ? 'bg-error-50 text-error-500' : 'bg-brand-50 text-brand-300 group-hover:bg-primary-50 group-hover:text-primary-500'}`}>
            {job.error ? <AlertCircle size={16} /> : <Info size={16} />}
          </div>
          {job.error ? 'Cluster Fault Protocol Active' : (job.resultSummary || 'Sync in progression...')}
        </div>
      </td>
      <td className="px-12 py-10 whitespace-nowrap">
        <div className="flex flex-col gap-1.5">
          <div className="flex items-center gap-2">
            <Clock size={12} className="text-brand-200" />
            <span className="text-[11px] font-black text-brand-400 tracking-tight">
              {new Date(job.createdAt).toLocaleDateString()}
            </span>
          </div>
          <span className="text-[10px] font-bold text-brand-200 ml-5">
            {new Date(job.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </span>
        </div>
      </td>
      <td className="px-12 py-10 whitespace-nowrap text-right">
        <div className="flex items-center justify-end gap-6">
          <div className="opacity-0 group-hover:opacity-100 transition-all duration-700 translate-x-8 group-hover:translate-x-0 p-3 rounded-2xl bg-primary-50">
            <ChevronRight size={22} className="text-primary-500" />
          </div>
        </div>
      </td>
    </motion.tr>
  );
};
