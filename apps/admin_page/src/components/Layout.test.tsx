import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import Layout from './Layout';

// Mock useNavigate since we want to verify navigation
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('Layout Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('renders common navigation links', () => {
    render(
      <MemoryRouter>
        <Layout />
      </MemoryRouter>
    );

    expect(screen.getByText('Valora')).toBeInTheDocument();
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Batch Jobs')).toBeInTheDocument();
    expect(screen.getByText('Sign Out')).toBeInTheDocument();
  });

  it('hides Users link for non-Admin', () => {
     render(
      <MemoryRouter>
        <Layout />
      </MemoryRouter>
    );
    // Users requires Admin role
    expect(screen.queryByText('Users')).not.toBeInTheDocument();
  });

  it('shows Users link for Admin', () => {
    localStorage.setItem('admin_roles', JSON.stringify(['Admin']));
    render(
      <MemoryRouter>
        <Layout />
      </MemoryRouter>
    );
    expect(screen.getByText('Users')).toBeInTheDocument();
  });

  it('handles logout correctly', () => {
    // Set some initial data
    localStorage.setItem('admin_token', 'test-token');
    localStorage.setItem('admin_refresh_token', 'test-refresh-token');
    localStorage.setItem('admin_email', 'admin@example.com');
    localStorage.setItem('admin_userId', '123');
    localStorage.setItem('admin_roles', JSON.stringify(['Admin']));

    render(
      <MemoryRouter>
        <Layout />
      </MemoryRouter>
    );

    const logoutButton = screen.getByText('Sign Out');
    fireEvent.click(logoutButton);

    // Verify localStorage is cleared
    expect(localStorage.getItem('admin_token')).toBeNull();
    expect(localStorage.getItem('admin_refresh_token')).toBeNull();
    expect(localStorage.getItem('admin_email')).toBeNull();
    expect(localStorage.getItem('admin_userId')).toBeNull();
    expect(localStorage.getItem('admin_roles')).toBeNull();

    // Verify navigation to login
    expect(mockNavigate).toHaveBeenCalledWith('/login');
  });
});
