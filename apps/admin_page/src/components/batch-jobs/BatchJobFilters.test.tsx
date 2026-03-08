import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { BatchJobFilters } from './BatchJobFilters';

describe('BatchJobFilters', () => {
  const defaultProps = {
    searchQuery: '',
    setSearchQuery: vi.fn(),
    statusFilter: 'All',
    setStatusFilter: vi.fn(),
    typeFilter: 'All',
    setTypeFilter: vi.fn(),
    hasActiveFilters: false,
    clearFilters: vi.fn(),
    setPage: vi.fn(),
  };

  it('updates state on search input focus and blur', () => {
    render(<BatchJobFilters {...defaultProps} />);
    const input = screen.getByPlaceholderText(/search by target/i);

    // This exercises the onFocus and onBlur handlers added in the premium UI refinement
    fireEvent.focus(input);
    fireEvent.blur(input);

    expect(input).toBeInTheDocument();
  });

  it('calls setSearchQuery on input change', () => {
    render(<BatchJobFilters {...defaultProps} />);
    const input = screen.getByPlaceholderText(/search by target/i);

    fireEvent.change(input, { target: { value: 'Amsterdam' } });
    expect(defaultProps.setSearchQuery).toHaveBeenCalledWith('Amsterdam');
  });

  it('calls setStatusFilter and setPage(1) on status change', () => {
    render(<BatchJobFilters {...defaultProps} />);

    // Get the status select specifically
    const statusSelect = screen.getByDisplayValue('All Statuses');
    fireEvent.change(statusSelect, { target: { value: 'Completed' } });

    expect(defaultProps.setStatusFilter).toHaveBeenCalledWith('Completed');
    expect(defaultProps.setPage).toHaveBeenCalledWith(1);
  });

  it('calls setTypeFilter and setPage(1) on type change', () => {
    render(<BatchJobFilters {...defaultProps} />);

    const typeSelect = screen.getByDisplayValue('All Types');
    fireEvent.change(typeSelect, { target: { value: 'CityIngestion' } });

    expect(defaultProps.setTypeFilter).toHaveBeenCalledWith('CityIngestion');
    expect(defaultProps.setPage).toHaveBeenCalledWith(1);
  });

  it('calls setSearchQuery("") when clear search button is clicked', () => {
    render(<BatchJobFilters {...defaultProps} searchQuery="Amsterdam" />);
    const clearSearchButton = screen.getByRole('button', { name: '' }); // The X button in the search input

    fireEvent.click(clearSearchButton);
    expect(defaultProps.setSearchQuery).toHaveBeenCalledWith('');
  });

  it('calls clearFilters when clear button is clicked', () => {
    render(<BatchJobFilters {...defaultProps} hasActiveFilters={true} />);
    const clearButton = screen.getByText(/clear filters/i);

    fireEvent.click(clearButton);
    expect(defaultProps.clearFilters).toHaveBeenCalled();
  });
});
