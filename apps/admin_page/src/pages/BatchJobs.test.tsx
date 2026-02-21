import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import BatchJobs from './BatchJobs';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getJobs: vi.fn(),
    getSystemStatus: vi.fn(),
  },
}));

describe('BatchJobs Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
    const mockStatus = {
        dbLatencyMs: 42,
        queueDepth: 0,
        workerHealth: "Idle",
        dbConnectivity: "Connected",
        lastIngestionRun: "2024-01-01T12:00:00Z"
    };

    (adminService.getJobs as Mock).mockResolvedValue(mockJobs);
    (adminService.getSystemStatus as Mock).mockResolvedValue(mockStatus);

    render(<BatchJobs />);

    expect(screen.getByText('Batch Jobs')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
      expect(screen.getByText('Completed')).toBeInTheDocument();
    });
  });

  it('renders error message when API call fails', async () => {
    (adminService.getJobs as Mock).mockRejectedValue(new Error('API Error'));
    (adminService.getSystemStatus as Mock).mockRejectedValue(new Error('API Error'));

    render(<BatchJobs />);

    await waitFor(() => {
      expect(screen.getByText('Unable to load job history.')).toBeInTheDocument();
    });
  });
});
