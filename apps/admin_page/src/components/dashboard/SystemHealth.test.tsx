import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach, afterEach, type Mock } from 'vitest';
import SystemHealth from './SystemHealth';
import { adminService } from '../../services/api';

// Mock adminService
vi.mock('../../services/api', () => ({
  adminService: {
    getHealth: vi.fn(),
  },
}));

describe('SystemHealth Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders loading skeleton initially', async () => {
    (adminService.getHealth as Mock).mockReturnValue(new Promise(() => {}));

    render(<SystemHealth />);

    expect(screen.getByText('Database Engine')).toBeInTheDocument();
    expect(screen.queryByText('Healthy & Connected')).not.toBeInTheDocument();
  });

  it('renders healthy state correctly', async () => {
    const mockHealth = {
      status: 'Healthy',
      database: true,
      apiLatencyP50: 20,
      apiLatencyP95: 50,
      apiLatencyP99: 100,
      apiLatency: 50,
      activeJobs: 5,
      queuedJobs: 2,
      failedJobs: 0,
      lastPipelineSuccess: '2023-01-01T12:00:00Z',
      timestamp: '2023-01-01T12:00:00Z'
    };

    (adminService.getHealth as Mock).mockResolvedValue(mockHealth);

    render(<SystemHealth />);

    await waitFor(() => {
      expect(screen.getByText('Healthy & Connected')).toBeInTheDocument();
      expect(screen.getByText('50ms')).toBeInTheDocument(); // P95
      expect(screen.getByText('5')).toBeInTheDocument(); // Active jobs
    });
  });

  it('renders unhealthy state correctly', async () => {
    const mockHealth = {
      status: 'Unhealthy',
      database: false,
      apiLatencyP50: 0,
      apiLatencyP95: 0,
      apiLatencyP99: 0,
      apiLatency: 0,
      activeJobs: 0,
      queuedJobs: 0,
      failedJobs: 0,
      lastPipelineSuccess: null,
      timestamp: '2023-01-01T12:00:00Z'
    };

    (adminService.getHealth as Mock).mockResolvedValue(mockHealth);

    render(<SystemHealth />);

    await waitFor(() => {
      expect(screen.getByText('Connection Failed')).toBeInTheDocument();
    });
  });

  it('shows stale data indicator on error after manual refresh', async () => {
    const mockHealth = {
      status: 'Healthy',
      database: true,
      apiLatencyP50: 20,
      apiLatencyP95: 50,
      apiLatencyP99: 100,
      apiLatency: 50,
      activeJobs: 5,
      queuedJobs: 2,
      failedJobs: 0,
      lastPipelineSuccess: '2023-01-01T12:00:00Z',
      timestamp: '2023-01-01T12:00:00Z'
    };

    // First call success
    (adminService.getHealth as Mock)
      .mockResolvedValueOnce(mockHealth);

    render(<SystemHealth />);

    // Wait for initial load
    await waitFor(() => {
      expect(screen.getByText('Healthy & Connected')).toBeInTheDocument();
      expect(screen.queryByText('Stale Data')).not.toBeInTheDocument();
    });

    // Mock next call failure
    (adminService.getHealth as Mock).mockRejectedValueOnce(new Error('Network error'));

    // Find refresh button (it has RefreshCw icon, maybe check for button role)
    const refreshButton = screen.getByRole('button');
    fireEvent.click(refreshButton);

    // Wait for stale indicator
    await waitFor(() => {
      expect(screen.getByText('Stale Data')).toBeInTheDocument();
      // Data should still be visible
      expect(screen.getByText('Healthy & Connected')).toBeInTheDocument();
    });
  });
});
