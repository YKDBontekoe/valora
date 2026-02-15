import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import Users from './Users';
import { adminService } from '../services/api';

// Mock the API service
vi.mock('../services/api', () => ({
  adminService: {
    getUsers: vi.fn(),
    deleteUser: vi.fn(),
  },
}));

// Mock toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('Users', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    localStorage.setItem('admin_userId', '1');
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders loading state initially', () => {
    vi.mocked(adminService.getUsers).mockImplementation(() => new Promise(() => {}));
    render(<Users />);
    // Loading state replaces the entire component content when users.length is 0
    expect(screen.queryByText('User Management')).not.toBeInTheDocument();
    // Check if table is not there
    expect(screen.queryByText('Email')).not.toBeInTheDocument();
    // We could check for a spinner if we added a test id or class logic, but absence of content is a valid check here.
  });

  it('renders users when loaded successfully', async () => {
    vi.mocked(adminService.getUsers).mockResolvedValue({
      items: [
        { id: '1', email: 'user1@example.com', roles: ['User'] },
        { id: '2', email: 'user2@example.com', roles: ['Admin'] },
      ],
      totalPages: 1,
      totalCount: 2,
    });

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('User Management')).toBeInTheDocument();
      expect(screen.getByText('user1@example.com')).toBeInTheDocument();
      expect(screen.getByText('user2@example.com')).toBeInTheDocument();
    });
  });

  it('renders error state on failure', async () => {
    vi.mocked(adminService.getUsers).mockRejectedValue(new Error('Failed to fetch'));

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load users')).toBeInTheDocument();
      expect(screen.getByText('Try Again')).toBeInTheDocument();
    });
  });

  it('retries fetching users when retry button is clicked', async () => {
    vi.mocked(adminService.getUsers)
      .mockRejectedValueOnce(new Error('Failed to fetch'))
      .mockResolvedValueOnce({
        items: [{ id: '1', email: 'user1@example.com', roles: ['User'] }],
        totalPages: 1,
        totalCount: 1,
      });

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load users')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Try Again'));

    await waitFor(() => {
      expect(screen.getByText('user1@example.com')).toBeInTheDocument();
    });

    expect(adminService.getUsers).toHaveBeenCalledTimes(2);
  });

  it('deletes user on confirmation', async () => {
    vi.mocked(adminService.getUsers).mockResolvedValue({
      items: [{ id: '2', email: 'user2@example.com', roles: ['User'] }],
      totalPages: 1,
      totalCount: 1,
    });
    vi.mocked(adminService.deleteUser).mockResolvedValue();
    window.confirm = vi.fn().mockReturnValue(true);

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('user2@example.com')).toBeInTheDocument();
    });

    // Find delete button
    const deleteButton = screen.getByTitle('Delete user');
    fireEvent.click(deleteButton);

    expect(window.confirm).toHaveBeenCalled();
    expect(adminService.deleteUser).toHaveBeenCalledWith('2');

    await waitFor(() => {
      expect(screen.queryByText('user2@example.com')).not.toBeInTheDocument();
    });
  });

  it('does not delete user if cancelled', async () => {
    vi.mocked(adminService.getUsers).mockResolvedValue({
      items: [{ id: '2', email: 'user2@example.com', roles: ['User'] }],
      totalPages: 1,
      totalCount: 1,
    });
    window.confirm = vi.fn().mockReturnValue(false);

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('user2@example.com')).toBeInTheDocument();
    });

    const deleteButton = screen.getByTitle('Delete user');
    fireEvent.click(deleteButton);

    expect(window.confirm).toHaveBeenCalled();
    expect(adminService.deleteUser).not.toHaveBeenCalled();
    expect(screen.getByText('user2@example.com')).toBeInTheDocument();
  });

  it('does not allow deleting self', async () => {
    vi.mocked(adminService.getUsers).mockResolvedValue({
      items: [{ id: '1', email: 'me@example.com', roles: ['Admin'] }],
      totalPages: 1,
      totalCount: 1,
    });

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('me@example.com')).toBeInTheDocument();
    });

    const deleteButton = screen.getByTitle('You cannot delete yourself');
    expect(deleteButton).toBeDisabled();

    // Even if we force click, the handler should check
    fireEvent.click(deleteButton);
    expect(window.confirm).not.toHaveBeenCalled();
    expect(adminService.deleteUser).not.toHaveBeenCalled();
  });
});
