import React from 'react';
import { motion } from 'framer-motion';
import { Activity } from 'lucide-react';

export const BatchJobTableEmptyState: React.FC = () => {
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
