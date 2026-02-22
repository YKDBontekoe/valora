import React, { useEffect, useState, useMemo } from 'react';
import { aiService, type AiModelConfig, type AiModel } from '../services/api';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import { motion, AnimatePresence } from 'framer-motion';
import { Settings2, Cpu, Sparkles, Plus, Edit2, AlertCircle, X, Check, Save } from 'lucide-react';
import Skeleton from '../components/Skeleton';

const container = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1
    }
  }
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 }
};

type SortOption = 'name' | 'price_asc' | 'price_desc';

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
      setAvailableModels(modelsData);
    } catch (error) {
      console.error(error);
      showToast('Failed to load AI configurations', 'error');
    } finally {
      setLoading(false);
    }
  };

  const sortedModels = useMemo(() => {
    return [...availableModels].sort((a, b) => {
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
    <div className="space-y-10">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-4xl font-black text-brand-900 tracking-tight">AI Orchestration</h1>
          <p className="text-brand-500 mt-2 font-medium">Configure model routing and fallback strategies for various intents.</p>
        </div>
        <Button
          onClick={() => setEditingConfig({
            id: '',
            intent: '',
            primaryModel: 'openai/gpt-4o-mini',
            fallbackModels: [],
            description: '',
            isEnabled: true,
            safetySettings: ''
          })}
          variant="secondary"
          leftIcon={<Plus size={18} />}
        >
          New Configuration
        </Button>
      </div>

      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="bg-white rounded-[2rem] border border-brand-100 shadow-premium overflow-hidden"
      >
        <div className="px-8 py-6 border-b border-brand-100 bg-brand-50/30 flex items-center justify-between">
          <h2 className="font-black text-brand-900 uppercase tracking-wider text-xs flex items-center gap-2">
            <Settings2 size={16} className="text-primary-600" />
            Routing Configurations
          </h2>
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-success-500 animate-pulse" />
            <span className="text-[10px] font-black text-brand-700 uppercase tracking-wider">Active Policy: Dynamic</span>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-brand-100">
            <thead>
              <tr className="bg-brand-50/10">
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Intent</th>
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Primary Model</th>
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Fallback Stack</th>
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Status</th>
                <th className="px-8 py-4 text-right text-[10px] font-black text-brand-400 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-brand-100">
              {loading ? (
                [...Array(3)].map((_, i) => (
                  <tr key={i}>
                    <td className="px-8 py-6"><Skeleton variant="text" width="40%" /></td>
                    <td className="px-8 py-6"><Skeleton variant="text" width="60%" /></td>
                    <td className="px-8 py-6"><Skeleton variant="text" width="50%" /></td>
                    <td className="px-8 py-6"><Skeleton variant="rectangular" width={80} height={24} className="rounded-full" /></td>
                    <td className="px-8 py-6"></td>
                  </tr>
                ))
              ) : configs.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-8 py-16 text-center">
                    <div className="flex flex-col items-center gap-2 text-brand-400">
                      <Cpu size={32} className="opacity-20 mb-2" />
                      <span className="font-bold">No custom configurations found. Defaults are in use.</span>
                    </div>
                  </td>
                </tr>
              ) : (
                <AnimatePresence mode="popLayout">
                  {configs.map((config) => (
                    <motion.tr
                      key={config.id || config.intent}
                      variants={item}
                      initial="hidden"
                      animate="show"
                      exit="hidden"
                      layout
                      className="hover:bg-brand-50/50 transition-colors group"
                    >
                      <td className="px-8 py-5 whitespace-nowrap">
                        <div className="flex items-center gap-3">
                          <div className="p-2 bg-primary-50 rounded-lg text-primary-600">
                            <Sparkles size={14} />
                          </div>
                          <span className="text-sm font-black text-brand-900">{config.intent}</span>
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap">
                        <span className="text-sm font-bold text-brand-600 font-mono bg-brand-50 px-2 py-1 rounded">
                          {config.primaryModel}
                        </span>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap">
                        <div className="flex flex-wrap gap-1">
                          {config.fallbackModels.length > 0 ? (
                            config.fallbackModels.map((m, idx) => (
                              <span key={idx} className="text-[10px] font-bold text-brand-400 border border-brand-100 px-1.5 py-0.5 rounded">
                                {m}
                              </span>
                            ))
                          ) : (
                            <span className="text-[10px] text-brand-300 italic font-medium">No fallbacks</span>
                          )}
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap">
                        <span className={`px-3 py-1 rounded-full text-[10px] font-black uppercase tracking-wider flex items-center gap-1.5 w-fit ${
                          config.isEnabled
                            ? 'bg-success-50 text-success-700 border border-success-100'
                            : 'bg-error-50 text-error-700 border border-error-100'
                        }`}>
                          <div className={`w-1.5 h-1.5 rounded-full ${config.isEnabled ? 'bg-success-500' : 'bg-error-500'}`} />
                          {config.isEnabled ? 'Active' : 'Disabled'}
                        </span>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-right">
                        <Button
                          onClick={() => handleEdit(config)}
                          variant="ghost"
                          size="sm"
                          leftIcon={<Edit2 size={14} />}
                          className="text-brand-400 hover:text-primary-600 hover:bg-primary-50"
                        >
                          Configure
                        </Button>
                      </td>
                    </motion.tr>
                  ))}
                </AnimatePresence>
              )}
            </tbody>
          </table>
        </div>
      </motion.div>

      {/* Edit Modal */}
      <AnimatePresence>
        {editingConfig && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setEditingConfig(null)}
              className="absolute inset-0 bg-brand-900/40 backdrop-blur-sm"
            />
            <motion.div
              initial={{ opacity: 0, scale: 0.95, y: 20 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 20 }}
              className="relative w-full max-w-2xl bg-white rounded-[2.5rem] shadow-premium-xl overflow-hidden border border-white/20"
            >
              <div className="p-8 md:p-10">
                <div className="flex items-center justify-between mb-8">
                  <div className="flex items-center gap-4">
                    <div className="p-3 bg-primary-600 rounded-2xl shadow-lg shadow-primary-200/50 text-white">
                      <Cpu size={24} />
                    </div>
                    <div>
                      <h2 className="text-2xl font-black text-brand-900 tracking-tight">
                        {editingConfig.id ? 'Edit Configuration' : 'New Configuration'}
                      </h2>
                      <p className="text-brand-400 text-sm font-bold">Define how requests for this intent are handled.</p>
                    </div>
                  </div>
                  <button
                    onClick={() => setEditingConfig(null)}
                    className="p-2 text-brand-300 hover:text-brand-600 hover:bg-brand-50 rounded-xl transition-colors"
                  >
                    <X size={24} />
                  </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                  <div className="space-y-6">
                    <div>
                      <label className="block text-[10px] font-black text-brand-400 uppercase tracking-widest mb-2 ml-1">Intent Identifier</label>
                      <input
                        type="text"
                        className="w-full px-5 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900 disabled:opacity-50"
                        value={editingConfig.intent}
                        onChange={(e) => setEditingConfig({ ...editingConfig, intent: e.target.value })}
                        disabled={!!editingConfig.id}
                        placeholder="e.g. quick_summary"
                      />
                      <p className="mt-2 text-[10px] text-brand-300 font-bold ml-1">The system key used to trigger this routing.</p>
                    </div>

                    <div>
                      <div className="flex justify-between items-center mb-2">
                        <label className="text-[10px] font-black text-brand-400 uppercase tracking-widest ml-1">Primary Model</label>
                        <select
                          value={modelSort}
                          onChange={(e) => setModelSort(e.target.value as SortOption)}
                          className="bg-brand-50 border border-brand-100 rounded-lg text-[10px] font-bold text-brand-500 px-2 py-1 outline-none focus:border-primary-500"
                        >
                          <option value="name">Name</option>
                          <option value="price_asc">Price: Low to High</option>
                          <option value="price_desc">Price: High to Low</option>
                        </select>
                      </div>
                      <select
                        className="w-full px-5 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900 font-mono appearance-none"
                        value={editingConfig.primaryModel}
                        onChange={(e) => setEditingConfig({ ...editingConfig, primaryModel: e.target.value })}
                      >
                        <option value="">Select a model...</option>
                        {sortedModels.map(model => (
                          <option key={model.id} value={model.id}>
                            {model.name} ({model.id}) - ${model.promptPrice}/1M
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  <div className="space-y-6">
                    <div>
                      <label className="block text-[10px] font-black text-brand-400 uppercase tracking-widest mb-2 ml-1">Fallback Strategy (CSV)</label>
                      <div className="flex gap-2">
                        <input
                          type="text"
                          className="w-full px-5 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900 font-mono"
                          value={editingConfig.fallbackModels.join(', ')}
                          onChange={(e) => setEditingConfig({ ...editingConfig, fallbackModels: e.target.value.split(',').map(s => s.trim()).filter(s => s) })}
                          placeholder="gpt-4o-mini, claude-3-haiku"
                        />
                         <select
                            className="w-1/3 px-3 py-4 bg-brand-50 border border-brand-100 rounded-2xl font-bold text-brand-900 text-sm outline-none"
                            onChange={(e) => {
                                if (e.target.value) {
                                    if (!editingConfig.fallbackModels.includes(e.target.value)) {
                                        const newFallbacks = [...editingConfig.fallbackModels, e.target.value];
                                        setEditingConfig({ ...editingConfig, fallbackModels: newFallbacks });
                                    }
                                    e.target.value = ""; // Reset select
                                }
                            }}
                         >
                            <option value="">+ Add</option>
                            {sortedModels.map(model => (
                                <option key={model.id} value={model.id}>{model.name}</option>
                            ))}
                         </select>
                      </div>
                      <p className="mt-2 text-[10px] text-brand-300 font-bold ml-1">Comma-separated list of models to try if primary fails.</p>
                    </div>

                    <div>
                      <label className="block text-[10px] font-black text-brand-400 uppercase tracking-widest mb-2 ml-1">Routing Status</label>
                      <button
                        onClick={() => setEditingConfig({ ...editingConfig, isEnabled: !editingConfig.isEnabled })}
                        className={`w-full flex items-center justify-between p-4 rounded-2xl border transition-all ${
                          editingConfig.isEnabled
                            ? 'bg-success-50 border-success-100 text-success-700'
                            : 'bg-brand-50 border-brand-100 text-brand-400'
                        }`}
                      >
                        <span className="font-bold">{editingConfig.isEnabled ? 'Enabled & Routing' : 'Disabled / Paused'}</span>
                        {editingConfig.isEnabled ? <Check size={20} /> : <X size={20} />}
                      </button>
                    </div>
                  </div>
                </div>

                <div className="mt-8">
                  <label className="block text-[10px] font-black text-brand-400 uppercase tracking-widest mb-2 ml-1">Description & Context</label>
                  <textarea
                    className="w-full px-5 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900 min-h-[100px]"
                    value={editingConfig.description}
                    onChange={(e) => setEditingConfig({ ...editingConfig, description: e.target.value })}
                    placeholder="Describe what this intent is used for..."
                  />
                </div>

                <div className="mt-10 flex items-center justify-between gap-4">
                  <div className="flex items-center gap-2 text-warning-600">
                    <AlertCircle size={16} />
                    <span className="text-[10px] font-black uppercase tracking-wider">Changes take effect immediately</span>
                  </div>
                  <div className="flex gap-4">
                    <Button
                      onClick={() => setEditingConfig(null)}
                      variant="ghost"
                      className="text-brand-500 font-bold"
                    >
                      Discard
                    </Button>
                    <Button
                      onClick={handleSave}
                      isLoading={isSaving}
                      leftIcon={<Save size={18} />}
                      className="shadow-premium shadow-primary-200/50"
                    >
                      Apply Changes
                    </Button>
                  </div>
                </div>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default AiModels;
