import type { ElementType } from 'react';
import { PackageOpen } from 'lucide-react';
import Button from './Button';

interface EmptyStateProps {
  title: string;
  description: string;
  icon?: ElementType;
  action?: {
    label: string;
    onClick: () => void;
  };
}

const EmptyState = ({ title, description, icon: Icon = PackageOpen, action }: EmptyStateProps) => {
  return (
    <div className="flex flex-col items-center justify-center p-12 text-center bg-white border border-brand-100 rounded-3xl shadow-sm">
      <div className="w-16 h-16 bg-brand-50 rounded-2xl flex items-center justify-center mb-6">
        <Icon className="w-8 h-8 text-brand-400" />
      </div>
      <h3 className="text-xl font-black text-brand-900 mb-2">{title}</h3>
      <p className="text-brand-500 font-medium max-w-sm mb-8">{description}</p>
      {action && (
        <Button onClick={action.onClick} variant="primary">
          {action.label}
        </Button>
      )}
    </div>
  );
};

export default EmptyState;
