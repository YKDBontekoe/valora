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

    fireEvent.click(screen.getByText('Configure'));

    expect(screen.getByText('Edit Configuration')).toBeInTheDocument();
    expect(screen.getByDisplayValue('test-intent')).toBeDisabled();
    expect(screen.getByDisplayValue('test-model')).toBeInTheDocument();
  });

  it('handles save configuration', async () => {
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
    fireEvent.click(screen.getByText('Configure'));

    const primaryModelInput = screen.getByDisplayValue('test-model');
    fireEvent.change(primaryModelInput, { target: { value: 'new-model' } });

    fireEvent.click(screen.getByText('Apply Changes'));

    await waitFor(() => {
      expect(aiService.updateConfig).toHaveBeenCalledWith('test-intent', expect.objectContaining({
        primaryModel: 'new-model'
      }));
    });
  });
});
