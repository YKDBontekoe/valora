import React, { useState, useEffect } from 'react';
import { Activity } from 'lucide-react';
import { adminService } from '../services/api';
// Removed unused BatchJob import
import { showToast } from '../services/toast';
import JobDetailsModal from './JobDetailsModal';
import DatasetStatusModal from './DatasetStatusModal';
import ConfirmationDialog from '../components/ConfirmationDialog';
import { useBatchJobsPolling } from '../hooks/useBatchJobsPolling';
import { IngestionPanel } from '../components/batch-jobs/IngestionPanel';
import { BatchJobFilters } from '../components/batch-jobs/BatchJobFilters';
import { BatchJobTable } from '../components/batch-jobs/BatchJobTable';

const BatchJobs: React.FC = () => {
  // Pagination & Filtering
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('All');
  const [typeFilter, setTypeFilter] = useState('All');
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [sortBy, setSortBy] = useState<string | undefined>(undefined);
  const pageSize = 10;

  // Use Custom Hook for Data & Polling
  const {
    jobs,
    health,
    loading,
    error,
    totalPages,
    refresh
  } = useBatchJobsPolling({
    page,
    pageSize,
    statusFilter,
    typeFilter,
    searchQuery: debouncedSearch,
    sortBy
  });

  const [isStarting, setIsStarting] = useState(false);
  const [targetCity, setTargetCity] = useState('');

  // Modal State
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDatasetModalOpen, setIsDatasetModalOpen] = useState(false);
  const [confirmation, setConfirmation] = useState<{
    isOpen: boolean;
    title: string;
    message: string;
    confirmLabel: string;
    onConfirm: () => Promise<void>;
    isDestructive?: boolean;
  }>({
    isOpen: false,
    title: '',
    message: '',
    confirmLabel: '',
    onConfirm: async () => {},
  });

  const hasActiveFilters = statusFilter !== 'All' || typeFilter !== 'All' || searchQuery !== '';

  const clearFilters = () => {
    setStatusFilter('All');
    setTypeFilter('All');
    setSearchQuery('');
    setDebouncedSearch('');
    setSortBy(undefined);
    setPage(1);
  };

  // Debounce search query
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setPage(1);
    }, 500);
    return () => clearTimeout(handler);
  }, [searchQuery]);

  const toggleSort = (field: string) => {
    setSortBy(current => {
      if (current === `${field}_asc`) return `${field}_desc`;
      if (current === `${field}_desc`) return undefined;
      return `${field}_asc`;
    });
    setPage(1);
  };

  const handleStartJob = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetCity) return;

    setIsStarting(true);
    try {
      await adminService.startJob('CityIngestion', targetCity);
      showToast('Pipeline initiated successfully', 'success');
      setTargetCity('');
      refresh();
    } catch {
      // Toast handled by api interceptor
    } finally {
      setIsStarting(false);
    }
  };

  const handleIngestAll = async () => {
    setConfirmation({
        isOpen: true,
        title: 'Start Full Ingestion?',
        message: 'Are you sure you want to trigger ingestion for ALL municipalities? This will queue hundreds of jobs and may impact system performance.',
        confirmLabel: 'Start Ingestion',
        isDestructive: false,
        onConfirm: async () => {
            try {
              await adminService.startJob('AllCitiesIngestion', 'Netherlands');
              showToast('Full dataset ingestion pipeline initiated', 'success');
              refresh();
            } catch {
              showToast('Failed to start ingestion pipeline. Please try again.', 'error');
            } finally {
              setConfirmation(prev => ({ ...prev, isOpen: false }));
            }
        }
     });
  };

  const openDetails = (jobId: string) => {
    setSelectedJobId(jobId);
    setIsModalOpen(true);
  };

  const closeDetails = () => {
    setIsModalOpen(false);
    setSelectedJobId(null);
  };

  const nextPage = () => {
    if (page < totalPages) setPage(p => p + 1);
  };

  const prevPage = () => {
    if (page > 1) setPage(p => p - 1);
  };

  return (
    <div className="space-y-12">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <h1 className="text-5xl font-black text-brand-900 tracking-tightest">Batch Jobs</h1>
          <p className="text-brand-400 mt-3 font-bold text-lg">Automated data ingestion and synchronization pipelines.</p>
        </div>
        <div className="flex items-center gap-4 px-6 py-4 bg-white rounded-2xl border border-brand-100 shadow-premium group cursor-default">
            <div className="flex items-center gap-3">
                <div className={`w-2.5 h-2.5 rounded-full ${health?.status === 'Healthy' ? 'bg-success-500 animate-pulse' : 'bg-error-500'}`} />
                <span className="text-[10px] font-black text-brand-900 uppercase tracking-widest">Cluster: Primary-A</span>
            </div>
            <div className="w-px h-6 bg-brand-100" />
            <div className="flex items-center gap-3">
                <Activity size={16} className={health?.status === 'Healthy' ? "text-success-500" : "text-error-500"} />
                <span className="text-sm font-black text-brand-900">{health?.status || 'Connecting...'}</span>
            </div>
        </div>
      </div>

      <IngestionPanel
        targetCity={targetCity}
        setTargetCity={setTargetCity}
        handleStartJob={handleStartJob}
        isStarting={isStarting}
        setIsDatasetModalOpen={setIsDatasetModalOpen}
        handleIngestAll={handleIngestAll}
      />

      <div className="bg-white rounded-[2.5rem] border border-brand-100 shadow-premium overflow-hidden">
        <BatchJobFilters
          searchQuery={searchQuery}
          setSearchQuery={setSearchQuery}
          statusFilter={statusFilter}
          setStatusFilter={setStatusFilter}
          typeFilter={typeFilter}
          setTypeFilter={setTypeFilter}
          hasActiveFilters={hasActiveFilters}
          clearFilters={clearFilters}
          setPage={setPage}
        />

        <BatchJobTable
          jobs={jobs}
          loading={loading}
          error={error}
          sortBy={sortBy}
          toggleSort={toggleSort}
          refresh={refresh}
          openDetails={openDetails}
          page={page}
          totalPages={totalPages}
          prevPage={prevPage}
          nextPage={nextPage}
        />
      </div>

      <JobDetailsModal
        isOpen={isModalOpen}
        onClose={closeDetails}
        jobId={selectedJobId}
        onJobUpdated={refresh}
      />

      <DatasetStatusModal
        isOpen={isDatasetModalOpen}
        onClose={() => setIsDatasetModalOpen(false)}
      />

      <ConfirmationDialog
        isOpen={confirmation.isOpen}
        onClose={() => setConfirmation(prev => ({ ...prev, isOpen: false }))}
        onConfirm={confirmation.onConfirm}
        title={confirmation.title}
        message={confirmation.message}
        confirmLabel={confirmation.confirmLabel}
        isDestructive={confirmation.isDestructive}
      />
    </div>
  );
};

export default BatchJobs;
