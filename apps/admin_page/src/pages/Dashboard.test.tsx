import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import Dashboard from './Dashboard';
import { adminService } from '../services/api';
import { showToast } from '../services/toast';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../services/api');
vi.mock('../services/toast');

// Mock useNavigate since QuickActions uses it
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

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

  it('handles retry failed jobs successfully', async () => {
    (adminService.getStats as Mock).mockResolvedValue({ totalUsers: 1, totalNotifications: 1 });
    (adminService.getJobs as Mock).mockResolvedValue({
      items: [
        { id: '1', status: 'Failed' },
        { id: '3', status: 'Failed' }
      ]
    });
    (adminService.retryJob as Mock).mockResolvedValue({});

    render(<MemoryRouter><Dashboard /></MemoryRouter>);

    // Wait for initial render
    await waitFor(() => expect(screen.getByText('System Health')).toBeInTheDocument());

    const retryButton = screen.getByText('Retry Failed Jobs');
    fireEvent.click(retryButton);

    await waitFor(() => {
      expect(adminService.getJobs).toHaveBeenCalled();
      expect(adminService.retryJob).toHaveBeenCalledTimes(2); // Should retry both failed jobs
      expect(adminService.retryJob).toHaveBeenCalledWith('1');
      expect(adminService.retryJob).toHaveBeenCalledWith('3');
      expect(showToast).toHaveBeenCalledWith('Retried 2 failed jobs.', 'success');
    });
  });

  it('handles partial retry failure', async () => {
    (adminService.getStats as Mock).mockResolvedValue({ totalUsers: 1, totalNotifications: 1 });
    (adminService.getJobs as Mock).mockResolvedValue({
      items: [
        { id: '1', status: 'Failed' },
        { id: '2', status: 'Failed' }
      ]
    });

    // Fail first, succeed second
    (adminService.retryJob as Mock)
      .mockRejectedValueOnce(new Error('Retry failed'))
      .mockResolvedValueOnce({});

    render(<MemoryRouter><Dashboard /></MemoryRouter>);
    await waitFor(() => expect(screen.getByText('System Health')).toBeInTheDocument());

    const retryButton = screen.getByText('Retry Failed Jobs');
    fireEvent.click(retryButton);

    await waitFor(() => {
      expect(adminService.retryJob).toHaveBeenCalledTimes(2);
      // Only 1 succeeded
      expect(showToast).toHaveBeenCalledWith('Retried 1 failed jobs.', 'success');
    });
  });

  it('shows no failed jobs toast', async () => {
    (adminService.getStats as Mock).mockResolvedValue({ totalUsers: 1, totalNotifications: 1 });
    (adminService.getJobs as Mock).mockResolvedValue({
      items: []
    });

    render(<MemoryRouter><Dashboard /></MemoryRouter>);
    await waitFor(() => expect(screen.getByText('System Health')).toBeInTheDocument());

    const retryButton = screen.getByText('Retry Failed Jobs');
    fireEvent.click(retryButton);

    await waitFor(() => {
      expect(adminService.retryJob).not.toHaveBeenCalled();
      expect(showToast).toHaveBeenCalledWith('No failed jobs found.', 'info');
    });
  });

  it('handles retry failure gracefully', async () => {
    (adminService.getStats as Mock).mockResolvedValue({ totalUsers: 1, totalNotifications: 1 });
    (adminService.getJobs as Mock).mockRejectedValue(new Error('API Error'));

    render(<MemoryRouter><Dashboard /></MemoryRouter>);
    await waitFor(() => expect(screen.getByText('System Health')).toBeInTheDocument());

    const retryButton = screen.getByText('Retry Failed Jobs');
    fireEvent.click(retryButton);

    await waitFor(() => {
      expect(showToast).toHaveBeenCalledWith('Failed to retry jobs.', 'error');
    });
  });

  it('navigates to users page on Manage Users click', async () => {
     (adminService.getStats as Mock).mockResolvedValue({ totalUsers: 1, totalNotifications: 1 });
     render(<MemoryRouter><Dashboard /></MemoryRouter>);
     await waitFor(() => expect(screen.getByText('System Health')).toBeInTheDocument());

     const usersButton = screen.getByText('Manage Users');
     fireEvent.click(usersButton);

     expect(mockNavigate).toHaveBeenCalledWith('/users');
  });
});
