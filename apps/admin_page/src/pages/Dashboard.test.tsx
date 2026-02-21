import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import Dashboard from './Dashboard';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getStats: vi.fn(),
    getSystemStatus: vi.fn(),
  },
}));

describe('Dashboard Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders stats and system status when API call succeeds', async () => {
    const mockStats = {
      totalUsers: 1234,
      totalNotifications: 56,
    };
    const mockStatus = {
        apiLatencyMs: 42,
        queueDepth: 0,
        workerHealth: "Idle",
        dbConnectivity: "Connected",
        lastIngestionRun: "2024-01-01T12:00:00Z"
    };

    (adminService.getStats as Mock).mockResolvedValue(mockStats);
    (adminService.getSystemStatus as Mock).mockResolvedValue(mockStatus);

    render(<Dashboard />);

    expect(screen.getByText('Dashboard')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('1,234')).toBeInTheDocument();
      expect(screen.getByText('56')).toBeInTheDocument();
      expect(screen.getByText('Connected')).toBeInTheDocument();
      expect(screen.getByText('42.0ms')).toBeInTheDocument();
      expect(screen.getAllByText('Idle').length).toBeGreaterThan(0);
    });
  });

  it('renders error message when API call fails', async () => {
    (adminService.getStats as Mock).mockRejectedValue(new Error('API Error'));
    (adminService.getSystemStatus as Mock).mockRejectedValue(new Error('API Error'));

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('Unable to Load Dashboard')).toBeInTheDocument();
      expect(screen.getByText('Failed to fetch dashboard data. Please try again.')).toBeInTheDocument();
    });
  });
});
