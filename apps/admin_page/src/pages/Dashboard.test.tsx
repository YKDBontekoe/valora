import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, Mock } from 'vitest';
import Dashboard from './Dashboard';
import { adminService } from '../services/api';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../services/api');
vi.mock('../services/toast');

describe('Dashboard Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', () => {
    (adminService.getStats as Mock).mockImplementation(() => new Promise(() => {}));
    render(<MemoryRouter><Dashboard /></MemoryRouter>);
    expect(screen.queryByText('Total Users')).not.toBeInTheDocument();
  });

  it('renders stats after loading', async () => {
    (adminService.getStats as Mock).mockResolvedValue({
      totalUsers: 123,
      totalNotifications: 45,
    });

    render(<MemoryRouter><Dashboard /></MemoryRouter>);

    await waitFor(() => {
      expect(screen.getByText('Total Users')).toBeInTheDocument();
      expect(screen.getByText('123')).toBeInTheDocument();
      expect(screen.getByText('Notifications')).toBeInTheDocument();
      expect(screen.getByText('45')).toBeInTheDocument();
    });
  });

  it('handles error state', async () => {
    (adminService.getStats as Mock).mockRejectedValue(new Error('Failed'));
    render(<MemoryRouter><Dashboard /></MemoryRouter>);

    await waitFor(() => {
      expect(screen.getByText('Failed to fetch dashboard statistics. Please try again.')).toBeInTheDocument();
    });
  });
});
