import React, { useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import type { BatchJob } from '../../types';
import { logger } from '../../services/logger';

import { listVariants } from './BatchJobTableUtils';
import { BatchJobTableHeader } from './BatchJobTableHeader';
import { BatchJobTableRow } from './BatchJobTableRow';
import { BatchJobTableSkeleton, BatchJobTableError, BatchJobTableEmpty } from './BatchJobTableStates';
import { BatchJobTablePagination } from './BatchJobTablePagination';

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
          <BatchJobTableHeader sortBy={sortBy} toggleSort={toggleSort} />

          <motion.tbody
            initial="hidden"
            animate="visible"
            variants={listVariants}
            className="divide-y divide-brand-100"
          >
            {loading && jobs.length === 0 ? (
              <BatchJobTableSkeleton />
            ) : error ? (
              <BatchJobTableError displayError={displayError} refresh={refresh} />
            ) : jobs.length === 0 ? (
              <BatchJobTableEmpty />
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
        safeTotalPages={safeTotalPages}
        loading={loading}
        prevPage={prevPage}
        nextPage={nextPage}
      />
    </div>
  );
};
