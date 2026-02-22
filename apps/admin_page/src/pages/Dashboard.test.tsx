import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import Dashboard from './Dashboard';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getStats: vi.fn(),
  },
}));

describe('Dashboard Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders stats when API call succeeds', async () => {
    const mockStats = {
      totalUsers: 1234,
      totalNotifications: 56,
      activeJobs: 2,
    };
    (adminService.getStats as Mock).mockResolvedValue(mockStats);

    render(<Dashboard />);

    expect(screen.getByText('Dashboard')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('1,234')).toBeInTheDocument();
      expect(screen.getByText('56')).toBeInTheDocument();
    });
  });

  it('renders error message when API call fails', async () => {
    (adminService.getStats as Mock).mockRejectedValue(new Error('API Error'));

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('Unable to Load Dashboard')).toBeInTheDocument();
      expect(screen.getByText('Failed to fetch dashboard statistics. Please try again.')).toBeInTheDocument();
    });
  });
});
