import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import AiModels from './AiModels';
import { aiService } from '../services/api';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../services/api');
vi.mock('../services/toast');

describe('AiModels Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Default mock for getAvailableModels to avoid "not iterable" error
    (aiService.getAvailableModels as Mock).mockResolvedValue([]);
  });

  it('renders loading state and then data', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        intent: 'test-intent',
        primaryModel: 'test-model',
        fallbackModels: ['fallback-1'],
        isEnabled: true,
        description: 'Test description'
      }
    ]);

    render(<MemoryRouter><AiModels /></MemoryRouter>);

    expect(screen.getByText('AI Orchestration')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('test-intent')).toBeInTheDocument();
      expect(screen.getByText('test-model')).toBeInTheDocument();
      expect(screen.getByText('fallback-1')).toBeInTheDocument();
    });
  });

  it('opens edit modal on Configure click', async () => {
    (aiService.getAvailableModels as Mock).mockResolvedValue([
        { id: 'test-model', name: 'Test Model', description: '', contextLength: 1000, promptPrice: 0, completionPrice: 0 }
    ]);
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        intent: 'test-intent',
        primaryModel: 'test-model',
        fallbackModels: ['fallback-1'],
        isEnabled: true,
        description: 'Test description'
      }
    ]);

    render(<MemoryRouter><AiModels /></MemoryRouter>);

    await waitFor(() => screen.getByText('test-intent'));

    fireEvent.click(screen.getByText('Modify'));

    expect(screen.getByText('Edit Policy')).toBeInTheDocument();
    expect(screen.getByDisplayValue('test-intent')).toBeDisabled();

    // Select should have the value 'test-model'
    const select = screen.getByRole('combobox', { name: /primary model/i });
    expect(select).toHaveValue('test-model');
  });

  it('handles save configuration', async () => {
    (aiService.getAvailableModels as Mock).mockResolvedValue([
        { id: 'test-model', name: 'Test Model', description: '', contextLength: 1000, promptPrice: 0, completionPrice: 0 },
        { id: 'new-model', name: 'New Model', description: '', contextLength: 2000, promptPrice: 0.1, completionPrice: 0.2 }
    ]);
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        intent: 'test-intent',
        primaryModel: 'test-model',
        fallbackModels: [],
        isEnabled: true,
        description: ''
      }
    ]);
    (aiService.updateConfig as Mock).mockResolvedValue({});

    render(<MemoryRouter><AiModels /></MemoryRouter>);

    await waitFor(() => screen.getByText('test-intent'));
    fireEvent.click(screen.getByText('Modify'));

    const primaryModelSelect = screen.getByRole('combobox', { name: /primary model/i });
    fireEvent.change(primaryModelSelect, { target: { value: 'new-model' } });

    fireEvent.click(screen.getByText('Commit Policy'));

    await waitFor(() => {
      expect(aiService.updateConfig).toHaveBeenCalledWith('test-intent', expect.objectContaining({
        primaryModel: 'new-model'
      }));
    });
  });

  it('provisions a new policy', async () => {
    (aiService.getAvailableModels as Mock).mockResolvedValue([
        { id: 'gpt-4', name: 'GPT-4', description: '', contextLength: 8000, promptPrice: 0.03, completionPrice: 0.06 }
    ]);
    (aiService.getConfigs as Mock).mockResolvedValue([]);
    (aiService.updateConfig as Mock).mockResolvedValue({});

    render(<MemoryRouter><AiModels /></MemoryRouter>);

    fireEvent.click(screen.getByText('Provision New Policy'));

    await waitFor(() => {
        expect(screen.getByRole('heading', { name: /provision policy/i })).toBeInTheDocument();
    });

    const intentInput = screen.getByLabelText(/intent key/i);
    fireEvent.change(intentInput, { target: { value: 'new-intent' } });

    const primaryModelSelect = screen.getByRole('combobox', { name: /primary model/i });
    fireEvent.change(primaryModelSelect, { target: { value: 'gpt-4' } });

    const descriptionInput = screen.getByPlaceholderText('Provide context for this routing policy...');
    fireEvent.change(descriptionInput, { target: { value: 'New policy description' } });

    fireEvent.click(screen.getByText('Commit Policy'));

    await waitFor(() => {
      expect(aiService.updateConfig).toHaveBeenCalledWith('new-intent', expect.objectContaining({
        intent: 'new-intent',
        primaryModel: 'gpt-4',
        description: 'New policy description'
      }));
    });
  });

  it('handles fallback model management', async () => {
    (aiService.getAvailableModels as Mock).mockResolvedValue([
        { id: 'model-1', name: 'Model 1', description: '', contextLength: 1000, promptPrice: 0, completionPrice: 0 },
        { id: 'model-2', name: 'Model 2', description: '', contextLength: 1000, promptPrice: 0, completionPrice: 0 }
    ]);
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        intent: 'test-intent',
        primaryModel: 'model-1',
        fallbackModels: [],
        isEnabled: true,
        description: ''
      }
    ]);

    render(<MemoryRouter><AiModels /></MemoryRouter>);
    await waitFor(() => screen.getByText('test-intent'));
    fireEvent.click(screen.getByText('Modify'));

    const fallbackSelect = screen.getByRole('combobox', { name: /add fallback model/i });
    fireEvent.change(fallbackSelect, { target: { value: 'model-2' } });

    expect(screen.getByDisplayValue('model-2')).toBeInTheDocument();
  });

  it('handles model sorting and status toggle', async () => {
    (aiService.getAvailableModels as Mock).mockResolvedValue([
        { id: 'b-model', name: 'B Model', description: '', contextLength: 1000, promptPrice: 0.2, completionPrice: 0.4 },
        { id: 'a-model', name: 'A Model', description: '', contextLength: 1000, promptPrice: 0.1, completionPrice: 0.2 }
    ]);
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        intent: 'test-intent',
        primaryModel: 'a-model',
        fallbackModels: [],
        isEnabled: true,
        description: ''
      }
    ]);

    render(<MemoryRouter><AiModels /></MemoryRouter>);
    await waitFor(() => screen.getByText('test-intent'));
    fireEvent.click(screen.getByText('Modify'));

    // Toggle status
    const statusButton = screen.getByText(/active & routing/i);
    fireEvent.click(statusButton);
    expect(screen.getByText(/deactivated/i)).toBeInTheDocument();

    // Sort models
    const sortSelect = screen.getByLabelText(/sort models/i);
    fireEvent.change(sortSelect, { target: { value: 'price_asc' } });

    const primarySelect = screen.getByRole('combobox', { name: /primary model/i });
    const options = Array.from(primarySelect.querySelectorAll('option'));
    // First option is "Select compute node...", then A Model (price 0.1), then B Model (price 0.2)
    expect(options[1]).toHaveTextContent(/A Model/i);
    expect(options[2]).toHaveTextContent(/B Model/i);
  });

  it('handles discard policy flow', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        intent: 'test-intent',
        primaryModel: 'test-model',
        fallbackModels: [],
        isEnabled: true,
        description: ''
      }
    ]);

    render(<MemoryRouter><AiModels /></MemoryRouter>);
    await waitFor(() => screen.getByText('test-intent'));
    fireEvent.click(screen.getByText('Modify'));

    expect(screen.getByText('Edit Policy')).toBeInTheDocument();

    const discardButton = screen.getByLabelText(/discard changes/i);
    fireEvent.click(discardButton);

    await waitFor(() => {
        expect(screen.queryByText('Edit Policy')).not.toBeInTheDocument();
    });
  });
});
