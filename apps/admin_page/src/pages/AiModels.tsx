import React, { useEffect, useState, useMemo } from 'react';
import { aiService, type AiModelConfig, type AiModel, type SortOption } from '../services/api';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import { Plus, Search, ChevronLeft, ChevronRight, X } from 'lucide-react';
import { AnimatePresence } from 'framer-motion';
import AiModelsTable from '../components/ai-models/AiModelsTable';
import EditAiModelModal from '../components/ai-models/EditAiModelModal';
import ConfirmationDialog from '../components/ConfirmationDialog';

const AiModels: React.FC = () => {
  const [configs, setConfigs] = useState<AiModelConfig[]>([]);
  const [availableModels, setAvailableModels] = useState<AiModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingConfig, setEditingConfig] = useState<AiModelConfig | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [modelSort, setModelSort] = useState<SortOption>('name');
  const [deleteConfirmation, setDeleteConfirmation] = useState<{ isOpen: boolean; config: AiModelConfig | null }>({
    isOpen: false,
    config: null,
  });

  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [page, setPage] = useState(1);
  const [tableSortBy, setTableSortBy] = useState<string | undefined>(undefined);
  const pageSize = 10;

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [configsData, modelsData] = await Promise.all([
        aiService.getConfigs(),
        aiService.getAvailableModels()
      ]);
      setConfigs(configsData);
      setAvailableModels(modelsData || []);
    } catch (error) {
      console.error(error);
      showToast('Failed to load AI configurations', 'error');
    } finally {
      setLoading(false);
    }
  };

  const sortedModels = useMemo(() => {
    return [...(availableModels || [])].sort((a, b) => {
      if (modelSort === 'name') return a.name.localeCompare(b.name);
      if (modelSort === 'price_asc') {
        if (a.promptPrice !== b.promptPrice) return a.promptPrice - b.promptPrice;
        return a.completionPrice - b.completionPrice;
      }
      if (modelSort === 'price_desc') {
        if (a.promptPrice !== b.promptPrice) return b.promptPrice - a.promptPrice;
        return b.completionPrice - a.completionPrice;
      }
      return 0;
    });
  }, [availableModels, modelSort]);

  const handleEdit = (config: AiModelConfig) => {
    setEditingConfig({ ...config });
  };

  const handleDeleteClick = (config: AiModelConfig) => {
    setDeleteConfirmation({ isOpen: true, config });
  };

  const confirmDelete = async () => {
    if (!deleteConfirmation.config?.id) return;
    try {
      await aiService.deleteConfig(deleteConfirmation.config.id);
      showToast('Configuration deleted successfully.', 'success');
      setDeleteConfirmation({ isOpen: false, config: null });
      await loadData();
    } catch (error) {
      console.error(error);
      showToast('Failed to delete configuration.', 'error');
      setDeleteConfirmation({ isOpen: false, config: null });
    }
  };

  const handleSave = async () => {
    if (!editingConfig || !editingConfig.feature) {
      showToast("Feature is required", "error");
      return;
    }

    setIsSaving(true);
    try {
      await aiService.updateConfig(editingConfig.feature, editingConfig);
      showToast('Configuration saved successfully', 'success');
      setEditingConfig(null);
      loadData();
    } catch (error) {
      console.error(error);
      showToast('Failed to save configuration', 'error');
    } finally {
      setIsSaving(false);
    }
  };

  const filteredConfigs = useMemo(() => {
    return configs.filter(config => {
      const matchesSearch = config.feature.toLowerCase().includes(searchQuery.toLowerCase()) ||
                            config.modelId.toLowerCase().includes(searchQuery.toLowerCase());

      const matchesStatus = statusFilter === 'All' ? true :
                            statusFilter === 'Active' ? config.isEnabled :
                            !config.isEnabled;

      return matchesSearch && matchesStatus;
    });
  }, [configs, searchQuery, statusFilter]);

  const sortedAndFilteredConfigs = useMemo(() => {
    if (!tableSortBy) return filteredConfigs;

    return [...filteredConfigs].sort((a, b) => {
      let comparison = 0;
      if (tableSortBy.startsWith('feature')) {
        comparison = a.feature.localeCompare(b.feature);
      } else if (tableSortBy.startsWith('modelId')) {
        comparison = a.modelId.localeCompare(b.modelId);
      } else if (tableSortBy.startsWith('isEnabled')) {
        comparison = (a.isEnabled === b.isEnabled) ? 0 : a.isEnabled ? -1 : 1;
      }

      return tableSortBy.endsWith('_asc') ? comparison : -comparison;
    });
  }, [filteredConfigs, tableSortBy]);

  const totalPages = Math.max(1, Math.ceil(sortedAndFilteredConfigs.length / pageSize));

  const paginatedConfigs = useMemo(() => {
    const startIndex = (page - 1) * pageSize;
    return sortedAndFilteredConfigs.slice(startIndex, startIndex + pageSize);
  }, [sortedAndFilteredConfigs, page, pageSize]);

  useEffect(() => {
    setPage(1);
  }, [searchQuery, statusFilter, tableSortBy]);

  const toggleSort = (field: string) => {
    setTableSortBy(current => {
      if (current === `${field}_asc`) return `${field}_desc`;
      if (current === `${field}_desc`) return undefined;
      return `${field}_asc`;
    });
  };

  const hasActiveFilters = searchQuery !== '' || statusFilter !== 'All';

  const nextPage = () => {
    if (page < totalPages) setPage(p => p + 1);
  };

  const prevPage = () => {
    if (page > 1) setPage(p => p - 1);
  };

  return (
    <div className="space-y-12">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-8">
        <div>
          <h1 className="text-5xl lg:text-6xl font-black text-brand-900 tracking-tightest">AI Configuration</h1>
          <p className="text-brand-400 mt-4 font-bold text-lg">Configure LLM parameters for various features.</p>
        </div>
        <Button
          onClick={() => setEditingConfig({
            id: '',
            feature: '',
            modelId: 'openai/gpt-4o-mini',
            description: '',
            isEnabled: true,
            safetySettings: '',
            systemPrompt: '',
            temperature: 0.7,
            maxTokens: 2048,
            _clientId: Date.now().toString()
          } as AiModelConfig & { _clientId?: string })}
          variant="secondary"
          leftIcon={<Plus size={20} />}
          className="px-8 py-4"
        >
          Add Feature Config
        </Button>
      </div>

      <div className="flex flex-col sm:flex-row items-center gap-6 justify-between">
          <div className="relative w-full max-w-xl group shadow-premium rounded-3xl">
            <Search className="absolute left-5 top-1/2 -translate-y-1/2 h-6 w-6 text-brand-300 group-focus-within:text-primary-500 transition-all duration-300 group-focus-within:scale-110" />
            <input
                type="text"
                placeholder="Search by feature or model name..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-14 pr-12 py-5 bg-white border border-brand-100 rounded-3xl focus:outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all shadow-premium hover:shadow-premium-lg font-black text-brand-900 placeholder:text-brand-200"
            />
            {searchQuery && (
                <button
                    onClick={() => setSearchQuery('')}
                    className="absolute right-5 top-1/2 -translate-y-1/2 p-1 hover:bg-brand-50 rounded-full text-brand-300 hover:text-brand-600 transition-colors"
                >
                    <X size={18} />
                </button>
            )}
          </div>

          <div className="flex items-center gap-4 w-full sm:w-auto">
             <div className="flex flex-col gap-1 w-full sm:w-48">
                <label className="text-[10px] font-black text-brand-400 uppercase tracking-widest px-1">Status Filter</label>
                <select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    className="w-full bg-white border border-brand-100 rounded-2xl px-4 py-3 text-sm font-black text-brand-900 focus:outline-none focus:ring-2 focus:ring-primary-500/20 shadow-sm appearance-none cursor-pointer"
                >
                    <option value="All">All Statuses</option>
                    <option value="Active">Active</option>
                    <option value="Offline">Offline</option>
                </select>
            </div>
          </div>
      </div>

      <AiModelsTable
        configs={paginatedConfigs}
        loading={loading}
        onEdit={handleEdit}
        onDelete={handleDeleteClick}
        tableSortBy={tableSortBy}
        toggleSort={toggleSort}
        hasActiveFilters={hasActiveFilters}
      />

      <div className="flex items-center justify-between px-8 py-4 bg-white/50 rounded-3xl border border-brand-100/50">
        <div className="text-[11px] font-black text-brand-400 uppercase tracking-[0.3em]">
          Record Group <span className="text-brand-900">{page}</span> <span className="mx-4 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
        </div>
        <div className="flex gap-4">
          <Button
            variant="outline"
            size="sm"
            onClick={prevPage}
            disabled={page === 1 || loading}
            leftIcon={<ChevronLeft size={18} />}
            className="font-black bg-white"
          >
            Prev
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={nextPage}
            disabled={page === totalPages || loading}
            rightIcon={<ChevronRight size={18} />}
            className="font-black bg-white"
          >
            Next
          </Button>
        </div>
      </div>

      {/* Edit Modal */}
      <AnimatePresence>
        {editingConfig && (
          <EditAiModelModal
            editingConfig={editingConfig}
            isSaving={isSaving}
            sortedModels={sortedModels}
            modelSort={modelSort}
            onModelSortChange={setModelSort}
            onClose={() => setEditingConfig(null)}
            onSave={handleSave}
            onChange={setEditingConfig}
          />
        )}
      </AnimatePresence>

      <ConfirmationDialog
        isOpen={deleteConfirmation.isOpen}
        onClose={() => setDeleteConfirmation({ isOpen: false, config: null })}
        onConfirm={confirmDelete}
        title="Delete AI Configuration"
        message={`Are you sure you want to delete the configuration for the feature "${deleteConfirmation.config?.feature}"? This action cannot be undone.`}
        confirmLabel="Delete"
        isDestructive
      />
    </div>
  );
};

export default AiModels;
