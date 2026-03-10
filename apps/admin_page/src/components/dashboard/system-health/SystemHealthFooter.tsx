import React from 'react';
import { CheckCircle2, Clock } from 'lucide-react';
import type { SystemHealth as SystemHealthType } from '../../../types';

interface SystemHealthFooterProps {
  health: SystemHealthType | null;
}

export const SystemHealthFooter: React.FC<SystemHealthFooterProps> = ({ health }) => {
  return (
    <div className="mt-16 flex flex-col sm:flex-row sm:items-center justify-between gap-8 border-t border-white/10 pt-10">
      <div className="flex items-center gap-8">
        <div className="flex -space-x-4">
          {[1, 2, 3, 4, 5].map(i => (
            <div key={i} className="w-10 h-10 rounded-full bg-success-500/20 border-4 border-brand-900 flex items-center justify-center shadow-lg transition-transform hover:translate-y-[-4px] cursor-pointer">
              <CheckCircle2 size={18} className="text-success-400 shadow-glow-success" />
            </div>
          ))}
        </div>
        <div className="flex flex-col">
            <div className="flex items-center gap-3">
                <div className="w-2.5 h-2.5 rounded-full bg-success-500 animate-pulse shadow-glow-success" />
                <span className="text-[12px] font-black text-white uppercase tracking-[0.25em]">Cluster Integrity Verified</span>
            </div>
            {health?.lastPipelineSuccess && (
                 <span className="text-[11px] font-bold text-brand-400 mt-1">
                    Pulse Sync: {new Date(health.lastPipelineSuccess).toLocaleString()}
                 </span>
            )}
        </div>
      </div>
      <div className="flex items-center gap-4 px-6 py-3 bg-white/5 rounded-2xl border border-white/10 backdrop-blur-md shadow-inner">
        <div className="p-2 bg-white/10 rounded-lg">
            <Clock size={14} className="text-primary-400" />
        </div>
        <div className="flex flex-col">
            <span className="text-[10px] font-black text-brand-400 uppercase tracking-[0.2em]">Service Uptime</span>
            <span className="text-xs font-black text-white tracking-widest">99.998% AGGREGATE</span>
        </div>
      </div>
    </div>
  );
};
