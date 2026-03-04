import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import AiModelsTable from './AiModelsTable';
import type { AiModelConfig } from '../../services/api';

describe('AiModelsTable', () => {
  const mockConfigs: AiModelConfig[] = [
    {
      id: '1',
      feature: 'test-feature-1',
      modelId: 'model-a',
      systemPrompt: 'prompt',
      temperature: 0.7,
      maxTokens: 1000
    },
    {
      id: '2',
      feature: 'test-feature-2',
      modelId: 'model-b',
      systemPrompt: 'prompt',
      temperature: 0.5,
      maxTokens: 2000
    }
  ];

  const defaultProps = {
    configs: mockConfigs,
    loading: false,
    onEdit: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders table headers and rows', () => {
    render(<AiModelsTable {...defaultProps} />);

    expect(screen.getByText('Feature')).toBeInTheDocument();
    expect(screen.getByText('Model')).toBeInTheDocument();
    expect(screen.getByText('Temperature')).toBeInTheDocument();
    expect(screen.getByText('Max Tokens')).toBeInTheDocument();

    expect(screen.getByText('test-feature-1')).toBeInTheDocument();
    expect(screen.getByText('model-a')).toBeInTheDocument();
    expect(screen.getByText('0.7')).toBeInTheDocument();
    expect(screen.getByText('1000')).toBeInTheDocument();

    expect(screen.getByText('test-feature-2')).toBeInTheDocument();
    expect(screen.getByText('model-b')).toBeInTheDocument();
    expect(screen.getByText('0.5')).toBeInTheDocument();
    expect(screen.getByText('2000')).toBeInTheDocument();
  });

  it('calls onEdit when clicking Modify button', () => {
    render(<AiModelsTable {...defaultProps} />);

    const modifyButtons = screen.getAllByText('Modify');
    fireEvent.click(modifyButtons[0]);

    expect(defaultProps.onEdit).toHaveBeenCalledWith(mockConfigs[0]);
  });

  it('calls onEdit when clicking a row', () => {
    render(<AiModelsTable {...defaultProps} />);

    const row = screen.getByText('test-feature-1').closest('tr')!;
    fireEvent.click(row);

    expect(defaultProps.onEdit).toHaveBeenCalledWith(mockConfigs[0]);
  });

  it('renders loading skeletons', () => {
    const { container } = render(<AiModelsTable {...defaultProps} configs={[]} loading={true} />);
    const shimmers = container.querySelectorAll('div[style*="background: linear-gradient"]');
    expect(shimmers.length).toBeGreaterThan(0);
  });

  it('renders empty state', () => {
    render(<AiModelsTable {...defaultProps} configs={[]} />);

    expect(screen.getByText('No feature configurations defined')).toBeInTheDocument();
  });
});
