import React, { useEffect, useState, useMemo } from 'react';
import { aiService, type AiModelConfig, type AiModel, type SortOption } from '../services/api';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import { Plus } from 'lucide-react';
import { AnimatePresence } from 'framer-motion';
import AiModelsTable from '../components/ai-models/AiModelsTable';
import EditAiModelModal from '../components/ai-models/EditAiModelModal';

const AiModels: React.FC = () => {
  const [configs, setConfigs] = useState<AiModelConfig[]>([]);
  const [availableModels, setAvailableModels] = useState<AiModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingConfig, setEditingConfig] = useState<AiModelConfig | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [modelSort, setModelSort] = useState<SortOption>('name');

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

      <AiModelsTable
        configs={configs}
        loading={loading}
        onEdit={handleEdit}
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
    </div>
  );
};

export default AiModels;
