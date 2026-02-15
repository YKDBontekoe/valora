import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import Dashboard from './Dashboard';
import { adminService } from '../services/api';

// Mock the API service
vi.mock('../services/api', () => ({
  adminService: {
    getStats: vi.fn(),
  },
}));

describe('Dashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders loading state initially', () => {
    vi.mocked(adminService.getStats).mockImplementation(() => new Promise(() => {}));
    render(<Dashboard />);
    // Check for spinner or loading text
    // We used a div with animate-spin, so we can check for that if we query by class or assume it's there
    // Or we can check if it's NOT rendering the content
    expect(screen.queryByText('Total Users')).not.toBeInTheDocument();
  });

  it('renders stats when loaded successfully', async () => {
    vi.mocked(adminService.getStats).mockResolvedValue({
      totalUsers: 10,
      totalListings: 20,
      totalNotifications: 5,
    });

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('Total Users')).toBeInTheDocument();
      expect(screen.getByText('10')).toBeInTheDocument();
      expect(screen.getByText('Total Listings')).toBeInTheDocument();
      expect(screen.getByText('20')).toBeInTheDocument();
      expect(screen.getByText('Notifications')).toBeInTheDocument();
      expect(screen.getByText('5')).toBeInTheDocument();
    });
  });

  it('renders error state on failure', async () => {
    vi.mocked(adminService.getStats).mockRejectedValue(new Error('Failed to fetch'));

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load dashboard statistics')).toBeInTheDocument();
      expect(screen.getByText('Try Again')).toBeInTheDocument();
    });
  });

  it('retries fetching stats when retry button is clicked', async () => {
    vi.mocked(adminService.getStats)
      .mockRejectedValueOnce(new Error('Failed to fetch'))
      .mockResolvedValueOnce({
        totalUsers: 10,
        totalListings: 20,
        totalNotifications: 5,
      });

    render(<Dashboard />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load dashboard statistics')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Try Again'));

    await waitFor(() => {
      expect(screen.getByText('Total Users')).toBeInTheDocument();
      expect(screen.getByText('10')).toBeInTheDocument();
    });

    expect(adminService.getStats).toHaveBeenCalledTimes(2);
  });
});
