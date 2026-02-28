import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import Login from './Login';
import { authService } from '../services/api';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../services/api', () => ({
  authService: {
    login: vi.fn(),
  },
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('Login Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('renders login form', () => {
    render(<MemoryRouter><Login /></MemoryRouter>);
    expect(screen.getByText('Valora Admin')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('admin@valora.com')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('••••••••••••')).toBeInTheDocument();
    expect(screen.getByText('Authorize Session')).toBeInTheDocument();
  });

  it('handles successful login', async () => {
    const mockData = {
      token: 'test-token',
      refreshToken: 'test-refresh',
      email: 'admin@test.com',
      userId: '1',
      roles: ['Admin'],
    };
    (authService.login as Mock).mockResolvedValue(mockData);

    render(<MemoryRouter><Login /></MemoryRouter>);

    fireEvent.change(screen.getByPlaceholderText('admin@valora.com'), { target: { value: 'admin@test.com' } });
    fireEvent.change(screen.getByPlaceholderText('••••••••••••'), { target: { value: 'password123' } });
    fireEvent.click(screen.getByText('Authorize Session'));

    await waitFor(() => {
      expect(authService.login).toHaveBeenCalledWith('admin@test.com', 'password123');
      expect(localStorage.getItem('admin_token')).toBe('test-token');
      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
  });

  it('shows error message on login failure', async () => {
    (authService.login as Mock).mockRejectedValue({
      response: { data: { error: 'Invalid credentials' } },
    });

    render(<MemoryRouter><Login /></MemoryRouter>);

    fireEvent.change(screen.getByPlaceholderText('admin@valora.com'), { target: { value: 'wrong@test.com' } });
    fireEvent.change(screen.getByPlaceholderText('••••••••••••'), { target: { value: 'wrong' } });
    fireEvent.click(screen.getByText('Authorize Session'));

    await waitFor(() => {
      expect(screen.getByText('Invalid credentials')).toBeInTheDocument();
    });
  });

  it('shows error message when user is not an admin', async () => {
    const mockData = {
      token: 'test-token',
      refreshToken: 'test-refresh',
      email: 'user@test.com',
      userId: '2',
      roles: ['User'],
    };
    (authService.login as Mock).mockResolvedValue(mockData);

    render(<MemoryRouter><Login /></MemoryRouter>);

    fireEvent.change(screen.getByPlaceholderText('admin@valora.com'), { target: { value: 'user@test.com' } });
    fireEvent.change(screen.getByPlaceholderText('••••••••••••'), { target: { value: 'password123' } });
    fireEvent.click(screen.getByText('Authorize Session'));

    await waitFor(() => {
      expect(screen.getByText('Access denied. Admin role required.')).toBeInTheDocument();
    });
  });
});
