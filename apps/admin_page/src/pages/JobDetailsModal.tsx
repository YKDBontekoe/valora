import React, { useEffect, useState, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, RefreshCw, StopCircle, FileText, CheckCircle2, AlertCircle, Activity } from 'lucide-react';
import { adminService } from '../services/api';
import type { BatchJob } from '../types';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import { AxiosError } from 'axios';

interface JobDetailsModalProps {
  isOpen: boolean;
  onClose: () => void;
  jobId: string | null;
  onJobUpdated: () => void;
}

const JobDetailsModal: React.FC<JobDetailsModalProps> = ({ isOpen, onClose, jobId, onJobUpdated }) => {
  const [job, setJob] = useState<BatchJob | null>(null);
  const [loading, setLoading] = useState(false);
  const [processingAction, setProcessingAction] = useState(false);

  const fetchJobDetails = useCallback(async (id: string) => {
    setLoading(true);
    try {
      const data = await adminService.getJobDetails(id);
      setJob(data);
    } catch {
      showToast('Failed to load job details', 'error');
      onClose();
    } finally {
      setLoading(false);
    }
  }, [onClose]);

  useEffect(() => {
    if (isOpen && jobId) {
      fetchJobDetails(jobId);
    } else {
      setJob(null);
    }
  }, [isOpen, jobId, fetchJobDetails]);

  const handleRetry = async () => {
    if (!job) return;
    if (!confirm('Are you sure you want to retry this job? It will be reset to Pending.')) return;

    setProcessingAction(true);
    try {
      await adminService.retryJob(job.id);
      showToast('Job retried successfully', 'success');
      onJobUpdated();
      fetchJobDetails(job.id); // Refresh details
    } catch (error) {
      const axiosError = error as AxiosError<{ error: string }>;
      showToast(axiosError.response?.data?.error || "Operation failed", "error");
      setProcessingAction(false);
    } finally {
        setProcessingAction(false);
    }
  };

  const handleCancel = async () => {
    if (!job) return;
    if (!confirm('Are you sure you want to cancel this job?')) return;

    setProcessingAction(true);
    try {
      await adminService.cancelJob(job.id);
      showToast('Job cancelled successfully', 'success');
      onJobUpdated();
      fetchJobDetails(job.id); // Refresh details
    } catch (error) {
      const axiosError = error as AxiosError<{ error: string }>;
      showToast(axiosError.response?.data?.error || "Operation failed", "error");
      setProcessingAction(false);
    } finally {
        setProcessingAction(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Completed': return 'text-success-600 bg-success-50 border-success-200';
      case 'Failed': return 'text-error-600 bg-error-50 border-error-200';
      case 'Processing': return 'text-primary-600 bg-primary-50 border-primary-200';
      default: return 'text-brand-600 bg-brand-50 border-brand-200';
    }
  };

  if (!isOpen && !jobId) return null;

  return (
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-brand-900/20 backdrop-blur-sm"
            onClick={onClose}
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            className="relative bg-white rounded-2xl shadow-premium-xl border border-brand-100 w-full max-w-2xl max-h-[90vh] flex flex-col pointer-events-auto z-10"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div className="px-6 py-4 border-b border-brand-100 flex items-center justify-between bg-brand-50/30">
              <div className="flex items-center gap-3">
                  <div className="p-2 bg-white rounded-lg border border-brand-100 shadow-sm">
                      <Activity size={20} className="text-primary-500" />
                  </div>
                  <div>
                      <h2 className="text-lg font-black text-brand-900 tracking-tight">Job Details</h2>
                      <p className="text-xs font-bold text-brand-400 uppercase tracking-wider">
                          ID: {jobId?.slice(0, 8)}...
                      </p>
                  </div>
              </div>
              <button
                onClick={onClose}
                className="p-2 hover:bg-brand-100 rounded-full text-brand-400 hover:text-brand-600 transition-colors"
              >
                <X size={20} />
              </button>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-6 space-y-6">
              {loading || !job ? (
                <div className="flex flex-col items-center justify-center py-12 text-brand-400">
                  <RefreshCw className="animate-spin mb-2" size={32} />
                  <span className="font-bold text-sm">Loading details...</span>
                </div>
              ) : (
                <>
                  {/* Status Card */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      <div className="p-4 rounded-xl border border-brand-100 bg-brand-50/50 space-y-1">
                          <span className="text-[10px] font-black text-brand-400 uppercase tracking-wider">Target</span>
                          <div className="font-bold text-brand-900 text-lg">{job.target}</div>
                      </div>
                      <div className={`p-4 rounded-xl border space-y-1 ${getStatusColor(job.status)}`}>
                          <span className="text-[10px] font-black opacity-60 uppercase tracking-wider">Status</span>
                          <div className="font-bold text-lg flex items-center gap-2">
                              {job.status === 'Completed' && <CheckCircle2 size={18} />}
                              {job.status === 'Failed' && <AlertCircle size={18} />}
                              {job.status === 'Processing' && <RefreshCw size={18} className="animate-spin" />}
                              {job.status}
                          </div>
                      </div>
                  </div>

                  {/* Timestamps */}
                  <div className="grid grid-cols-3 gap-2 text-xs">
                      <div className="p-3 rounded-lg bg-brand-50 border border-brand-100">
                          <span className="block text-brand-400 font-bold mb-1">Created</span>
                          <span className="font-mono text-brand-700">{new Date(job.createdAt).toLocaleString()}</span>
                      </div>
                      <div className="p-3 rounded-lg bg-brand-50 border border-brand-100">
                          <span className="block text-brand-400 font-bold mb-1">Started</span>
                          <span className="font-mono text-brand-700">{job.startedAt ? new Date(job.startedAt).toLocaleString() : '-'}</span>
                      </div>
                      <div className="p-3 rounded-lg bg-brand-50 border border-brand-100">
                          <span className="block text-brand-400 font-bold mb-1">Completed</span>
                          <span className="font-mono text-brand-700">{job.completedAt ? new Date(job.completedAt).toLocaleString() : '-'}</span>
                      </div>
                  </div>

                  {/* Progress */}
                   <div className="space-y-2">
                      <div className="flex items-center justify-between text-xs font-bold">
                          <span className="text-brand-500">Progress</span>
                          <span className="text-brand-900">{job.progress}%</span>
                      </div>
                      <div className="h-2 w-full bg-brand-100 rounded-full overflow-hidden">
                           <motion.div
                              className={`h-full ${job.status === 'Failed' ? 'bg-error-500' : 'bg-primary-500'}`}
                              initial={{ width: 0 }}
                              animate={{ width: `${job.progress}%` }}
                          />
                      </div>
                   </div>

                  {/* Error */}
                  {job.error && (
                      <div className="p-4 rounded-xl bg-error-50 border border-error-100 text-error-800 text-sm font-medium flex items-start gap-3">
                          <AlertCircle size={18} className="mt-0.5 flex-shrink-0" />
                          <div>
                              <div className="font-bold mb-1">Error Occurred</div>
                              {job.error}
                          </div>
                      </div>
                  )}

                  {/* Logs */}
                  <div className="space-y-2">
                      <div className="flex items-center gap-2 text-brand-900 font-bold text-sm">
                          <FileText size={16} className="text-brand-400" />
                          Execution Log
                      </div>
                      <div className="bg-brand-900 rounded-xl p-4 overflow-x-auto shadow-inner max-h-60">
                          <pre className="text-xs font-mono text-brand-100 whitespace-pre-wrap">
                              {job.executionLog || 'No logs available.'}
                          </pre>
                      </div>
                  </div>
                </>
              )}
            </div>

            {/* Footer */}
            <div className="px-6 py-4 border-t border-brand-100 bg-brand-50/30 flex justify-end gap-3">
              <Button variant="outline" onClick={onClose} disabled={processingAction}>
                Close
              </Button>
              {job && (
                <>
                  {(job.status === 'Failed' || job.status === 'Completed') && (
                    <Button
                      variant="secondary"
                      onClick={handleRetry}
                      isLoading={processingAction}
                      leftIcon={!processingAction && <RefreshCw size={16} />}
                    >
                      Retry Job
                    </Button>
                  )}
                  {(job.status === 'Pending' || job.status === 'Processing') && (
                    <Button
                      variant="danger"
                      onClick={handleCancel}
                      isLoading={processingAction}
                      leftIcon={!processingAction && <StopCircle size={16} />}
                    >
                      Cancel Job
                    </Button>
                  )}
                </>
              )}
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default JobDetailsModal;
