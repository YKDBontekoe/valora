import React, { useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import type { BatchJob } from '../../types';
import { logger } from '../../services/logger';
import { BatchJobTableHeaderCell } from './BatchJobTableHeaderCell';
import { BatchJobTableRow } from './BatchJobTableRow';
import { BatchJobTableLoadingState } from './BatchJobTableLoadingState';
import { BatchJobTableErrorState } from './BatchJobTableErrorState';
import { BatchJobTableEmptyState } from './BatchJobTableEmptyState';
import { BatchJobTablePagination } from './BatchJobTablePagination';

const listVariants = {
  visible: {
    transition: {
      staggerChildren: 0.05
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

  return (
    <div className="bg-white rounded-[2.5rem] overflow-hidden shadow-premium-xl border border-brand-100/50">
      <div className="overflow-x-auto custom-scrollbar">
        <table className="min-w-full divide-y divide-brand-100">
          <thead className="bg-brand-50/50 backdrop-blur-md">
            <tr>
              <BatchJobTableHeaderCell label="Definition" field="type" sortBy={sortBy} toggleSort={toggleSort} />
              <BatchJobTableHeaderCell label="Target" field="target" sortBy={sortBy} toggleSort={toggleSort} />
              <BatchJobTableHeaderCell label="Status" field="status" sortBy={sortBy} toggleSort={toggleSort} />
              <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Progress</th>
              <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Context</th>
              <BatchJobTableHeaderCell label="Timestamp" field="createdAt" sortBy={sortBy} toggleSort={toggleSort} />
              <th className="px-12 py-8 text-right text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Action</th>
            </tr>
          </thead>
          <motion.tbody
            initial="hidden"
            animate="visible"
            variants={listVariants}
            className="divide-y divide-brand-100"
          >
            {loading && jobs.length === 0 ? (
              <BatchJobTableLoadingState />
            ) : error ? (
              <BatchJobTableErrorState error={displayError || "Error"} refresh={refresh} />
            ) : jobs.length === 0 ? (
              <BatchJobTableEmptyState />
            ) : (
              <AnimatePresence mode="popLayout">
                {jobs.map((job) => (
                  <BatchJobTableRow key={job.id} job={job} openDetails={openDetails} />
                ))}
              </AnimatePresence>
            )}
          </motion.tbody>
        </table>
      </div>

      <BatchJobTablePagination
        page={page}
        totalPages={safeTotalPages}
        loading={loading}
        prevPage={prevPage}
        nextPage={nextPage}
      />
    </div>
  );
};
