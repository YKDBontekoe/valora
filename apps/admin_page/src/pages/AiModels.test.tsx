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
});
