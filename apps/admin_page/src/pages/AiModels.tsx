import React, { useEffect, useState, useMemo } from 'react';
import { aiService, type AiModelConfig, type AiModel, type SortOption } from '../services/api';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import { Plus, Search } from 'lucide-react';
import { AnimatePresence } from 'framer-motion';
import AiModelsTable, { type ConfigSortOption } from '../components/ai-models/AiModelsTable';
import EditAiModelModal from '../components/ai-models/EditAiModelModal';

const AiModels: React.FC = () => {
  const [configs, setConfigs] = useState<AiModelConfig[]>([]);
  const [availableModels, setAvailableModels] = useState<AiModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingConfig, setEditingConfig] = useState<AiModelConfig | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [modelSort, setModelSort] = useState<SortOption>('name');

  // Filtering, sorting and pagination states
  const [searchQuery, setSearchQuery] = useState('');
  const [sortConfig, setSortConfig] = useState<{ key: ConfigSortOption; direction: 'asc' | 'desc' } | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

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

  const filteredAndSortedConfigs = useMemo(() => {
    let result = [...configs];

    // Filter
    if (searchQuery) {
      const lowerQuery = searchQuery.toLowerCase();
      result = result.filter(
        (c) =>
          c.intent.toLowerCase().includes(lowerQuery) ||
          c.primaryModel.toLowerCase().includes(lowerQuery) ||
          c.description?.toLowerCase().includes(lowerQuery)
      );
    }

    // Sort
    if (sortConfig) {
      result.sort((a, b) => {
        const aValue: unknown = a[sortConfig.key];
        const bValue: unknown = b[sortConfig.key];

        // Handle string comparison
        if (typeof aValue === 'string' && typeof bValue === 'string') {
          return sortConfig.direction === 'asc'
            ? aValue.localeCompare(bValue)
            : bValue.localeCompare(aValue);
        }

        // Handle boolean (isEnabled) comparison
        if (typeof aValue === 'boolean' && typeof bValue === 'boolean') {
          return sortConfig.direction === 'asc'
            ? (aValue === bValue ? 0 : aValue ? 1 : -1)
            : (aValue === bValue ? 0 : aValue ? -1 : 1);
        }

        return 0;
      });
    }

    return result;
  }, [configs, searchQuery, sortConfig]);

  // Pagination
  const totalPages = Math.ceil(filteredAndSortedConfigs.length / itemsPerPage);
  const paginatedConfigs = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage;
    return filteredAndSortedConfigs.slice(startIndex, startIndex + itemsPerPage);
  }, [filteredAndSortedConfigs, currentPage]);

  const handleSort = (key: ConfigSortOption) => {
    let direction: 'asc' | 'desc' = 'asc';
    if (sortConfig && sortConfig.key === key && sortConfig.direction === 'asc') {
      direction = 'desc';
    }
    setSortConfig({ key, direction });
  };

  const handleEdit = (config: AiModelConfig) => {
    setEditingConfig({ ...config });
  };

  const handleSave = async () => {
    if (!editingConfig || !editingConfig.intent) {
      showToast("Intent is required", "error");
      return;
    }

    setIsSaving(true);
    try {
      await aiService.updateConfig(editingConfig.intent, editingConfig);
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

  return (
    <div className="space-y-12">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-8">
        <div>
          <h1 className="text-5xl lg:text-6xl font-black text-brand-900 tracking-tightest">AI Orchestration</h1>
          <p className="text-brand-400 mt-4 font-bold text-lg">Configure model routing and fallback strategies for various intents.</p>
        </div>
        <Button
          onClick={() => setEditingConfig({
            id: '',
            intent: '',
            primaryModel: 'openai/gpt-4o-mini',
            fallbackModels: [],
            description: '',
            isEnabled: true,
            safetySettings: '',
            _clientId: Date.now().toString()
          } as AiModelConfig & { _clientId?: string })}
          variant="secondary"
          leftIcon={<Plus size={20} />}
          className="px-8 py-4"
        >
          Provision New Policy
        </Button>
      </div>

      <div className="flex flex-col sm:flex-row gap-4 items-center justify-between bg-white p-6 rounded-[2rem] border border-brand-100 shadow-sm">
        <div className="relative w-full sm:w-96">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-brand-300" size={20} />
          <input
            type="text"
            placeholder="Search routing configurations by intent or model..."
            value={searchQuery}
            onChange={(e) => {
              setSearchQuery(e.target.value);
              setCurrentPage(1); // Reset to first page on search
            }}
            className="w-full pl-12 pr-4 py-3 bg-brand-50/50 border-2 border-transparent focus:border-primary-500 focus:bg-white rounded-2xl outline-none transition-all font-medium text-brand-900 placeholder:text-brand-300"
          />
        </div>
      </div>

      <AiModelsTable
        configs={paginatedConfigs}
        loading={loading}
        onEdit={handleEdit}
        sortConfig={sortConfig}
        onSort={handleSort}
        searchQuery={searchQuery}
      />

      {totalPages > 1 && (
        <div className="flex justify-center items-center gap-4 mt-8">
          <Button
            onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
            disabled={currentPage === 1 || loading}
            variant="secondary"
          >
            Previous
          </Button>
          <span className="text-sm font-bold text-brand-500">
            Page {currentPage} of {totalPages}
          </span>
          <Button
            onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
            disabled={currentPage === totalPages || loading}
            variant="secondary"
          >
            Next
          </Button>
        </div>
      )}

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
    </div>
  );
};

export default AiModels;
