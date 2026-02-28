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

  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const latestRequestIdRef = useRef(0);
  const isFetchingRef = useRef(false);
  const optionsRef = useRef(options);
  const jobsRef = useRef<BatchJob[]>([]);

  // Keep options ref up to date
  useEffect(() => {
    optionsRef.current = options;
  }, [options]);

  const fetchData = useCallback(async (requestId: number) => {
    isFetchingRef.current = true;
    try {
      const currentOptions = optionsRef.current;

      const results = await Promise.allSettled([
        adminService.getJobs(
          currentOptions.page,
          currentOptions.pageSize,
          currentOptions.statusFilter,
          currentOptions.typeFilter,
          currentOptions.searchQuery,
          currentOptions.sortBy
        ),
        adminService.getHealth()
      ]);

      // Check for stale response
      if (requestId !== latestRequestIdRef.current) {
        return;
      }

      const [jobsResult, healthResult] = results;

      if (jobsResult.status === 'fulfilled') {
        const jobsData = jobsResult.value;
        setJobs(jobsData.items);
        jobsRef.current = jobsData.items; // Update ref for immediate access
        setTotalPages(jobsData.totalPages);
        setError(null);
      } else {
        console.error('Failed to fetch jobs:', jobsResult.reason);
        setError('Unable to load job history.');
      }

      if (healthResult.status === 'fulfilled') {
        setHealth(healthResult.value);
      } else {
        // Silent failure for health check is acceptable as per original design
        console.warn('Failed to fetch system health:', healthResult.reason);
      }
    } catch (err) {
        // This catch block might not be reached due to allSettled, but good for safety
      if (requestId === latestRequestIdRef.current) {
         console.error('Unexpected polling error:', err);
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
    fetchData(requestId);
  }, [fetchData]);

  return { jobs, health, loading, error, totalPages, refresh };
};
