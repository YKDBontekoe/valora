import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { BatchJobFilters } from './BatchJobFilters';

describe('BatchJobFilters', () => {
  const createProps = (overrides = {}) => ({
    searchQuery: '',
    setSearchQuery: vi.fn(),
    statusFilter: 'All',
    setStatusFilter: vi.fn(),
    typeFilter: 'All',
    setTypeFilter: vi.fn(),
    hasActiveFilters: false,
    clearFilters: vi.fn(),
    setPage: vi.fn(),
    ...overrides,
  });

  it('updates state on search input focus and blur', () => {
    const props = createProps();
    render(<BatchJobFilters {...props} />);
    const input = screen.getByPlaceholderText(/search by target/i);

    // This exercises the onFocus and onBlur handlers added in the premium UI refinement
    act(() => {
      input.focus();
    });
    expect(document.activeElement).toBe(input);

    act(() => {
      input.blur();
    });
    expect(document.activeElement).not.toBe(input);
  });

  it('calls setSearchQuery on input change', () => {
    const props = createProps();
    render(<BatchJobFilters {...props} />);
    const input = screen.getByPlaceholderText(/search by target/i);

    fireEvent.change(input, { target: { value: 'Amsterdam' } });
    expect(props.setSearchQuery).toHaveBeenCalledWith('Amsterdam');
  });

  it('calls setStatusFilter and setPage(1) on status change', () => {
    const props = createProps();
    render(<BatchJobFilters {...props} />);

    // Get the status select specifically
    const statusSelect = screen.getByDisplayValue('All Statuses');
    fireEvent.change(statusSelect, { target: { value: 'Completed' } });

    expect(props.setStatusFilter).toHaveBeenCalledWith('Completed');
    expect(props.setPage).toHaveBeenCalledWith(1);
  });

  it('calls setTypeFilter and setPage(1) on type change', () => {
    const props = createProps();
    render(<BatchJobFilters {...props} />);

    const typeSelect = screen.getByDisplayValue('All Types');
    fireEvent.change(typeSelect, { target: { value: 'CityIngestion' } });

    expect(props.setTypeFilter).toHaveBeenCalledWith('CityIngestion');
    expect(props.setPage).toHaveBeenCalledWith(1);
  });

  it('calls setSearchQuery("") when clear search button is clicked', () => {
    const props = createProps({ searchQuery: 'Amsterdam' });
    render(<BatchJobFilters {...props} />);
    const clearSearchButton = screen.getByRole('button', { name: '' }); // The X button in the search input

    fireEvent.click(clearSearchButton);
    expect(props.setSearchQuery).toHaveBeenCalledWith('');
  });

  it('calls clearFilters when clear button is clicked', () => {
    const props = createProps({ hasActiveFilters: true });
    render(<BatchJobFilters {...props} />);
    const clearButton = screen.getByText(/clear filters/i);

    fireEvent.click(clearButton);
    expect(props.clearFilters).toHaveBeenCalled();
  });
});
