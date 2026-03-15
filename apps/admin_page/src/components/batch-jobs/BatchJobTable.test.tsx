import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { BatchJobTable } from './BatchJobTable';
import type { BatchJob } from '../../types';

describe('BatchJobTable', () => {
  const mockJobs: BatchJob[] = [
    {
      id: '1',
      type: 'CityIngestion',
      status: 'Completed',
      target: 'Amsterdam',
      progress: 100,
      createdAt: new Date().toISOString(),
      resultSummary: 'Finished Amsterdam',
      error: null,
      startedAt: new Date().toISOString(),
      completedAt: new Date().toISOString(),
    },
    {
      id: '2',
      type: 'CityIngestion',
      status: 'Processing',
      target: 'Rotterdam',
      progress: 50,
      createdAt: new Date().toISOString(),
      resultSummary: null,
      error: null,
      startedAt: new Date().toISOString(),
      completedAt: null,
    }
  ];

  const defaultProps = {
    jobs: mockJobs,
    loading: false,
    error: null,
    sortBy: undefined,
    toggleSort: vi.fn(),
    refresh: vi.fn(),
    openDetails: vi.fn(),
    page: 1,
    totalPages: 1,
    prevPage: vi.fn(),
    nextPage: vi.fn(),
  };

  it('renders table headers and rows', () => {
    render(<BatchJobTable {...defaultProps} />);

    expect(screen.getByText('Definition')).toBeInTheDocument();
    expect(screen.getByText('Target')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();

    expect(screen.getByText('Amsterdam')).toBeInTheDocument();
    expect(screen.getByText('Rotterdam')).toBeInTheDocument();
    expect(screen.getByText('Finished Amsterdam')).toBeInTheDocument();
  });

  it('calls toggleSort when clicking headers', () => {
    render(<BatchJobTable {...defaultProps} />);

    fireEvent.click(screen.getByText('Definition'));
    expect(defaultProps.toggleSort).toHaveBeenCalledWith('type');

    fireEvent.click(screen.getByText('Target'));
    expect(defaultProps.toggleSort).toHaveBeenCalledWith('target');
  });

  it('handles keyboard navigation for sorting', () => {
    render(<BatchJobTable {...defaultProps} />);

    const header = screen.getByRole('button', { name: /sort by definition/i });
    fireEvent.keyDown(header, { key: 'Enter' });
    expect(defaultProps.toggleSort).toHaveBeenCalledWith('type');

    fireEvent.keyDown(header, { key: ' ' });
    expect(defaultProps.toggleSort).toHaveBeenCalledWith('type');
  });

  it('renders loading skeletons', () => {
    const { container } = render(<BatchJobTable {...defaultProps} jobs={[]} loading={true} />);
    // Check for the shimmer div inside Skeleton
    const shimmers = container.querySelectorAll('div[style*="background: linear-gradient"]');
    expect(shimmers.length).toBeGreaterThan(0);
  });

  it('renders error state and handles retry', () => {
    render(<BatchJobTable {...defaultProps} jobs={[]} error="Fetch failed" />);

    expect(screen.getByText('Sync Failure')).toBeInTheDocument();
    expect(screen.getByText('An error occurred while fetching jobs.')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Retry Pipeline Sync'));
    expect(defaultProps.refresh).toHaveBeenCalled();
  });

  it('renders empty state', () => {
    render(<BatchJobTable {...defaultProps} jobs={[]} />);

    expect(screen.getByText('Idle Pipeline')).toBeInTheDocument();
    expect(screen.getByText('No active batches detected in the current cluster.')).toBeInTheDocument();
  });

  it('calls openDetails when clicking a row', () => {
    render(<BatchJobTable {...defaultProps} />);

    const amsterdamRow = screen.getByText('Amsterdam').closest('tr');
    if (!amsterdamRow) throw new Error('Row not found');

    fireEvent.click(amsterdamRow);
    expect(defaultProps.openDetails).toHaveBeenCalledWith('1');
  });

  it('handles pagination', () => {
    const props = { ...defaultProps, totalPages: 2, page: 1 };
    render(<BatchJobTable {...props} />);

    fireEvent.click(screen.getByText('Next').closest('button')!);
    expect(props.nextPage).toHaveBeenCalled();

    const prevButton = screen.getByText('Prev').closest('button')!;
    expect(prevButton).toBeDisabled();
  });

  it('shows correct sort indicators', () => {
    const { rerender } = render(<BatchJobTable {...defaultProps} sortBy="target_asc" />);
    const targetHeader = screen.getByRole('columnheader', { name: /target/i });
    expect(targetHeader).toHaveAttribute('aria-sort', 'ascending');

    rerender(<BatchJobTable {...defaultProps} sortBy="target_desc" />);
    expect(targetHeader).toHaveAttribute('aria-sort', 'descending');
  });
});
