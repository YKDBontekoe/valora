import React from 'react';
import { motion } from 'framer-motion';
import { AlertCircle, Activity } from 'lucide-react';
import Button from '../Button';
import Skeleton from '../Skeleton';

export const BatchJobTableSkeleton: React.FC = () => {
  return (
    <>
      {[...Array(5)].map((_, i) => (
        <tr key={i}>
          <td className="px-12 py-10"><Skeleton variant="text" width="60%" height={24} /></td>
          <td className="px-12 py-10"><Skeleton variant="text" width="40%" height={20} /></td>
          <td className="px-12 py-10"><Skeleton variant="rectangular" width={100} height={32} className="rounded-2xl" /></td>
          <td className="px-12 py-10"><Skeleton variant="rectangular" width="100%" height={12} className="rounded-full" /></td>
          <td className="px-12 py-10"><Skeleton variant="text" width="70%" height={20} /></td>
          <td className="px-12 py-10"><Skeleton variant="text" width="80%" height={16} /></td>
          <td className="px-12 py-10"></td>
        </tr>
      ))}
    </>
  );
};

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

export const BatchJobTableEmpty: React.FC = () => {
  return (
    <tr>
      <td colSpan={7} className="px-12 py-60 text-center">
        <div className="flex flex-col items-center gap-10 text-brand-100">
          <div className="p-14 bg-brand-50 rounded-[3rem] border border-brand-100 shadow-inner relative overflow-hidden group/empty">
              <Activity size={120} className="opacity-10 group-hover/empty:scale-110 group-hover/empty:rotate-12 transition-transform duration-700" />
              <motion.div
                  className="absolute inset-0 bg-linear-to-br from-primary-500/5 to-transparent opacity-0 group-hover/empty:opacity-100 transition-opacity duration-700"
              />
          </div>
          <div className="flex flex-col gap-3">
              <span className="font-black text-3xl uppercase tracking-[0.3em] text-brand-200">Idle Pipeline</span>
              <p className="text-brand-300 font-bold">No active batches detected in the current cluster.</p>
          </div>
        </div>
      </td>
    </tr>
  );
};
