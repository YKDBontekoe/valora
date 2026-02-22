import { render, screen, waitFor } from '@testing-library/react';
import { vi, describe, it, expect } from 'vitest';
import SystemHealth from './SystemHealth';
import { adminService } from '../../services/api';

// Mock the API
vi.mock('../../services/api', () => ({
  adminService: {
    getSystemHealth: vi.fn(),
  },
}));

describe('SystemHealth Component', () => {
  it('renders loading skeletons initially', () => {
    (adminService.getSystemHealth as any).mockReturnValue(new Promise(() => {})); // Never resolves
    render(<SystemHealth />);
    // Check for static labels which are always present
    expect(screen.getByText(/Database/i)).toBeInTheDocument();
    expect(screen.getByText(/API Latency/i)).toBeInTheDocument();
  });

  it('renders health data correctly when API call succeeds', async () => {
    const mockHealth = {
      status: 'Healthy',
      databaseStatus: 'Connected',
      apiLatencyMs: 45,
      activeJobs: 2,
      queuedJobs: 1,
      failedJobs: 0,
      lastPipelineSuccess: '2023-10-27T10:00:00Z',
      timestamp: '2023-10-27T10:05:00Z'
    };
    (adminService.getSystemHealth as any).mockResolvedValue(mockHealth);

    render(<SystemHealth />);

    await waitFor(() => {
        expect(screen.getByText('Connected')).toBeInTheDocument();
        expect(screen.getByText('45ms')).toBeInTheDocument();
        expect(screen.getByText('2')).toBeInTheDocument();
        expect(screen.getByText('(1 queued)')).toBeInTheDocument();
    });
  });

  it('renders error state when API call fails', async () => {
    (adminService.getSystemHealth as any).mockRejectedValue(new Error('Network error'));

    render(<SystemHealth />);

    await waitFor(() => {
        expect(screen.getByText('System Health Monitor Unavailable')).toBeInTheDocument();
    });
  });
});
