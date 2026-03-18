import React, { useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ArrowUp, ArrowDown, AlertCircle, Activity, Info, ChevronLeft, ChevronRight, Clock, Database, Sparkles } from 'lucide-react';
import Button from '../Button';
import Skeleton from '../Skeleton';
import type { BatchJob } from '../../types';
import { logger } from '../../services/logger';

const listVariants = {
  visible: {
    transition: {
      staggerChildren: 0.03
    }
  }
};

const rowVariants = {
  hidden: { opacity: 0, x: -20, scale: 0.98 },
  visible: {
    opacity: 1,
    x: 0,
    scale: 1,
    transition: { duration: 0.5, ease: [0.22, 1, 0.36, 1] as const }
  },
  exit: { opacity: 0, x: 20, scale: 0.98, transition: { duration: 0.3 } }
} as const;

const getStatusBadge = (status: string) => {
  const base = "px-5 py-2 rounded-2xl text-[11px] font-black uppercase tracking-widest border flex items-center gap-2.5 transition-all duration-500 group-hover:shadow-md group-hover:translate-y-[-2px]";
  switch (status) {
    case 'Completed': return `${base} bg-success-50 text-success-700 border-success-200 shadow-glow-success group-hover:bg-white`;
    case 'Failed': return `${base} bg-error-50 text-error-700 border-error-200 shadow-glow-error group-hover:bg-white`;
    case 'Processing': return `${base} bg-primary-50 text-primary-700 border-primary-200 shadow-glow-primary group-hover:bg-white`;
    default: return `${base} bg-brand-50 text-brand-700 border-brand-200`;
  }
};

interface BatchJobTableProps {
  jobs: BatchJob[];
  loading: boolean;
  error: string | null;
  sortBy: string | undefined;
  toggleSort: (field: string) => void;
  refresh: () => void;
  openDetails: (jobId: string) => void;
  page: number;
  totalPages: number;
  prevPage: () => void;
  nextPage: () => void;
}

export const BatchJobTable: React.FC<BatchJobTableProps> = ({
  jobs,
  loading,
  error,
  sortBy,
  toggleSort,
  refresh,
  openDetails,
  page,
  totalPages,
  prevPage,
  nextPage,
}) => {
  useEffect(() => {
    if (error) {
      logger.error("BatchJobTable Error:", error);
    }
  }, [error]);

  const displayError = error ? "An error occurred while fetching jobs." : null;
  const safeTotalPages = Math.max(1, totalPages);

  const getAriaSort = (field: string) => {
    if (sortBy === `${field}_asc`) return 'ascending';
    if (sortBy === `${field}_desc`) return 'descending';
    return 'none';
  };

  const handleKeyDown = (e: React.KeyboardEvent, field: string) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleSort(field);
    }
  };

  return (
    <div className="bg-white rounded-[2.5rem] overflow-hidden shadow-premium-xl border border-brand-100/50">
      <div className="overflow-x-auto custom-scrollbar">
        <table className="min-w-full divide-y divide-brand-100">
          <thead className="bg-brand-50/50 backdrop-blur-md">
            <tr>
              <th
                className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
                onClick={() => toggleSort('type')}
                onKeyDown={(e) => handleKeyDown(e, 'type')}
                tabIndex={0}
                role="button"
                aria-sort={getAriaSort('type')}
                aria-label="Sort by Definition"
              >
                <div className="flex items-center gap-3">
                  Definition
                  <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                    <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'type_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
                    <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'type_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
                  </div>
                </div>
              </th>
              <th
                className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
                onClick={() => toggleSort('target')}
                onKeyDown={(e) => handleKeyDown(e, 'target')}
                tabIndex={0}
                role="button"
                aria-sort={getAriaSort('target')}
                aria-label="Sort by Target"
              >
                <div className="flex items-center gap-3">
                  Target
                  <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                    <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'target_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
                    <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'target_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
                  </div>
                </div>
              </th>
              <th
                className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
                onClick={() => toggleSort('status')}
                onKeyDown={(e) => handleKeyDown(e, 'status')}
                tabIndex={0}
                role="button"
                aria-sort={getAriaSort('status')}
                aria-label="Sort by Status"
              >
                <div className="flex items-center gap-3">
                  Status
                  <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                    <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'status_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
                    <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'status_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
                  </div>
                </div>
              </th>
              <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide border-b border-brand-100">Progress</th>
              <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide border-b border-brand-100">Context</th>
              <th
                className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10 border-b border-brand-100"
                onClick={() => toggleSort('createdAt')}
                onKeyDown={(e) => handleKeyDown(e, 'createdAt')}
                tabIndex={0}
                role="button"
                aria-sort={getAriaSort('createdAt')}
                aria-label="Sort by Timestamp"
              >
                <div className="flex items-center gap-3">
                  Timestamp
                  <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                    <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'createdAt_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
                    <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'createdAt_desc' || (!sortBy) ? 'text-primary-600' : 'text-brand-200'}`} />
                  </div>
                </div>
              </th>
              <th className="px-12 py-8 text-right text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide border-b border-brand-100">Action</th>
            </tr>
          </thead>
          <motion.tbody
            initial="hidden"
            animate="visible"
            variants={listVariants}
            className="divide-y divide-brand-100"
          >
            {loading && jobs.length === 0 ? (
              [...Array(5)].map((_, i) => (
                <tr key={i}>
                  <td className="px-12 py-10"><Skeleton variant="text" width="60%" height={24} /></td>
                  <td className="px-12 py-10"><Skeleton variant="text" width="40%" height={20} /></td>
                  <td className="px-12 py-10"><Skeleton variant="rectangular" width={100} height={32} className="rounded-2xl" /></td>
                  <td className="px-12 py-10"><Skeleton variant="rectangular" width="100%" height={12} className="rounded-full" /></td>
                  <td className="px-12 py-10"><Skeleton variant="text" width="70%" height={20} /></td>
                  <td className="px-12 py-10"><Skeleton variant="text" width="80%" height={16} /></td>
                  <td className="px-12 py-10"></td>
                </tr>
              ))
            ) : error ? (
              <tr>
                <td colSpan={7} className="px-12 py-40 text-center">
                  <div className="flex flex-col items-center gap-10 text-error-500">
                    <div className="p-12 bg-error-50 rounded-[2.5rem] border border-error-100 shadow-glow-error">
                        <AlertCircle size={80} className="opacity-40" />
                    </div>
                    <div className="flex flex-col gap-3">
                        <span className="font-black text-4xl tracking-tightest uppercase tracking-widest">Sync Failure</span>
                        <p className="text-error-600 font-bold text-lg">{displayError}</p>
                    </div>
                    <Button onClick={refresh} variant="outline" size="lg" className="mt-4 border-error-200 text-error-700 bg-white shadow-sm hover:shadow-glow-error">Retry Pipeline Sync</Button>
                  </div>
                </td>
              </tr>
            ) : jobs.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-12 py-60 text-center">
                  <div className="flex flex-col items-center gap-10 text-brand-100">
                    <div className="p-14 bg-brand-50 rounded-[3rem] border border-brand-100 shadow-inner relative overflow-hidden group/empty">
                        <Activity size={120} className="opacity-10 group-hover/empty:scale-110 group-hover/empty:rotate-12 transition-transform duration-700" />
                        <motion.div
                            className="absolute inset-0 bg-linear-to-br from-primary-500/5 to-transparent opacity-0 group-hover/empty:opacity-100 transition-opacity duration-700"
                        />
                    </div>
                    <div className="flex flex-col gap-3">
                        <span className="font-black text-3xl uppercase tracking-[0.3em] text-brand-200">Idle Pipeline</span>
                        <p className="text-brand-300 font-bold">No active batches detected in the current cluster.</p>
                    </div>
                  </div>
                </td>
              </tr>
            ) : (
              <AnimatePresence mode="popLayout">
                {jobs.map((job) => {
                  const clampedProgress = Math.min(Math.max(job.progress || 0, 0), 100);
                  return (
                    <motion.tr
                      key={job.id}
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
                            <span className="text-[10px] text-brand-400 font-black tracking-ultra-wide">{clampedProgress}% SYNCED</span>
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
                })}
              </AnimatePresence>
            )}
          </motion.tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="px-12 py-10 border-t border-brand-100 bg-brand-50/20 flex items-center justify-between backdrop-blur-md">
        <div className="flex items-center gap-4">
            <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center border border-brand-100 shadow-sm">
                <Sparkles size={18} className="text-primary-500" />
            </div>
            <div className="text-[12px] font-black text-brand-400 uppercase tracking-ultra-wide">
                Registry Page <span className="text-brand-900">{page}</span> <span className="mx-4 text-brand-200">/</span> <span className="text-brand-900">{safeTotalPages}</span>
            </div>
        </div>
        <div className="flex gap-6">
          <Button
            variant="outline"
            size="md"
            onClick={prevPage}
            disabled={page <= 1 || loading}
            leftIcon={<ChevronLeft size={20} />}
            className="font-black bg-white shadow-sm hover:shadow-md px-8"
          >
            Prev
          </Button>
          <Button
            variant="outline"
            size="md"
            onClick={nextPage}
            disabled={page >= safeTotalPages || loading}
            rightIcon={<ChevronRight size={20} />}
            className="font-black bg-white shadow-sm hover:shadow-md px-8"
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  );
};
