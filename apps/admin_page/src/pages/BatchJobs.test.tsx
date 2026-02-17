import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import BatchJobs from './BatchJobs';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getJobs: vi.fn(),
    startJob: vi.fn(),
  },
}));

describe('BatchJobs Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders start job form and job list', async () => {
    const mockJobs = [
      {
        id: '1',
        type: 'CityIngestion',
        status: 'Completed',
        target: 'Amsterdam',
        progress: 100,
        error: null,
        resultSummary: 'Success',
        createdAt: new Date().toISOString(),
      },
    ];
    (adminService.getJobs as Mock).mockResolvedValue(mockJobs);

    render(
      <MemoryRouter>
        <BatchJobs />
      </MemoryRouter>
    );

    expect(screen.getByText('Batch Jobs')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('City Name (e.g. Amsterdam)')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
      expect(screen.getByText('Completed')).toBeInTheDocument();
    });
  });

  it('starts a new job when form is submitted', async () => {
    (adminService.getJobs as Mock).mockResolvedValue([]);
    (adminService.startJob as Mock).mockResolvedValue({ id: '2' });

    render(
      <MemoryRouter>
        <BatchJobs />
      </MemoryRouter>
    );

    const input = screen.getByPlaceholderText('City Name (e.g. Amsterdam)');
    const button = screen.getByText('Start City Ingestion');

    fireEvent.change(input, { target: { value: 'Utrecht' } });
    fireEvent.click(button);

    await waitFor(() => {
      expect(adminService.startJob).toHaveBeenCalledWith('CityIngestion', 'Utrecht');
      expect(input).toHaveValue('');
    });
  });

  it('shows loading state and error/processing statuses', async () => {
    const mockJobs = [
      {
        id: '1',
        type: 'CityIngestion',
        status: 'Processing',
        target: 'Rotterdam',
        progress: 50,
        error: null,
        resultSummary: null,
        createdAt: new Date().toISOString(),
      },
      {
        id: '2',
        type: 'CityIngestion',
        status: 'Failed',
        target: 'InvalidCity',
        progress: 10,
        error: 'Not Found',
        resultSummary: null,
        createdAt: new Date().toISOString(),
      },
    ];
    (adminService.getJobs as Mock).mockResolvedValue(mockJobs);

    render(
      <MemoryRouter>
        <BatchJobs />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Processing')).toBeInTheDocument();
      expect(screen.getByText('Failed')).toBeInTheDocument();
      expect(screen.getByText('Not Found')).toBeInTheDocument();
      expect(screen.getByText('50%')).toBeInTheDocument();
    });
  });
});
