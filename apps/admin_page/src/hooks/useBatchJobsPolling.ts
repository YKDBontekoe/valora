import { useState, useEffect, useCallback, useRef } from 'react';
import { adminService } from '../services/api';
import type { BatchJob, SystemHealth } from '../types';

interface UseBatchJobsPollingOptions {
  page: number;
  pageSize: number;
  statusFilter: string;
  typeFilter: string;
  searchQuery: string;
  sortBy?: string;
}

interface UseBatchJobsPollingResult {
  jobs: BatchJob[];
  health: SystemHealth | null;
  loading: boolean;
  error: string | null;
  totalPages: number;
  refresh: () => void;
}

export const useBatchJobsPolling = (options: UseBatchJobsPollingOptions): UseBatchJobsPollingResult => {
  const [jobs, setJobs] = useState<BatchJob[]>([]);
  const [health, setHealth] = useState<SystemHealth | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [totalPages, setTotalPages] = useState(1);

  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const latestRequestIdRef = useRef(0);
  const isFetchingRef = useRef(false);
  const optionsRef = useRef(options);
  const jobsRef = useRef<BatchJob[]>([]);

  // Keep options ref up to date
  useEffect(() => {
    optionsRef.current = options;
  }, [options.page, options.pageSize, options.statusFilter, options.typeFilter, options.searchQuery, options.sortBy]);

  const fetchData = useCallback(async (requestId: number) => {
    isFetchingRef.current = true;
    try {
      const currentOptions = optionsRef.current;

      const [jobsData, healthData] = await Promise.all([
        adminService.getJobs(
          currentOptions.page,
          currentOptions.pageSize,
          currentOptions.statusFilter,
          currentOptions.typeFilter,
          currentOptions.searchQuery,
          currentOptions.sortBy
        ).catch(() => null),
        adminService.getHealth().catch(() => null)
      ]);

      // Check for stale response
      if (requestId !== latestRequestIdRef.current) {
        return;
      }

      if (jobsData) {
        setJobs(jobsData.items);
        jobsRef.current = jobsData.items; // Update ref for immediate access
        setTotalPages(jobsData.totalPages);
        setError(null);
      } else {
        setError('Unable to load job history.');
      }

      if (healthData) {
        setHealth(healthData);
      }
    } catch (err) {
      if (requestId === latestRequestIdRef.current) {
         setError('An unexpected error occurred.');
      }
    } finally {
      if (requestId === latestRequestIdRef.current) {
        setLoading(false);
        isFetchingRef.current = false;

        // Schedule next poll
        if (timeoutRef.current) clearTimeout(timeoutRef.current);

        if (!document.hidden) {
            const hasActiveJobs = jobsRef.current.some(job => job.status === 'Pending' || job.status === 'Processing');
            const interval = hasActiveJobs ? 2000 : 5000;
            timeoutRef.current = setTimeout(() => {
                fetchData(requestId);
            }, interval);
        }
      }
    }
  }, []);

  // Initial fetch and on options change
  useEffect(() => {
    const requestId = ++latestRequestIdRef.current;
    if (timeoutRef.current) clearTimeout(timeoutRef.current);

    setLoading(true);
    fetchData(requestId);

    return () => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
    };
  }, [options.page, options.pageSize, options.statusFilter, options.typeFilter, options.searchQuery, options.sortBy, fetchData]);

  // Visibility change handler
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (!document.hidden) {
        // Resume polling immediately if not fetching
        if (!isFetchingRef.current) {
             if (timeoutRef.current) clearTimeout(timeoutRef.current);
             fetchData(latestRequestIdRef.current);
        }
      } else {
        // Stop polling
        if (timeoutRef.current) {
          clearTimeout(timeoutRef.current);
        }
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [fetchData]);

  const refresh = useCallback(() => {
    const requestId = ++latestRequestIdRef.current;
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    // Do not set loading to true to keep UI stable during refresh, or set it if desired.
    // Keeping it false to avoid flash of skeleton/loading state.
    fetchData(requestId);
  }, [fetchData]);

  return { jobs, health, loading, error, totalPages, refresh };
};
