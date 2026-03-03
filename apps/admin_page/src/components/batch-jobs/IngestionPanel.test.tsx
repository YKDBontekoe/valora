import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { IngestionPanel } from './IngestionPanel';

describe('IngestionPanel', () => {
  const defaultProps = {
    targetCity: '',
    setTargetCity: vi.fn(),
    handleStartJob: vi.fn((e) => e.preventDefault()),
    isStarting: false,
    setIsDatasetModalOpen: vi.fn(),
    handleIngestAll: vi.fn(),
  };

  it('renders input and buttons', () => {
    render(<IngestionPanel {...defaultProps} />);

    expect(screen.getByPlaceholderText(/Target Municipality/)).toBeInTheDocument();
    expect(screen.getByText('Execute Sync')).toBeInTheDocument();
    expect(screen.getByText('Dataset Catalog')).toBeInTheDocument();
    expect(screen.getByText('Provision All Cities')).toBeInTheDocument();
  });

  it('calls setTargetCity on input change', () => {
    render(<IngestionPanel {...defaultProps} />);

    const input = screen.getByPlaceholderText(/Target Municipality/);
    fireEvent.change(input, { target: { value: 'Utrecht' } });
    expect(defaultProps.setTargetCity).toHaveBeenCalledWith('Utrecht');
  });

  it('calls handleStartJob on form submit', () => {
    render(<IngestionPanel {...defaultProps} targetCity="Utrecht" />);

    const button = screen.getByText('Execute Sync');
    fireEvent.click(button);
    expect(defaultProps.handleStartJob).toHaveBeenCalled();
  });

  it('calls setIsDatasetModalOpen when clicking Dataset Catalog', () => {
    render(<IngestionPanel {...defaultProps} />);

    fireEvent.click(screen.getByText('Dataset Catalog'));
    expect(defaultProps.setIsDatasetModalOpen).toHaveBeenCalledWith(true);
  });

  it('calls handleIngestAll when clicking Provision All Cities', () => {
    render(<IngestionPanel {...defaultProps} />);

    fireEvent.click(screen.getByText('Provision All Cities'));
    expect(defaultProps.handleIngestAll).toHaveBeenCalled();
  });

  it('disables input and buttons when isStarting is true', () => {
    render(<IngestionPanel {...defaultProps} isStarting={true} />);

    expect(screen.getByPlaceholderText(/Target Municipality/)).toBeDisabled();
    // The text 'Execute Sync' is not present when isLoading is true in Button component
    expect(screen.queryByText('Execute Sync')).not.toBeInTheDocument();
    expect(screen.getByText('Provision All Cities').closest('button')).toBeDisabled();
  });
});
