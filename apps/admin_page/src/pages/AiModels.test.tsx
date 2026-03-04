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
    (aiService.getAvailableModels as Mock).mockResolvedValue([]);
  });

  it('renders loading state and then data', async () => {
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        feature: 'test-feature',
        modelId: 'test-model',
        systemPrompt: 'prompt',
        temperature: 0.7,
        maxTokens: 1000
      }
    ]);

    render(<MemoryRouter><AiModels /></MemoryRouter>);

    expect(screen.getByText('AI Orchestration')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('test-feature')).toBeInTheDocument();
      expect(screen.getByText('test-model')).toBeInTheDocument();
      expect(screen.getByText('0.7')).toBeInTheDocument();
      expect(screen.getByText('1000')).toBeInTheDocument();
    });
  });

  it('opens edit modal on Configure click', async () => {
    (aiService.getAvailableModels as Mock).mockResolvedValue([
        { id: 'test-model', name: 'Test Model', description: '', contextLength: 1000, promptPrice: 0, completionPrice: 0 }
    ]);
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        feature: 'test-feature',
        modelId: 'test-model',
        systemPrompt: 'prompt',
        temperature: 0.7,
        maxTokens: 1000
      }
    ]);

    render(<MemoryRouter><AiModels /></MemoryRouter>);

    await waitFor(() => screen.getByText('test-feature'));

    fireEvent.click(screen.getByText('Modify'));

    expect(screen.getByText('Edit Configuration')).toBeInTheDocument();
    expect(screen.getByDisplayValue('test-feature')).toBeDisabled();

    // Select should have the value 'test-model'
    const select = screen.getByRole('combobox', { name: "Model" });
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
        feature: 'test-feature',
        modelId: 'test-model',
        systemPrompt: 'prompt',
        temperature: 0.7,
        maxTokens: 1000
      }
    ]);
    (aiService.updateConfig as Mock).mockResolvedValue({});

    render(<MemoryRouter><AiModels /></MemoryRouter>);

    await waitFor(() => screen.getByText('test-feature'));
    fireEvent.click(screen.getByText('Modify'));

    const primaryModelSelect = screen.getByRole('combobox', { name: "Model" });
    fireEvent.change(primaryModelSelect, { target: { value: 'new-model' } });

    fireEvent.click(screen.getByText('Commit Config'));

    await waitFor(() => {
      expect(aiService.updateConfig).toHaveBeenCalledWith('test-feature', expect.objectContaining({
        modelId: 'new-model'
      }));
    });
  });

  it('shows discard confirmation when closing with unsaved changes', async () => {
    (aiService.getAvailableModels as Mock).mockResolvedValue([
        { id: 'test-model', name: 'Test Model', description: '', contextLength: 1000, promptPrice: 0, completionPrice: 0 },
        { id: 'new-model', name: 'New Model', description: '', contextLength: 2000, promptPrice: 0.1, completionPrice: 0.2 }
    ]);
    (aiService.getConfigs as Mock).mockResolvedValue([
      {
        id: '1',
        feature: 'test-feature',
        modelId: 'test-model',
        systemPrompt: 'prompt',
        temperature: 0.7,
        maxTokens: 1000
      }
    ]);

    render(<MemoryRouter><AiModels /></MemoryRouter>);

    await waitFor(() => screen.getByText('test-feature'));
    fireEvent.click(screen.getByText('Modify'));

    const primaryModelSelect = screen.getByRole('combobox', { name: "Model" });
    fireEvent.change(primaryModelSelect, { target: { value: 'new-model' } });

    fireEvent.click(screen.getByLabelText('Close modal'));

    expect(screen.getByText('Discard Changes?')).toBeInTheDocument();
    expect(screen.getByText(/You have unsaved modifications to this configuration/)).toBeInTheDocument();

    // Confirming discard should close the modal
    fireEvent.click(screen.getByText('Discard Changes'));
    await waitFor(() => {
        expect(screen.queryByText('Edit Configuration')).not.toBeInTheDocument();
    });
  });
});
