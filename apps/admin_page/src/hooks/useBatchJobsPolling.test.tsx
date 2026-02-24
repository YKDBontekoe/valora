import { renderHook, waitFor, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach, type Mock } from 'vitest';
import { useBatchJobsPolling } from './useBatchJobsPolling';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getJobs: vi.fn(),
    getHealth: vi.fn(),
  },
}));

describe('useBatchJobsPolling', () => {
  const defaultOptions = {
    page: 1,
    pageSize: 10,
    statusFilter: 'All',
    typeFilter: 'All',
    searchQuery: '',
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (adminService.getHealth as Mock).mockResolvedValue({ status: 'Healthy' });
    vi.useRealTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('fetches data on mount', async () => {
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 1 });

    const { result } = renderHook(() => useBatchJobsPolling(defaultOptions));

    expect(result.current.loading).toBe(true);

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(adminService.getJobs).toHaveBeenCalledTimes(1);
  });

  it('polls periodically (idle)', async () => {
    vi.useFakeTimers();
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 1 });

    const { result } = renderHook(() => useBatchJobsPolling(defaultOptions));

    // Initial state
    expect(result.current.loading).toBe(true);

    // Flush microtasks to complete initial fetch
    await act(async () => {
        await Promise.resolve();
        await Promise.resolve();
    });

    expect(result.current.loading).toBe(false);

    // Idle interval is 5000ms
    await act(async () => {
      vi.advanceTimersByTime(5000);
    });

    expect(adminService.getJobs).toHaveBeenCalledTimes(2);
  });

  it('polls faster when active jobs exist', async () => {
    vi.useFakeTimers();
    const activeJobs = [{ id: '1', status: 'Processing' }];
    (adminService.getJobs as Mock).mockResolvedValue({ items: activeJobs, totalPages: 1 });

    const { result } = renderHook(() => useBatchJobsPolling(defaultOptions));

    expect(result.current.loading).toBe(true);

    await act(async () => {
        await Promise.resolve();
        await Promise.resolve();
    });

    expect(result.current.loading).toBe(false);

    // Active interval is 2000ms
    await act(async () => {
      vi.advanceTimersByTime(2000);
    });

    expect(adminService.getJobs).toHaveBeenCalledTimes(2);
  });

  it('stops polling when hidden and resumes when visible', async () => {
    vi.useFakeTimers();
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 1 });

    const { result } = renderHook(() => useBatchJobsPolling(defaultOptions));

    await act(async () => {
        await Promise.resolve();
        await Promise.resolve();
    });
    expect(result.current.loading).toBe(false);

    // Simulate hidden
    Object.defineProperty(document, 'hidden', { configurable: true, value: true });
    act(() => {
      document.dispatchEvent(new Event('visibilitychange'));
    });

    // Advance time - should NOT fetch
    await act(async () => {
      vi.advanceTimersByTime(10000);
    });

    expect(adminService.getJobs).toHaveBeenCalledTimes(1);

    // Simulate visible
    Object.defineProperty(document, 'hidden', { configurable: true, value: false });
    act(() => {
      document.dispatchEvent(new Event('visibilitychange'));
    });

    // Should fetch immediately
    // Flush promises as event handler calls fetchData async
    await act(async () => {
        await Promise.resolve();
        await Promise.resolve();
    });

    expect(adminService.getJobs).toHaveBeenCalledTimes(2);
  });

  it('ignores stale responses', async () => {
    vi.useRealTimers();

    let resolveFirst: (val: any) => void = () => {};
    const firstPromise = new Promise(resolve => { resolveFirst = resolve; });
    const secondPromise = Promise.resolve({ items: [{ id: 'new', status: 'Completed' }], totalPages: 1 });

    (adminService.getJobs as Mock)
        .mockReturnValueOnce(firstPromise)
        .mockReturnValueOnce(secondPromise);

    const { result, rerender } = renderHook((props) => useBatchJobsPolling(props), { initialProps: defaultOptions });

    expect(result.current.loading).toBe(true);

    // Trigger second fetch
    rerender({ ...defaultOptions, page: 2 });

    // Wait for second fetch to complete
    await waitFor(() => {
        expect(result.current.jobs).toEqual([{ id: 'new', status: 'Completed' }]);
    });

    // Now resolve the first fetch
    await act(async () => {
        resolveFirst({ items: [{ id: 'old', status: 'Completed' }], totalPages: 1 });
    });

    // Should still be 'new'
    expect(result.current.jobs).toEqual([{ id: 'new', status: 'Completed' }]);
  });
});
