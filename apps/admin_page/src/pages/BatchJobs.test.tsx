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
    startJob: vi.fn(),
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

      fireEvent.click(screen.getByText('Restart Pipeline'));

      await waitFor(() => {
          expect(adminService.retryJob).toHaveBeenCalledWith('2');
      });
  });

  it('opens details modal when clicking on a job row', async () => {
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
    });

    render(<BatchJobs />);

    await waitFor(() => {
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
    });

    const row = screen.getByText('Amsterdam').closest('tr');
    fireEvent.click(row!);

    await waitFor(() => {
        expect(screen.getByText('Pipeline Diagnostics')).toBeInTheDocument();
    });
  });

  it('triggers full ingestion', async () => {
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 0 });
    (adminService.startJob as Mock).mockResolvedValue({});

    render(<BatchJobs />);

    fireEvent.click(screen.getByText('Provision All Cities'));

    expect(screen.getByText('Start Full Ingestion?')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Start Ingestion'));

    await waitFor(() => {
        expect(adminService.startJob).toHaveBeenCalledWith('AllCitiesIngestion', 'Netherlands');
    });
  });

  it('executes municipality sync', async () => {
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 0 });
    (adminService.startJob as Mock).mockResolvedValue({});

    render(<BatchJobs />);

    const input = screen.getByPlaceholderText(/target municipality/i);
    fireEvent.change(input, { target: { value: 'Utrecht' } });

    const syncButton = screen.getByText('Execute Sync');
    fireEvent.click(syncButton);

    await waitFor(() => {
        expect(adminService.startJob).toHaveBeenCalledWith('CityIngestion', 'Utrecht');
    });
  });

  it('opens dataset catalog modal', async () => {
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 0 });
    render(<BatchJobs />);

    const catalogButton = screen.getByRole('button', { name: /dataset catalog/i });
    fireEvent.click(catalogButton);

    expect(screen.getByRole('heading', { name: /dataset catalog/i })).toBeInTheDocument();
  });

  it('handles search and filtering', async () => {
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 0 });

    render(<BatchJobs />);

    const searchInput = screen.getByPlaceholderText('Search by target...');
    fireEvent.change(searchInput, { target: { value: 'test-city' } });

    const statusSelect = screen.getByDisplayValue('All Statuses');
    fireEvent.change(statusSelect, { target: { value: 'Failed' } });

    const typeSelect = screen.getByDisplayValue('All Types');
    fireEvent.change(typeSelect, { target: { value: 'CityIngestion' } });

    await waitFor(() => {
        // useBatchJobsPolling calls getJobs with individual arguments, not an object
        expect(adminService.getJobs).toHaveBeenCalledWith(
            1,
            10,
            'Failed',
            'CityIngestion',
            'test-city',
            undefined
        );
    }, { timeout: 2000 }); // Account for debounce
  });

  it('handles sorting', async () => {
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 0 });

    render(<BatchJobs />);

    const typeHeader = screen.getByText('Definition');
    fireEvent.click(typeHeader);

    await waitFor(() => {
        expect(adminService.getJobs).toHaveBeenCalledWith(
            1,
            10,
            'All',
            'All',
            '',
            'type_asc'
        );
    });

    fireEvent.click(typeHeader);

    await waitFor(() => {
        expect(adminService.getJobs).toHaveBeenCalledWith(
            1,
            10,
            'All',
            'All',
            '',
            'type_desc'
        );
    });
  });

  it('handles keyboard sorting', async () => {
    (adminService.getJobs as Mock).mockResolvedValue({ items: [], totalPages: 0 });

    render(<BatchJobs />);

    const typeHeader = screen.getByRole('button', { name: /sort by definition/i });
    fireEvent.keyDown(typeHeader, { key: 'Enter' });

    await waitFor(() => {
        expect(adminService.getJobs).toHaveBeenCalledWith(
            1,
            10,
            'All',
            'All',
            '',
            'type_asc'
        );
    });
  });
});
