import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';
import BatchJobs from './BatchJobs';

vi.mock('../services/api', () => ({
  adminService: {
    getJobs: vi.fn().mockResolvedValue([]),
    startJob: vi.fn().mockResolvedValue({}),
  },
}));

describe('BatchJobs Page', () => {
  it('renders start job form', async () => {
    render(
      <MemoryRouter>
        <BatchJobs />
      </MemoryRouter>
    );

    expect(screen.getByText('Start New Job')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('City Name (e.g. Amsterdam)')).toBeInTheDocument();
    expect(screen.getByText('Start City Ingestion')).toBeInTheDocument();
  });
});
