import React from 'react';
import { AlertCircle } from 'lucide-react';
import Button from '../../Button';

interface BatchJobTableErrorProps {
  displayError: string | null;
  refresh: () => void;
}

export const BatchJobTableError: React.FC<BatchJobTableErrorProps> = ({ displayError, refresh }) => {
  return (
    <tr>
      <td colSpan={7} className="px-12 py-40 text-center">
        <div className="flex flex-col items-center gap-10 text-error-500">
          <div className="p-12 bg-error-50 rounded-[2.5rem] border border-error-100 shadow-glow-error">
            <AlertCircle size={80} className="opacity-40" />
          </div>
          <div className="flex flex-col gap-3">
            <span className="font-black text-4xl tracking-tightest uppercase tracking-widest">Sync Failure</span>
            <p className="text-error-600 font-bold text-lg">{displayError}</p>
          </div>
          <Button onClick={refresh} variant="outline" size="lg" className="mt-4 border-error-200 text-error-700 bg-white shadow-sm hover:shadow-glow-error">Retry Pipeline Sync</Button>
        </div>
      </td>
    </tr>
  );
};
