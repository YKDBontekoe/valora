import { useState } from 'react';
import { RefreshCw, PlayCircle, Users, Activity, ChevronRight } from 'lucide-react';
import { adminService } from '../../services/api';
import { showToast } from '../../services/toast';
import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';

interface QuickActionsProps {
  onRefreshStats: () => void;
}

const QuickActions = ({ onRefreshStats }: QuickActionsProps) => {
  const [retrying, setRetrying] = useState(false);
  const navigate = useNavigate();

  const handleRetryFailedJobs = async () => {
    setRetrying(true);
    try {
      const response = await adminService.getJobs(1, 100, 'Failed');
      const failedJobs = response.items;

      if (failedJobs.length === 0) {
        showToast('No failed jobs found in the cluster.', 'info');
        setRetrying(false);
        return;
      }

      let successCount = 0;
      for (const job of failedJobs) {
        try {
          await adminService.retryJob(job.id);
          successCount++;
        } catch (e) {
          console.error(`Failed to retry job ${job.id}`, e);
        }
      }

      showToast(`Successfully re-queued ${successCount} failed ${successCount === 1 ? 'job' : 'jobs'}.`, 'success');
      onRefreshStats();
    } catch (error) {
        console.error(error);
        showToast('System failed to re-queue jobs.', 'error');
    } finally {
      setRetrying(false);
    }
  };

  const actionCards = [
      {
          title: 'Sync Cluster',
          description: 'Force a full refresh of system metrics.',
          icon: RefreshCw,
          onClick: onRefreshStats,
          color: 'text-primary-600',
          bg: 'bg-primary-50',
          accent: 'bg-primary-500'
      },
      {
          title: 'Retry Pipeline',
          description: 'Re-queue all currently failed batch jobs.',
          icon: PlayCircle,
          onClick: handleRetryFailedJobs,
          isLoading: retrying,
          color: 'text-warning-600',
          bg: 'bg-warning-50',
          accent: 'bg-warning-500'
      },
      {
          title: 'User Control',
          description: 'Navigate to enterprise user management.',
          icon: Users,
          onClick: () => navigate('/users'),
          color: 'text-info-600',
          bg: 'bg-info-50',
          accent: 'bg-info-500'
      }
  ];

  return (
    <div className="h-full flex flex-col gap-8">
        <div className="flex items-center gap-4">
            <div className="w-10 h-10 bg-primary-50 rounded-xl flex items-center justify-center border border-primary-100/50 shadow-sm">
                <Activity className="text-primary-600" size={20} />
            </div>
            <h2 className="text-2xl font-black text-brand-900 tracking-tight">Control Center</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 xl:grid-cols-1 gap-6">
            {actionCards.map((action) => (
                <motion.button
                    key={action.title}
                    whileHover={{
                      x: 8,
                      transition: { type: 'spring', stiffness: 400, damping: 25 }
                    }}
                    whileTap={{ scale: 0.98 }}
                    onClick={action.onClick}
                    disabled={action.isLoading}
                    className="flex items-center gap-6 p-8 bg-white rounded-4xl border border-brand-100 shadow-premium hover:shadow-premium-xl transition-all duration-500 text-left group cursor-pointer relative overflow-hidden hover-border-gradient"
                >
                    {/* Active highlight line */}
                    <div className={`absolute left-0 top-1/2 -translate-y-1/2 w-1.5 h-1/2 ${action.accent} rounded-r-full opacity-0 group-hover:opacity-100 transition-all duration-500 scale-y-0 group-hover:scale-y-100`} />

                    <div className={`relative z-10 w-16 h-16 ${action.bg} rounded-2xl flex items-center justify-center transition-all duration-500 group-hover:scale-110 group-hover:rotate-6 group-hover:shadow-lg group-hover:shadow-brand-100/50`}>
                        <action.icon className={`w-8 h-8 ${action.color} ${action.isLoading ? 'animate-spin' : ''}`} />
                    </div>
                    <div className="relative z-10 flex-1">
                        <h3 className="text-xl font-black text-brand-900 leading-tight tracking-tight">{action.title}</h3>
                        <p className="text-xs font-bold text-brand-400 mt-2 uppercase tracking-widest">{action.description}</p>
                    </div>
                    <div className="relative z-10 opacity-0 group-hover:opacity-100 transition-all duration-500 translate-x-4 group-hover:translate-x-0">
                        <ChevronRight className={`w-6 h-6 ${action.color}`} />
                    </div>
                </motion.button>
            ))}
        </div>
    </div>
  );
};

export default QuickActions;
