import { useState } from 'react';
import { RefreshCw, PlayCircle, Users, Activity } from 'lucide-react';
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
          hoverBg: 'hover:bg-primary-100/50'
      },
      {
          title: 'Retry Pipeline',
          description: 'Re-queue all currently failed batch jobs.',
          icon: PlayCircle,
          onClick: handleRetryFailedJobs,
          isLoading: retrying,
          color: 'text-warning-600',
          bg: 'bg-warning-50',
          hoverBg: 'hover:bg-warning-100/50'
      },
      {
          title: 'User Control',
          description: 'Navigate to enterprise user management.',
          icon: Users,
          onClick: () => navigate('/users'),
          color: 'text-info-600',
          bg: 'bg-info-50',
          hoverBg: 'hover:bg-info-100/50'
      }
  ];

  return (
    <div className="h-full flex flex-col gap-8">
        <div className="flex items-center gap-3">
            <Activity className="text-primary-600" size={20} />
            <h2 className="text-xl font-black text-brand-900 uppercase tracking-widest">Control Center</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 xl:grid-cols-1 gap-4">
            {actionCards.map((action) => (
                <motion.button
                    key={action.title}
                    whileHover={{ x: 8 }}
                    whileTap={{ scale: 0.98 }}
                    onClick={action.onClick}
                    disabled={action.isLoading}
                    className={`flex items-center gap-5 p-6 bg-white rounded-3xl border border-brand-100 shadow-premium hover:shadow-premium-lg transition-all duration-300 text-left group ${action.hoverBg}`}
                >
                    <div className={`w-14 h-14 ${action.bg} rounded-2xl flex items-center justify-center transition-transform duration-500 group-hover:scale-110 group-hover:rotate-6`}>
                        <action.icon className={`w-7 h-7 ${action.color} ${action.isLoading ? 'animate-spin' : ''}`} />
                    </div>
                    <div className="flex-1">
                        <h3 className="text-lg font-black text-brand-900 leading-tight">{action.title}</h3>
                        <p className="text-xs font-bold text-brand-400 mt-1">{action.description}</p>
                    </div>
                </motion.button>
            ))}
        </div>
    </div>
  );
};

export default QuickActions;
