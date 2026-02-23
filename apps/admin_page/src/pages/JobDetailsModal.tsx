import React, { useEffect, useState, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, RefreshCw, StopCircle, CheckCircle2, AlertCircle, Activity, Clock } from 'lucide-react';
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
    setProcessingAction(true);
    try {
      await adminService.retryJob(job.id);
      showToast('Pipeline reset to Pending.', 'success');
      onJobUpdated();
      fetchJobDetails(job.id);
    } catch (error) {
      const axiosError = error as AxiosError<{ error: string }>;
      showToast(axiosError.response?.data?.error || "Reset failed", "error");
    } finally {
        setProcessingAction(false);
    }
  };

  const handleCancel = async () => {
    if (!job) return;
    setProcessingAction(true);
    try {
      await adminService.cancelJob(job.id);
      showToast('Pipeline termination successful.', 'success');
      onJobUpdated();
      fetchJobDetails(job.id);
    } catch (error) {
      const axiosError = error as AxiosError<{ error: string }>;
      showToast(axiosError.response?.data?.error || "Termination failed", "error");
    } finally {
        setProcessingAction(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Completed': return 'text-success-700 bg-success-50 border-success-200 shadow-sm shadow-success-100/50';
      case 'Failed': return 'text-error-700 bg-error-50 border-error-200 shadow-sm shadow-error-100/50';
      case 'Processing': return 'text-primary-700 bg-primary-50 border-primary-200 shadow-sm shadow-primary-100/50';
      default: return 'text-brand-700 bg-brand-50 border-brand-200';
    }
  };

  if (!isOpen && !jobId) return null;

  return (
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-6">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-brand-900/40 backdrop-blur-md"
            onClick={onClose}
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.9, y: 40 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.9, y: 40 }}
            className="relative bg-white rounded-[2.5rem] shadow-premium-xl border border-white/20 w-full max-w-3xl max-h-[90vh] flex flex-col pointer-events-auto z-10 overflow-hidden"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div className="px-10 py-8 border-b border-brand-100 flex items-center justify-between bg-brand-50/20">
              <div className="flex items-center gap-5">
                  <div className="p-3 bg-white rounded-2xl border border-brand-100 shadow-premium text-primary-600">
                      <Activity size={24} />
                  </div>
                  <div>
                      <h2 className="text-2xl font-black text-brand-900 tracking-tightest">Pipeline Diagnostics</h2>
                      <div className="flex items-center gap-2 mt-1">
                          <span className="text-[10px] font-black text-brand-300 uppercase tracking-widest">Descriptor:</span>
                          <span className="text-[10px] font-black text-primary-600 bg-primary-50 px-2 py-0.5 rounded-md uppercase tracking-wider">
                              {jobId}
                          </span>
                      </div>
                  </div>
              </div>
              <button
                onClick={onClose}
                className="w-12 h-12 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-brand-50 rounded-2xl transition-all duration-300"
              >
                <X size={28} />
              </button>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-10 space-y-10 custom-scrollbar">
              {loading || !job ? (
                <div className="flex flex-col items-center justify-center py-20 text-brand-200">
                  <RefreshCw className="animate-spin mb-4" size={48} />
                  <span className="font-black text-lg uppercase tracking-widest">Querying Cluster Assets...</span>
                </div>
              ) : (
                <>
                  {/* Status Grid */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                      <div className="p-6 rounded-[1.5rem] border border-brand-100 bg-brand-50/30 flex flex-col gap-1">
                          <span className="text-[10px] font-black text-brand-300 uppercase tracking-widest">Deployment Target</span>
                          <div className="font-black text-brand-900 text-2xl tracking-tight">{job.target}</div>
                      </div>
                      <div className={`p-6 rounded-[1.5rem] border flex flex-col gap-1 transition-colors duration-500 ${getStatusColor(job.status)}`}>
                          <span className="text-[10px] font-black opacity-60 uppercase tracking-widest">Operational Status</span>
                          <div className="font-black text-2xl tracking-tight flex items-center gap-3">
                              {job.status === 'Completed' && <CheckCircle2 size={24} />}
                              {job.status === 'Failed' && <AlertCircle size={24} />}
                              {job.status === 'Processing' && <RefreshCw size={24} className="animate-spin" />}
                              {job.status}
                          </div>
                      </div>
                  </div>

                  {/* Metrics */}
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <div className="p-5 rounded-2xl bg-white border border-brand-100 shadow-sm flex items-center gap-4 group">
                          <div className="p-2 bg-brand-50 rounded-xl group-hover:bg-primary-50 transition-colors">
                              <Clock size={18} className="text-brand-400 group-hover:text-primary-500" />
                          </div>
                          <div className="flex flex-col">
                              <span className="text-[10px] font-black text-brand-300 uppercase tracking-widest">Initiated</span>
                              <span className="text-xs font-black text-brand-800 font-mono">{new Date(job.createdAt).toLocaleString()}</span>
                          </div>
                      </div>
                      <div className="p-5 rounded-2xl bg-white border border-brand-100 shadow-sm flex items-center gap-4 group">
                          <div className="p-2 bg-brand-50 rounded-xl group-hover:bg-primary-50 transition-colors">
                              <Activity size={18} className="text-brand-400 group-hover:text-primary-500" />
                          </div>
                          <div className="flex flex-col">
                              <span className="text-[10px] font-black text-brand-300 uppercase tracking-widest">Processing</span>
                              <span className="text-xs font-black text-brand-800 font-mono">{job.startedAt ? new Date(job.startedAt).toLocaleString() : 'N/A'}</span>
                          </div>
                      </div>
                      <div className="p-5 rounded-2xl bg-white border border-brand-100 shadow-sm flex items-center gap-4 group">
                          <div className="p-2 bg-brand-50 rounded-xl group-hover:bg-primary-50 transition-colors">
                              <CheckCircle2 size={18} className="text-brand-400 group-hover:text-primary-500" />
                          </div>
                          <div className="flex flex-col">
                              <span className="text-[10px] font-black text-brand-300 uppercase tracking-widest">Termination</span>
                              <span className="text-xs font-black text-brand-800 font-mono">{job.completedAt ? new Date(job.completedAt).toLocaleString() : 'N/A'}</span>
                          </div>
                      </div>
                  </div>

                  {/* Progress Visualization */}
                   <div className="space-y-4">
                      <div className="flex items-center justify-between">
                          <h3 className="text-[10px] font-black text-brand-900 uppercase tracking-widest">Cluster Throughput</h3>
                          <span className="text-xl font-black text-primary-600">{job.progress}%</span>
                      </div>
                      <div className="h-4 w-full bg-brand-50 rounded-full overflow-hidden border border-brand-100 p-1">
                           <motion.div
                              className={`h-full rounded-full shadow-lg ${job.status === 'Failed' ? 'bg-error-500 shadow-error-200/50' : 'bg-linear-to-r from-primary-500 to-primary-600 shadow-primary-200/50'}`}
                              initial={{ width: 0 }}
                              animate={{ width: `${job.progress}%` }}
                              transition={{ duration: 1, ease: "circOut" }}
                          />
                      </div>
                   </div>

                  {/* Incident Log */}
                  {job.error && (
                      <motion.div
                        initial={{ opacity: 0, y: 10 }}
                        animate={{ opacity: 1, y: 0 }}
                        className="p-6 rounded-3xl bg-error-50 border border-error-100 text-error-900 shadow-sm"
                      >
                          <div className="flex items-center gap-3 mb-3">
                              <div className="p-2 bg-white rounded-xl shadow-sm">
                                  <AlertCircle size={20} className="text-error-500" />
                              </div>
                              <span className="font-black text-sm uppercase tracking-widest">Pipeline Fault Exception</span>
                          </div>
                          <p className="font-bold text-sm leading-relaxed bg-white/40 p-4 rounded-xl border border-error-100/50">{job.error}</p>
                      </motion.div>
                  )}

                  {/* Trace Logs */}
                  <div className="space-y-4">
                      <div className="flex items-center gap-3">
                          <div className="w-1.5 h-6 bg-brand-900 rounded-full" />
                          <h3 className="text-[10px] font-black text-brand-900 uppercase tracking-widest">Telemetry Trace</h3>
                      </div>
                      <div className="bg-brand-900 rounded-3xl p-8 overflow-hidden shadow-premium-xl group relative">
                          <div className="absolute top-4 right-4 text-brand-700/50 text-[10px] font-mono group-hover:text-brand-300 transition-colors uppercase">Console Output</div>
                          <div className="max-h-80 overflow-y-auto custom-scrollbar-dark pr-4">
                              <pre className="text-xs font-mono text-brand-100 whitespace-pre-wrap leading-relaxed">
                                  {job.executionLog || '// No telemetry data captured for this session.'}
                              </pre>
                          </div>
                      </div>
                  </div>
                </>
              )}
            </div>

            {/* Action Bar */}
            <div className="px-10 py-8 border-t border-brand-100 bg-brand-50/20 flex flex-col sm:flex-row justify-between items-center gap-6">
              <div className="flex items-center gap-3 text-brand-300">
                  <Activity size={16} />
                  <span className="text-[10px] font-black uppercase tracking-widest">Encrypted diagnostic session</span>
              </div>
              <div className="flex gap-4 w-full sm:w-auto">
                  <Button variant="ghost" onClick={onClose} disabled={processingAction} className="font-black text-brand-400">
                    Close Session
                  </Button>
                  {job && (
                    <>
                      {(job.status === 'Failed' || job.status === 'Completed') && (
                        <Button
                          variant="secondary"
                          onClick={handleRetry}
                          isLoading={processingAction}
                          leftIcon={!processingAction && <RefreshCw size={18} />}
                          className="px-8 shadow-premium shadow-brand-200/40"
                        >
                          Restart Pipeline
                        </Button>
                      )}
                      {(job.status === 'Pending' || job.status === 'Processing') && (
                        <Button
                          variant="danger"
                          onClick={handleCancel}
                          isLoading={processingAction}
                          leftIcon={!processingAction && <StopCircle size={18} />}
                          className="px-8 shadow-premium shadow-error-200/40"
                        >
                          Terminate Process
                        </Button>
                      )}
                    </>
                  )}
              </div>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default JobDetailsModal;
