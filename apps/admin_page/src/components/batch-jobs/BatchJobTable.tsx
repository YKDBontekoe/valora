import React, { useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ArrowUp, ArrowDown, AlertCircle, Activity, ChevronLeft, ChevronRight, Sparkles } from 'lucide-react';
import Button from '../Button';
import Skeleton from '../Skeleton';
import type { BatchJob } from '../../types';
import { logger } from '../../services/logger';
import BatchJobRow from './BatchJobRow';

const listVariants = {
  visible: {
    transition: {
      staggerChildren: 0.03
    }
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

  return (
    <div className="bg-white rounded-[2.5rem] overflow-hidden shadow-premium-xl border border-brand-100/50">
      <div className="overflow-x-auto custom-scrollbar">
        <table className="min-w-full divide-y divide-brand-100">
          <thead className="bg-brand-50/50 backdrop-blur-md">
            <tr>
              <th
                className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none border-b border-brand-100"
                role="columnheader"
                aria-sort={getAriaSort('type')}
                aria-label="Sort by Definition"
              >
                <button
                  className="flex items-center gap-3 focus:outline-none w-full h-full text-left"
                  onClick={() => toggleSort('type')}
                >
                  Definition
                  <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                    <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'type_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
                    <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'type_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
                  </div>
                </button>
              </th>
              <th
                className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none border-b border-brand-100"
                role="columnheader"
                aria-sort={getAriaSort('target')}
                aria-label="Sort by Target"
              >
                <button
                  className="flex items-center gap-3 focus:outline-none w-full h-full text-left"
                  onClick={() => toggleSort('target')}
                >
                  Target
                  <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                    <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'target_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
                    <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'target_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
                  </div>
                </button>
              </th>
              <th
                className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none border-b border-brand-100"
                role="columnheader"
                aria-sort={getAriaSort('status')}
                aria-label="Sort by Status"
              >
                <button
                  className="flex items-center gap-3 focus:outline-none w-full h-full text-left"
                  onClick={() => toggleSort('status')}
                >
                  Status
                  <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                    <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'status_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
                    <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'status_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
                  </div>
                </button>
              </th>
              <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide border-b border-brand-100">Progress</th>
              <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide border-b border-brand-100">Context</th>
              <th
                className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-ultra-wide cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none border-b border-brand-100"
                role="columnheader"
                aria-sort={getAriaSort('createdAt')}
                aria-label="Sort by Timestamp"
              >
                <button
                  className="flex items-center gap-3 focus:outline-none w-full h-full text-left"
                  onClick={() => toggleSort('createdAt')}
                >
                  Timestamp
                  <div className="flex flex-col gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                    <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'createdAt_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
                    <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'createdAt_desc' || (!sortBy) ? 'text-primary-600' : 'text-brand-200'}`} />
                  </div>
                </button>
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
                {jobs.map((job) => (
                  <BatchJobRow key={job.id} job={job} openDetails={openDetails} />
                ))}
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
