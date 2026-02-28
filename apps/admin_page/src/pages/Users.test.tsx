import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import Users from './Users';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getUsers: vi.fn(),
    deleteUser: vi.fn(),
  },
}));

describe('Users Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders users list when API call succeeds', async () => {
    const mockUsers = {
      items: [
        { id: '1', email: 'user@example.com', roles: ['Admin'] },
        { id: '2', email: 'guest@example.com', roles: ['User'] },
      ],
      totalPages: 1,
    };
    (adminService.getUsers as Mock).mockResolvedValue(mockUsers);

    render(<Users />);

    expect(screen.getByText('Users')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('user@example.com')).toBeInTheDocument();
      expect(screen.getByText('guest@example.com')).toBeInTheDocument();
    });
  });

  it('renders error message when API call fails', async () => {
    (adminService.getUsers as Mock).mockRejectedValue(new Error('API Error'));

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load users. Please try again.')).toBeInTheDocument();
    });
  });

  it('handles search query input', async () => {
    (adminService.getUsers as Mock).mockResolvedValue({ items: [], totalPages: 0 });

    render(<Users />);

    const searchInput = screen.getByPlaceholderText(/search enterprise users/i);
    fireEvent.change(searchInput, { target: { value: 'test@example.com' } });

    await waitFor(() => {
        expect(adminService.getUsers).toHaveBeenCalledWith(
            1,
            10,
            'test@example.com',
            undefined
        );
    });
  });

  it('handles pagination', async () => {
    (adminService.getUsers as Mock).mockResolvedValue({ items: [], totalPages: 2 });

    render(<Users />);

    await waitFor(() => {
        expect(screen.getByText('Next')).not.toBeDisabled();
    });

    const nextButton = screen.getByText('Next');
    fireEvent.click(nextButton);

    await waitFor(() => {
        expect(adminService.getUsers).toHaveBeenCalledWith(
            2,
            10,
            '',
            undefined
        );
    });
  });

  it('handles user deletion flow', async () => {
    const mockUsers = {
      items: [{ id: '1', email: 'user@example.com', roles: ['Admin'] }],
      totalPages: 1,
    };
    (adminService.getUsers as Mock).mockResolvedValue(mockUsers);
    (adminService.deleteUser as Mock).mockResolvedValue({});

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('user@example.com')).toBeInTheDocument();
    });

    const deleteButton = screen.getByTitle('Revoke Session Access');
    fireEvent.click(deleteButton);

    expect(screen.getByText('Revoke Administrative Access')).toBeInTheDocument();

    const confirmButton = screen.getByText('Deauthorize & Delete');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(adminService.deleteUser).toHaveBeenCalledWith('1');
    });
  });

  it('renders empty state when no users found', async () => {
    (adminService.getUsers as Mock).mockResolvedValue({ items: [], totalPages: 0 });

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText(/no records exist/i)).toBeInTheDocument();
    });
  });

  it('handles user deletion failure', async () => {
    const mockUsers = {
      items: [{ id: '1', email: 'user@example.com', roles: ['Admin'] }],
      totalPages: 1,
    };
    (adminService.getUsers as Mock).mockResolvedValue(mockUsers);
    (adminService.deleteUser as Mock).mockRejectedValue(new Error('Delete failed'));

    render(<Users />);

    await waitFor(() => screen.getByText('user@example.com'));
    fireEvent.click(screen.getByTitle('Revoke Session Access'));
    fireEvent.click(screen.getByText('Deauthorize & Delete'));

    await waitFor(() => {
      expect(adminService.deleteUser).toHaveBeenCalledWith('1');
    });
  });
});
