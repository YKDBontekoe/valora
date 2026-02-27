import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ArrowUp, ArrowDown, AlertCircle, Activity, Info, ChevronLeft, ChevronRight } from 'lucide-react';
import Button from '../Button';
import Skeleton from '../Skeleton';
import type { BatchJob } from '../../types';

const listVariants = {
  visible: {
    transition: {
      staggerChildren: 0.05
    }
  }
};

const rowVariants = {
  hidden: { opacity: 0, x: -10 },
  visible: {
    opacity: 1,
    x: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] as const }
  },
  exit: { opacity: 0, x: 10, transition: { duration: 0.2 } }
} as const;

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
  const getStatusBadge = (status: string) => {
    const base = "px-3 py-1 rounded-full text-[10px] font-black uppercase tracking-wider border flex items-center gap-1.5";
    switch (status) {
      case 'Completed': return `${base} bg-success-50 text-success-700 border-success-200 shadow-sm shadow-success-100/50`;
      case 'Failed': return `${base} bg-error-50 text-error-700 border-error-200 shadow-sm shadow-error-100/50`;
      case 'Processing': return `${base} bg-primary-50 text-primary-700 border-primary-100 shadow-sm shadow-primary-100/50`;
      default: return `${base} bg-brand-50 text-brand-700 border-brand-200`;
    }
  };

  return (
    <>
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-brand-100">
          <thead>
            <tr className="bg-brand-50/10">
              <th
                className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/30 transition-colors select-none"
                onClick={() => toggleSort('type')}
              >
                <div className="flex items-center gap-2">
                  Definition
                  <div className="flex flex-col">
                    <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'type_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                    <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'type_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                  </div>
                </div>
              </th>
              <th
                className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/30 transition-colors select-none"
                onClick={() => toggleSort('target')}
              >
                <div className="flex items-center gap-2">
                  Target
                  <div className="flex flex-col">
                    <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'target_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                    <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'target_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                  </div>
                </div>
              </th>
              <th
                className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/30 transition-colors select-none"
                onClick={() => toggleSort('status')}
              >
                <div className="flex items-center gap-2">
                  Status
                  <div className="flex flex-col">
                    <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'status_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                    <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'status_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                  </div>
                </div>
              </th>
              <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Progress</th>
              <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Context</th>
              <th
                className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/30 transition-colors select-none"
                onClick={() => toggleSort('createdAt')}
              >
                <div className="flex items-center gap-2">
                  Timestamp
                  <div className="flex flex-col">
                    <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'createdAt_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                    <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'createdAt_desc' || (!sortBy) ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                  </div>
                </div>
              </th>
              <th className="px-10 py-5 text-right text-[10px] font-black text-brand-400 uppercase tracking-widest">Action</th>
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
                  <td className="px-10 py-6"><Skeleton variant="text" width="40%" /></td>
                  <td className="px-10 py-6"><Skeleton variant="text" width="60%" /></td>
                  <td className="px-10 py-6"><Skeleton variant="rectangular" width={80} height={24} className="rounded-lg" /></td>
                  <td className="px-10 py-6"><Skeleton variant="rectangular" width="100%" height={8} className="rounded-full" /></td>
                  <td className="px-10 py-6"><Skeleton variant="text" width="50%" /></td>
                  <td className="px-10 py-6"><Skeleton variant="text" width="80%" /></td>
                  <td className="px-10 py-6"></td>
                </tr>
              ))
            ) : error ? (
              <tr>
                <td colSpan={7} className="px-10 py-20 text-center">
                  <div className="flex flex-col items-center gap-6 text-error-500">
                    <AlertCircle size={48} className="opacity-20" />
                    <span className="font-black text-xl">{error}</span>
                    <Button onClick={refresh} variant="outline" size="sm" className="mt-4 border-error-200 text-error-700">Retry Pipeline Sync</Button>
                  </div>
                </td>
              </tr>
            ) : jobs.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-10 py-24 text-center">
                  <div className="flex flex-col items-center gap-4 text-brand-200">
                    <Activity size={64} className="opacity-10 mb-2" />
                    <span className="font-black text-xl uppercase tracking-widest">Empty Pipeline History</span>
                  </div>
                </td>
              </tr>
            ) : (
              <AnimatePresence mode="popLayout">
                {jobs.map((job) => (
                  <motion.tr
                    key={job.id}
                    variants={rowVariants}
                    whileHover={{ scale: 1.005, backgroundColor: 'var(--color-brand-50)', transition: { duration: 0.2 } }}
                    className="group cursor-pointer relative"
                    onClick={() => openDetails(job.id)}
                  >
                    <td className="px-10 py-6 whitespace-nowrap">
                      <div className="flex flex-col">
                        <span className="text-sm font-black text-brand-900 group-hover:text-primary-700 transition-colors">{job.type}</span>
                        <span className="text-[10px] text-brand-300 font-black uppercase tracking-tighter mt-0.5">ID: {job.id.slice(0, 8)}</span>
                      </div>
                    </td>
                    <td className="px-10 py-6 whitespace-nowrap text-sm font-black text-brand-600">{job.target}</td>
                    <td className="px-10 py-6 whitespace-nowrap">
                      <div className="flex items-center gap-3">
                        <span className={getStatusBadge(job.status)}>{job.status}</span>
                      </div>
                    </td>
                    <td className="px-10 py-6 whitespace-nowrap">
                      <div className="flex flex-col gap-2.5">
                        <div className="w-full bg-brand-50 rounded-full h-2.5 min-w-[140px] overflow-hidden relative border border-brand-100/50">
                          <motion.div
                            className={`h-full rounded-full relative z-10 ${job.status === 'Failed' ? 'bg-error-500 shadow-[0_0_10px_rgba(239,68,68,0.4)]' : 'bg-linear-to-r from-primary-500 to-primary-600 shadow-[0_0_10px_rgba(124,58,237,0.3)]'}`}
                            initial={{ width: 0 }}
                            animate={{ width: `${job.progress}%` }}
                            transition={{ duration: 1.2, ease: "circOut" }}
                          >
                            {job.status === 'Processing' && (
                              <motion.div
                                className="absolute inset-0 bg-linear-to-r from-transparent via-white/40 to-transparent"
                                animate={{ x: ['-100%', '200%'] }}
                                transition={{ duration: 2, repeat: Infinity, ease: "linear" }}
                              />
                            )}
                          </motion.div>
                        </div>
                        <span className="text-[10px] text-brand-400 font-black tracking-widest">{job.progress}% COMPLETE</span>
                      </div>
                    </td>
                    <td className="px-10 py-6 whitespace-nowrap">
                      <div className="flex items-center gap-2 text-brand-500 text-sm font-bold max-w-[220px] truncate">
                        {(job.error || job.resultSummary) && <Info size={14} className="text-brand-200 flex-shrink-0" />}
                        {job.error ? 'Pipeline Fault (check logs)' : (job.resultSummary || 'No summary available')}
                      </div>
                    </td>
                    <td className="px-10 py-6 whitespace-nowrap text-[11px] font-black text-brand-400">
                      {new Date(job.createdAt).toLocaleString()}
                    </td>
                    <td className="px-10 py-6 whitespace-nowrap text-right">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-brand-300 hover:text-primary-600 hover:bg-primary-50"
                        onClick={(e) => { e.stopPropagation(); openDetails(job.id); }}
                      >
                        Details
                      </Button>
                    </td>
                  </motion.tr>
                ))}
              </AnimatePresence>
            )}
          </motion.tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="px-10 py-8 border-t border-brand-100 bg-brand-50/10 flex items-center justify-between">
        <div className="text-[10px] font-black text-brand-400 uppercase tracking-[0.25em]">
          Sequence <span className="text-brand-900">{page}</span> <span className="mx-3 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
        </div>
        <div className="flex gap-4">
          <Button
            variant="outline"
            size="sm"
            onClick={prevPage}
            disabled={page === 1 || loading}
            leftIcon={<ChevronLeft size={16} />}
            className="font-black"
          >
            Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={nextPage}
            disabled={page === totalPages || loading}
            rightIcon={<ChevronRight size={16} />}
            className="font-black"
          >
            Next
          </Button>
        </div>
      </div>
    </>
  );
};
