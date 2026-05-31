import React from 'react';
import { CheckCircle2, AlertTriangle, XCircle, Clock } from 'lucide-react';
import type { SystemHealth as SystemHealthType } from '../../types';

interface SystemHealthFooterProps {
  health: SystemHealthType | null;
  isStale: boolean;
}

export const SystemHealthFooter: React.FC<SystemHealthFooterProps> = ({ health, isStale }) => {
  const isHealthy = health?.status === 'Healthy' && !isStale;
  const isWarning = (health?.status === 'Degraded' || isStale) && health?.status !== 'Unhealthy';
  const isError = health?.status === 'Unhealthy';

  let statusText = "Status Unknown";
  let StatusIcon = AlertTriangle;
  let statusColorClass = "text-brand-400";
  let statusBgClass = "bg-brand-500/20";
  let pulseClass = "bg-brand-500";

  if (isHealthy) {
    statusText = "Cluster Integrity Verified";
    StatusIcon = CheckCircle2;
    statusColorClass = "text-success-400 shadow-glow-success";
    statusBgClass = "bg-success-500/20";
    pulseClass = "bg-success-500 shadow-glow-success animate-pulse";
  } else if (isWarning) {
    statusText = isStale ? "Data Stale" : "Cluster Degraded";
    StatusIcon = AlertTriangle;
    statusColorClass = "text-warning-400 shadow-glow-warning";
    statusBgClass = "bg-warning-500/20";
    pulseClass = "bg-warning-500 shadow-glow-warning";
  } else if (isError) {
    statusText = "Cluster Unhealthy";
    StatusIcon = XCircle;
    statusColorClass = "text-error-400 shadow-glow-error";
    statusBgClass = "bg-error-500/20";
    pulseClass = "bg-error-500 shadow-glow-error";
  }

  return (
    <div className="mt-16 flex flex-col sm:flex-row sm:items-center justify-between gap-8 border-t border-white/10 pt-10">
      <div className="flex items-center gap-8">
        <div className="flex -space-x-4">
          {[1, 2, 3, 4, 5].map(i => (
            <div key={i} className={`w-10 h-10 rounded-full ${statusBgClass} border-4 border-brand-900 flex items-center justify-center shadow-lg transition-transform hover:translate-y-[-4px] cursor-pointer`}>
              <StatusIcon size={18} className={statusColorClass} />
            </div>
          ))}
        </div>
        <div className="flex flex-col">
            <div className="flex items-center gap-3">
                <div className={`w-2.5 h-2.5 rounded-full ${pulseClass}`} />
                <span className="text-[12px] font-black text-white uppercase tracking-[0.25em]">{statusText}</span>
            </div>
            {!isStale && health?.lastPipelineSuccess ? (
                 <span className="text-[11px] font-bold text-brand-400 mt-1">
                    Pulse Sync: {new Date(health.lastPipelineSuccess).toLocaleString()}
                 </span>
            ) : (
                <span className="text-[11px] font-bold text-brand-400 mt-1">Pulse Sync: Unknown</span>
            )}
        </div>
      </div>
      <div className="flex items-center gap-4 px-6 py-3 bg-white/5 rounded-2xl border border-white/10 backdrop-blur-md shadow-inner">
        <div className="p-2 bg-white/10 rounded-lg">
            <Clock size={14} className="text-primary-400" />
        </div>
        <div className="flex flex-col">
            <span className="text-[10px] font-black text-brand-400 uppercase tracking-[0.2em]">Service Uptime</span>
            <span className="text-xs font-black text-white tracking-widest">{isHealthy ? "99.998% AGGREGATE" : "UNKNOWN"}</span>
        </div>
      </div>
    </div>
  );
};
