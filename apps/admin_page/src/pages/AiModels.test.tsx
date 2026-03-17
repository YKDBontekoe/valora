import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import AiModels from './AiModels';
import { aiService } from '../services/api';

vi.mock('../services/api', () => ({
  aiService: {
    getConfigs: vi.fn(),
    getAvailableModels: vi.fn(),
    updateConfig: vi.fn(),
    deleteConfig: vi.fn()
  }
}));

vi.mock('../services/toast', () => ({
  showToast: vi.fn()
}));

describe('AiModels', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', () => {
    (aiService.getConfigs as Mock).mockImplementation(() => new Promise(() => {}));
    (aiService.getAvailableModels as Mock).mockImplementation(() => new Promise(() => {}));

    render(<AiModels />);
    expect(screen.getByText('AI Configuration')).toBeInTheDocument();
  });

  it('loads and displays configurations', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([
      { id: '1', feature: 'test-feature', modelId: 'model-1', description: '', isEnabled: true }
    ]);
    (aiService.getAvailableModels as Mock).mockResolvedValue([
      { id: 'model-1', name: 'Model 1', promptPrice: 0, completionPrice: 0 }
    ]);

    render(<AiModels />);

    await waitFor(() => {
      expect(screen.getByText('test-feature')).toBeInTheDocument();
    });
  });

  it('opens edit modal on Configure click', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([
      { id: '1', feature: 'test-feature', modelId: 'model-1', description: '', isEnabled: true }
    ]);
    (aiService.getAvailableModels as Mock).mockResolvedValue([]);

    render(<AiModels />);

    await waitFor(() => {
      expect(screen.getByText('test-feature')).toBeInTheDocument();
    });

    const modifyBtn = screen.getByText('Modify');
    fireEvent.click(modifyBtn);

    expect(screen.getByText('Edit Feature')).toBeInTheDocument();
  });

  it('opens empty modal on New Config click', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([]);
    (aiService.getAvailableModels as Mock).mockResolvedValue([]);

    render(<AiModels />);

    await waitFor(() => {
      expect(screen.getByText('No configurations defined')).toBeInTheDocument();
    });

    const newBtn = screen.getByText('Add Feature Config');
    fireEvent.click(newBtn);

    expect(screen.getByText('Configure Feature')).toBeInTheDocument();
  });

  it('opens confirmation dialog when delete is clicked and handles successful deletion', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([
      { id: '1', feature: 'test-feature-to-delete', modelId: 'model-1', description: '', isEnabled: true }
    ]);
    (aiService.getAvailableModels as Mock).mockResolvedValue([]);
    (aiService.deleteConfig as Mock).mockResolvedValue(undefined);

    render(<AiModels />);

    await waitFor(() => {
      expect(screen.getByText('test-feature-to-delete')).toBeInTheDocument();
    });

    const deleteBtns = screen.getAllByRole('button').filter(btn => btn.querySelector('svg.lucide-trash2'));
    fireEvent.click(deleteBtns[0]);

    await waitFor(() => {
        expect(screen.getByText('Delete AI Configuration')).toBeInTheDocument();
        expect(screen.getByText(/Are you sure you want to delete the configuration for the feature/)).toBeInTheDocument();
    });

    const confirmDeleteBtn = screen.getByRole('button', { name: 'Delete' });
    fireEvent.click(confirmDeleteBtn);

    await waitFor(() => {
        expect(aiService.deleteConfig).toHaveBeenCalledWith('1');
        expect(aiService.getConfigs).toHaveBeenCalledTimes(2); // Initial load + reload
    });
  });

  it('filters configurations by search query', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([
      { id: '1', feature: 'apple-feature', modelId: 'model-1', description: '', isEnabled: true },
      { id: '2', feature: 'banana-feature', modelId: 'model-2', description: '', isEnabled: true }
    ]);
    (aiService.getAvailableModels as Mock).mockResolvedValue([]);

    render(<AiModels />);

    await waitFor(() => {
      expect(screen.getByText('apple-feature')).toBeInTheDocument();
      expect(screen.getByText('banana-feature')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText('Search by feature or model name...');
    fireEvent.change(searchInput, { target: { value: 'apple' } });

    await waitFor(() => {
      expect(screen.getByText('apple-feature')).toBeInTheDocument();
      expect(screen.queryByText('banana-feature')).not.toBeInTheDocument();
    });
  });

  it('filters configurations by status dropdown', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([
      { id: '1', feature: 'active-feature', modelId: 'model-1', description: '', isEnabled: true },
      { id: '2', feature: 'offline-feature', modelId: 'model-2', description: '', isEnabled: false }
    ]);
    (aiService.getAvailableModels as Mock).mockResolvedValue([]);

    render(<AiModels />);

    await waitFor(() => {
      expect(screen.getByText('active-feature')).toBeInTheDocument();
      expect(screen.getByText('offline-feature')).toBeInTheDocument();
    });

    const statusSelect = screen.getByDisplayValue('All Statuses');
    fireEvent.change(statusSelect, { target: { value: 'Active' } });

    await waitFor(() => {
      expect(screen.getByText('active-feature')).toBeInTheDocument();
      expect(screen.queryByText('offline-feature')).not.toBeInTheDocument();
    });

    fireEvent.change(statusSelect, { target: { value: 'Offline' } });

    await waitFor(() => {
      expect(screen.queryByText('active-feature')).not.toBeInTheDocument();
      expect(screen.getByText('offline-feature')).toBeInTheDocument();
    });
  });

  it('renders pagination controls and updates current page', async () => {
    const configs = Array.from({ length: 15 }, (_, i) => ({
      id: `${i}`, feature: `feature-${i}`, modelId: `model-${i}`, description: '', isEnabled: true
    }));

    (aiService.getConfigs as Mock).mockResolvedValue(configs);
    (aiService.getAvailableModels as Mock).mockResolvedValue([]);

    render(<AiModels />);

    await waitFor(() => {
      expect(screen.getByText('feature-0')).toBeInTheDocument();
    });

    // Page 1 assertions
    expect(screen.getByText('feature-9')).toBeInTheDocument();
    expect(screen.queryByText('feature-10')).not.toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument(); // current page
    expect(screen.getByText('2')).toBeInTheDocument(); // total pages

    // Click next page
    const nextBtn = screen.getByRole('button', { name: 'Next' });
    fireEvent.click(nextBtn);

    await waitFor(() => {
      expect(screen.queryByText('feature-9')).not.toBeInTheDocument();
      expect(screen.getByText('feature-10')).toBeInTheDocument();
      expect(screen.getByText('feature-14')).toBeInTheDocument();
    });
  });
});
