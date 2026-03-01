import { Clock, CheckCircle2 } from 'lucide-react';
import type { SystemHealth as SystemHealthType } from '../../types';

interface SystemHealthFooterProps {
  health: SystemHealthType | null;
}

const SystemHealthFooter = ({ health }: SystemHealthFooterProps) => {
  return (
    <div className="mt-12 flex flex-col sm:flex-row sm:items-center justify-between gap-6 border-t border-brand-100 pt-8">
      <div className="flex items-center gap-6">
        <div className="flex -space-x-3">
          {[1, 2, 3, 4].map(i => (
            <div key={i} className="w-8 h-8 rounded-full bg-success-50 border-2 border-white flex items-center justify-center shadow-sm">
              <CheckCircle2 size={16} className="text-success-500" />
            </div>
          ))}
        </div>
        <div className="flex flex-col">
            <div className="flex items-center gap-2">
                <div className="w-2 h-2 rounded-full bg-success-500 animate-pulse" />
                <span className="text-[11px] font-black text-brand-800 uppercase tracking-widest">Cluster: Amsterdam-Primary</span>
            </div>
            {health?.lastPipelineSuccess && (
                 <span className="text-[10px] font-bold text-brand-300 mt-0.5">
                    Pulse Sync: {new Date(health.lastPipelineSuccess).toLocaleString()}
                 </span>
            )}
        </div>
      </div>
      <div className="flex items-center gap-2 px-4 py-2 bg-brand-50/50 rounded-xl border border-brand-100">
        <Clock size={12} className="text-brand-300" />
        <span className="text-[10px] font-black text-brand-400 uppercase tracking-[0.2em]">
            Uptime Integrity: 99.98%
        </span>
      </div>
    </div>
  );
};

export default SystemHealthFooter;
