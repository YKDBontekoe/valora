import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ModelSelector from '../../components/ModelSelector';
import { aiService, type ExternalAiModel } from '../../services/api';
import { vi } from 'vitest';
import React from 'react';

// Mock dependencies
vi.mock('../../services/api', () => ({
  aiService: {
    getAvailableModels: vi.fn(),
  },
}));

vi.mock('../../services/toast', () => ({
  showToast: vi.fn(),
}));

// Mock Button component to avoid framer-motion issues in tests
vi.mock('../../components/Button', () => ({
  default: ({ children, onClick, ...props }: React.ComponentProps<"button">) => (
    <button onClick={onClick} {...props}>
      {children}
    </button>
  ),
}));

describe('ModelSelector', () => {
  const mockModels: ExternalAiModel[] = [
    {
      id: 'model-1',
      name: 'Free Model',
      description: 'A free model',
      contextLength: 8192,
      promptPrice: 0,
      completionPrice: 0,
      isFree: true
    },
    {
      id: 'model-2',
      name: 'Paid Model',
      description: 'A paid model',
      contextLength: 16384,
      promptPrice: 0.0001,
      completionPrice: 0.0002,
      isFree: false
    }
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', async () => {
    (aiService.getAvailableModels as unknown as ReturnType<typeof vi.fn>).mockImplementation(() => new Promise(() => {}));
    render(<ModelSelector onSelect={vi.fn()} onClose={vi.fn()} />);
    expect(screen.getByText('Loading models...')).toBeInTheDocument();
  });

  it('renders models after loading', async () => {
    (aiService.getAvailableModels as unknown as ReturnType<typeof vi.fn>).mockResolvedValue(mockModels);
    render(<ModelSelector onSelect={vi.fn()} onClose={vi.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Free Model')).toBeInTheDocument();
      expect(screen.getByText('Paid Model')).toBeInTheDocument();
    });
  });

  it('filters models by search', async () => {
    (aiService.getAvailableModels as unknown as ReturnType<typeof vi.fn>).mockResolvedValue(mockModels);
    render(<ModelSelector onSelect={vi.fn()} onClose={vi.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Free Model')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText('Search models...');
    fireEvent.change(searchInput, { target: { value: 'Paid' } });

    expect(screen.queryByText('Free Model')).not.toBeInTheDocument();
    expect(screen.getByText('Paid Model')).toBeInTheDocument();
  });

  it('calls onSelect when a model is selected', async () => {
    (aiService.getAvailableModels as unknown as ReturnType<typeof vi.fn>).mockResolvedValue(mockModels);
    const handleSelect = vi.fn();
    render(<ModelSelector onSelect={handleSelect} onClose={vi.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Free Model')).toBeInTheDocument();
    });

    const selectButtons = screen.getAllByText('Select');
    fireEvent.click(selectButtons[0]); // First one is Free Model

    expect(handleSelect).toHaveBeenCalledWith('model-1');
  });

  it('calls onClose when close button is clicked', async () => {
    (aiService.getAvailableModels as unknown as ReturnType<typeof vi.fn>).mockResolvedValue(mockModels);
    const handleClose = vi.fn();
    render(<ModelSelector onSelect={vi.fn()} onClose={handleClose} />);

    // Wait for content to load so Close button is interactive
    await waitFor(() => {
        expect(screen.getByText('Select AI Model')).toBeInTheDocument();
    });

    const closeButton = screen.getByText('Close');
    fireEvent.click(closeButton);

    expect(handleClose).toHaveBeenCalled();
  });

  it('sorts models correctly', async () => {
    (aiService.getAvailableModels as unknown as ReturnType<typeof vi.fn>).mockResolvedValue(mockModels);
    render(<ModelSelector onSelect={vi.fn()} onClose={vi.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Free Model')).toBeInTheDocument();
    });

    // Default sort is 'free' (free first).
    // We check order by finding all rows.
    // The table structure: Thead > Tr > Th... Tbody > Tr > Td...
    // Screen.getAllByRole('row') returns header row + data rows.
    let rows = screen.getAllByRole('row');
    // Row 0 is header. Row 1 is first data row.
    expect(rows[1]).toHaveTextContent('Free Model');
    expect(rows[2]).toHaveTextContent('Paid Model');

    // Change sort to Context Length (descending)
    // The select is the only combobox
    const sortSelect = screen.getByRole('combobox');
    fireEvent.change(sortSelect, { target: { value: 'context' } });

    // Re-query rows
    rows = screen.getAllByRole('row');
    expect(rows[1]).toHaveTextContent('Paid Model'); // 16384 > 8192
    expect(rows[2]).toHaveTextContent('Free Model');
  });
});
