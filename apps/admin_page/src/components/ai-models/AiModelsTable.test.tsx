import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import AiModelsTable from './AiModelsTable';
import type { AiModelConfig } from '../../services/api';

describe('AiModelsTable', () => {
  const mockConfigs: AiModelConfig[] = [
    {
      id: '1',
      feature: 'test-feature-1',
      modelId: 'model-a',
      isEnabled: true,
      description: 'Description 1'
    },
    {
      id: '2',
      feature: 'test-feature-2',
      modelId: 'model-b',
      isEnabled: false,
      description: 'Description 2'
    }
  ];

  const defaultProps = {
    configs: mockConfigs,
    loading: false,
    onEdit: vi.fn(),
    onDelete: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders table headers and rows', () => {
    render(<AiModelsTable {...defaultProps} />);

    expect(screen.getByText('Feature')).toBeInTheDocument();
    expect(screen.getByText('Model')).toBeInTheDocument();

    expect(screen.getByText('test-feature-1')).toBeInTheDocument();
    expect(screen.getByText('model-a')).toBeInTheDocument();

    expect(screen.getByText('test-feature-2')).toBeInTheDocument();
    expect(screen.getByText('model-b')).toBeInTheDocument();
  });

  it('renders status badges correctly', () => {
    render(<AiModelsTable {...defaultProps} />);

    expect(screen.getByText('Active')).toBeInTheDocument();
    expect(screen.getByText('Offline')).toBeInTheDocument();
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

    expect(screen.getByText('No configurations defined')).toBeInTheDocument();
  });

  it('calls onDelete when clicking the delete button', () => {
    render(<AiModelsTable {...defaultProps} />);

    // Find the trash icons and click their parent button
    const deleteBtns = screen.getAllByRole('button').filter(btn => btn.querySelector('svg.lucide-trash2'));
    fireEvent.click(deleteBtns[0]);

    expect(defaultProps.onDelete).toHaveBeenCalledWith(mockConfigs[0]);
  });

  it('calls toggleSort when column headers are clicked', () => {
    const toggleSort = vi.fn();
    render(<AiModelsTable configs={mockConfigs} loading={false} onEdit={vi.fn()} onDelete={vi.fn()} toggleSort={toggleSort} />);

    fireEvent.click(screen.getByText('Feature'));
    expect(toggleSort).toHaveBeenCalledWith('feature');
    fireEvent.click(screen.getByText('Model'));
    expect(toggleSort).toHaveBeenCalledWith('modelId');
    fireEvent.click(screen.getByText('Status'));
    expect(toggleSort).toHaveBeenCalledWith('isEnabled');
  });
});
