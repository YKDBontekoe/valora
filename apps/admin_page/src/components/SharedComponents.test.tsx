import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import EmptyState from './EmptyState';
import LoadingState from './LoadingState';
import ErrorState from './ErrorState';
import { Mail } from 'lucide-react';

describe('Shared Components', () => {
  describe('EmptyState', () => {
    it('renders title and description', () => {
      render(<EmptyState title="No Items" description="There are no items to display." />);
      expect(screen.getByText('No Items')).toBeInTheDocument();
      expect(screen.getByText('There are no items to display.')).toBeInTheDocument();
    });

    it('renders custom icon', () => {
      // Just verifying it renders without crashing with a custom icon
      render(<EmptyState title="Title" description="Desc" icon={Mail} />);
      expect(screen.getByText('Title')).toBeInTheDocument();
    });

    it('renders action button and handles click', () => {
      const handleClick = vi.fn();
      render(
        <EmptyState
          title="Title"
          description="Desc"
          action={{ label: 'Create New', onClick: handleClick }}
        />
      );

      const button = screen.getByText('Create New');
      expect(button).toBeInTheDocument();
      fireEvent.click(button);
      expect(handleClick).toHaveBeenCalled();
    });
  });

  describe('LoadingState', () => {
    it('renders default number of rows', () => {
      const { container } = render(<LoadingState />);
      // Default is 3 rows
      expect(container.querySelectorAll('.space-y-4 > div')).toHaveLength(3);
    });

    it('renders specified number of rows', () => {
      const { container } = render(<LoadingState rows={5} />);
      expect(container.querySelectorAll('.space-y-4 > div')).toHaveLength(5);
    });
  });

  describe('ErrorState', () => {
    it('renders default title and provided message', () => {
      render(<ErrorState message="Connection failed" />);
      expect(screen.getByText('Something went wrong')).toBeInTheDocument();
      expect(screen.getByText('Connection failed')).toBeInTheDocument();
    });

    it('renders custom title', () => {
      render(<ErrorState title="Critical Error" message="Failed" />);
      expect(screen.getByText('Critical Error')).toBeInTheDocument();
    });

    it('renders retry button and handles click', () => {
      const handleRetry = vi.fn();
      render(<ErrorState message="Failed" onRetry={handleRetry} />);

      const button = screen.getByText('Retry Connection');
      expect(button).toBeInTheDocument();
      fireEvent.click(button);
      expect(handleRetry).toHaveBeenCalled();
    });

    it('does not render retry button if onRetry is not provided', () => {
      render(<ErrorState message="Failed" />);
      expect(screen.queryByText('Retry Connection')).not.toBeInTheDocument();
    });
  });
});
