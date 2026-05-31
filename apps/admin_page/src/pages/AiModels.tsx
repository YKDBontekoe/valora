import React, { useEffect, useState, useMemo } from 'react';
import { aiService, type AiModelConfig, type AiModel, type SortOption } from '../services/api';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import { Plus, Search, X } from 'lucide-react';
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
  const [sortBy, setSortBy] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
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

  // Filtering, Sorting, and Pagination logic
  const filteredConfigs = useMemo(() => {
    return configs.filter(config =>
      config.feature.toLowerCase().includes(searchQuery.toLowerCase()) ||
      config.modelId.toLowerCase().includes(searchQuery.toLowerCase())
    );
  }, [configs, searchQuery]);

  const sortedConfigs = useMemo(() => {
    if (!sortBy) return filteredConfigs;
    return [...filteredConfigs].sort((a, b) => {
      const field = sortBy.split("_")[0] as keyof AiModelConfig;
      const isAsc = sortBy.endsWith("_asc");
      const aValue = a[field];
      const bValue = b[field];

      if (typeof aValue === "string" && typeof bValue === "string") {
        return isAsc ? aValue.localeCompare(bValue) : bValue.localeCompare(aValue);
      }
      if (typeof aValue === "boolean" && typeof bValue === "boolean") {
        return isAsc ? (aValue === bValue ? 0 : aValue ? -1 : 1) : (aValue === bValue ? 0 : aValue ? 1 : -1);
      }

      const aNum = Number(aValue);
      const bNum = Number(bValue);
      if (aNum < bNum) return isAsc ? -1 : 1;
      if (aNum > bNum) return isAsc ? 1 : -1;
      return 0;

    });
  }, [filteredConfigs, sortBy]);

  const totalPages = Math.max(1, Math.ceil(sortedConfigs.length / pageSize));

  // Reset page if it exceeds total pages after filtering
  useEffect(() => {
    if (page > totalPages) {
      setPage(totalPages);
    }
  }, [totalPages, page]);

  const paginatedConfigs = useMemo(() => {
    const startIndex = (page - 1) * pageSize;
    return sortedConfigs.slice(startIndex, startIndex + pageSize);
  }, [sortedConfigs, page, pageSize]);

  const toggleSort = (field: string) => {
    setSortBy(current => {
      if (current === `${field}_asc`) return `${field}_desc`;
      if (current === `${field}_desc`) return undefined;
      return `${field}_asc`;
    });
    setPage(1);
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
                placeholder="Search feature or model..."
                value={searchQuery}
                onChange={(e) => {
                    setSearchQuery(e.target.value);
                    setPage(1);
                }}
                className="w-full pl-14 pr-12 py-5 bg-white border border-brand-100 rounded-3xl focus:outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all shadow-premium hover:shadow-premium-lg font-black text-brand-900 placeholder:text-brand-200"
            />
            {searchQuery && (
                <button
                    onClick={() => {
                        setSearchQuery('');
                        setPage(1);
                    }}
                    className="absolute right-5 top-1/2 -translate-y-1/2 p-1 hover:bg-brand-50 rounded-full text-brand-300 hover:text-brand-600 transition-colors"
                >
                    <X size={18} />
                </button>
            )}
          </div>

          <div className="flex items-center gap-2 px-4 py-2 bg-brand-100/50 rounded-2xl border border-brand-200/50">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Displaying</span>
              <span className="text-xs font-black text-brand-900">{filteredConfigs.length} Records</span>
          </div>
      </div>

      <AiModelsTable
        configs={paginatedConfigs}
        loading={loading}
        onEdit={handleEdit}
        onDelete={handleDeleteClick}
        sortBy={sortBy}
        toggleSort={toggleSort}
        page={page}
        totalPages={totalPages}
        prevPage={() => setPage(p => Math.max(1, p - 1))}
        nextPage={() => setPage(p => Math.min(totalPages, p + 1))}
      />

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
