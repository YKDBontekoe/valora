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

    expect(screen.getByText('Modify Policy')).toBeInTheDocument();
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
});
