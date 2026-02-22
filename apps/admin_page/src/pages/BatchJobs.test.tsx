import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import BatchJobs from './BatchJobs';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getJobs: vi.fn(),
    getJobDetails: vi.fn(),
    retryJob: vi.fn(),
    cancelJob: vi.fn(),
  },
}));

describe('BatchJobs Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.restoreAllMocks();
  });

  it('renders job list when API call succeeds', async () => {
    const mockJobs = [
      {
        id: '1',
        type: 'CityIngestion',
        status: 'Completed',
        target: 'Amsterdam',
        progress: 100,
        createdAt: new Date().toISOString(),
      },
    ];
    (adminService.getJobs as Mock).mockResolvedValue(mockJobs);

    render(<BatchJobs />);

    expect(screen.getByText('Batch Jobs')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
      expect(screen.getByText('Completed')).toBeInTheDocument();
    });
  });

  it('renders error message when API call fails', async () => {
    (adminService.getJobs as Mock).mockRejectedValue(new Error('API Error'));

    render(<BatchJobs />);

    await waitFor(() => {
      expect(screen.getByText('Unable to load job history.')).toBeInTheDocument();
    });
  });

  it('opens details modal and loads details', async () => {
    const mockJobs = [
      {
        id: '1',
        type: 'CityIngestion',
        status: 'Completed',
        target: 'Amsterdam',
        progress: 100,
        createdAt: new Date().toISOString(),
      },
    ];
    (adminService.getJobs as Mock).mockResolvedValue(mockJobs);
    (adminService.getJobDetails as Mock).mockResolvedValue({
        ...mockJobs[0],
        executionLog: 'Logs...',
        startedAt: new Date().toISOString(),
        completedAt: new Date().toISOString(),
    });

    render(<BatchJobs />);

    await waitFor(() => {
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
    });

    // Click View button
    const viewButton = screen.getByText('View');
    fireEvent.click(viewButton);

    await waitFor(() => {
        expect(screen.getByText('Job Details')).toBeInTheDocument();
        expect(screen.getByText('Logs...')).toBeInTheDocument();
    });

    expect(adminService.getJobDetails).toHaveBeenCalledWith('1');
  });

  it('retries a failed job', async () => {
      // Mock confirm
      vi.spyOn(window, 'confirm').mockImplementation(() => true);

      const mockJobs = [
          {
              id: '2',
              type: 'CityIngestion',
              status: 'Failed',
              target: 'Rotterdam',
              progress: 50,
              createdAt: new Date().toISOString(),
              error: 'Error',
          },
      ];
      (adminService.getJobs as Mock).mockResolvedValue(mockJobs);
      (adminService.getJobDetails as Mock).mockResolvedValue({
          ...mockJobs[0],
          executionLog: 'Failed logs',
      });
      (adminService.retryJob as Mock).mockResolvedValue({
           ...mockJobs[0],
           status: 'Pending',
           error: null,
      });

      render(<BatchJobs />);

      await waitFor(() => {
          expect(screen.getByText('Rotterdam')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('View'));

      await waitFor(() => {
          expect(screen.getByText('Retry Job')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('Retry Job'));

      await waitFor(() => {
          expect(adminService.retryJob).toHaveBeenCalledWith('2');
      });
  });
});
