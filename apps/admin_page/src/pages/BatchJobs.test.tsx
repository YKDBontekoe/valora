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
    getHealth: vi.fn().mockResolvedValue({ status: 'Healthy' }),
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
    (adminService.getJobs as Mock).mockResolvedValue({ items: mockJobs, totalPages: 1 });

    render(<BatchJobs />);

    expect(screen.getByText('Batch Jobs')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
      expect(screen.getAllByText('Completed').length).toBeGreaterThan(0);
    });
  });

  it('renders error message when API call fails', async () => {
    (adminService.getJobs as Mock).mockRejectedValue(new Error('API Error'));

    render(<BatchJobs />);

    await waitFor(() => {
      // Updated expectation to match the new generic error message
      expect(screen.getByText('An error occurred while fetching jobs.')).toBeInTheDocument();
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
    (adminService.getJobs as Mock).mockResolvedValue({ items: mockJobs, totalPages: 1 });
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

    // Click Details button (was View)
    const viewButton = screen.getByText('Details');
    fireEvent.click(viewButton);

    await waitFor(() => {
        expect(screen.getByText('Pipeline Diagnostics')).toBeInTheDocument();
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
      (adminService.getJobs as Mock).mockResolvedValue({ items: mockJobs, totalPages: 1 });
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

      fireEvent.click(screen.getByText('Details'));

      await waitFor(() => {
          expect(screen.getByText('Restart Pipeline')).toBeInTheDocument();
      });

      // Click the first "Restart Pipeline" to open the confirmation modal
      const restartButtons = screen.getAllByText('Restart Pipeline');
      fireEvent.click(restartButtons[0]);

      // Wait for the modal and click its confirm button
      // The button text is "Restart Pipeline"
      await waitFor(() => {
          expect(screen.getByText('Are you sure you want to restart this ingestion pipeline? This will clear the current progress and attempt to re-run the entire process.')).toBeInTheDocument();
      });

      // 0 is original button (in modal), 1 is modal title (in confirm dialog), 2 is confirm button (in confirm dialog)
      const confirmButtons = screen.getAllByRole('button', { name: /Restart Pipeline/i });
      // The last button matching "Restart Pipeline" should be our confirmation button
      fireEvent.click(confirmButtons[confirmButtons.length - 1]);

      await waitFor(() => {
          expect(adminService.retryJob).toHaveBeenCalledWith('2');
      });
  });
});
