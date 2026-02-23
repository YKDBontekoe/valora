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

const rowVariants = {
  hidden: { opacity: 0, y: 10 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] }
  }
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
            safetySettings: ''
          })}
          variant="secondary"
          leftIcon={<Plus size={20} />}
          className="px-8 py-4"
        >
          Provision New Policy
        </Button>
      </div>

      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="bg-white rounded-[2.5rem] border border-brand-100 shadow-premium overflow-hidden"
      >
        <div className="px-10 py-8 border-b border-brand-100 bg-brand-50/30 flex items-center justify-between">
          <h2 className="font-black text-brand-900 uppercase tracking-[0.2em] text-xs flex items-center gap-3">
            <Settings2 size={18} className="text-primary-600" />
            Routing Configurations
          </h2>
          <div className="flex items-center gap-3 bg-success-50 px-4 py-2 rounded-full border border-success-100/50">
            <span className="w-2.5 h-2.5 rounded-full bg-success-500 animate-pulse" />
            <span className="text-[10px] font-black text-success-700 uppercase tracking-widest">Active Policy: Dynamic</span>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-brand-100">
            <thead>
              <tr className="bg-brand-50/10">
                <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Intent Key</th>
                <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Primary Model</th>
                <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Fallback Stack</th>
                <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Status</th>
                <th className="px-10 py-5 text-right text-[10px] font-black text-brand-400 uppercase tracking-widest">Action</th>
              </tr>
            </thead>
            <motion.tbody
                initial="hidden"
                animate="visible"
                variants={container}
                className="divide-y divide-brand-100"
            >
              {loading ? (
                [...Array(3)].map((_, i) => (
                  <tr key={i}>
                    <td className="px-10 py-7"><Skeleton variant="text" width="40%" height={20} /></td>
                    <td className="px-10 py-7"><Skeleton variant="text" width="60%" height={20} /></td>
                    <td className="px-10 py-7"><Skeleton variant="text" width="50%" height={20} /></td>
                    <td className="px-10 py-7"><Skeleton variant="rectangular" width={80} height={28} className="rounded-full" /></td>
                    <td className="px-10 py-7"></td>
                  </tr>
                ))
              ) : configs.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-10 py-32 text-center">
                    <div className="flex flex-col items-center gap-6 text-brand-200">
                      <Cpu size={64} className="opacity-10 mb-2" />
                      <span className="font-black text-xl uppercase tracking-widest">No custom policies defined</span>
                    </div>
                  </td>
                </tr>
              ) : (
                <AnimatePresence mode="popLayout">
                  {configs.map((config) => (
                    <motion.tr
                      key={config.id || config.intent}
                      variants={rowVariants}
                      whileHover={{ scale: 1.002, backgroundColor: 'var(--color-brand-50)' }}
                      className="group cursor-pointer relative"
                      onClick={() => handleEdit(config)}
                    >
                      <td className="px-10 py-6 whitespace-nowrap">
                        <div className="flex items-center gap-4">
                          <div className="p-2.5 bg-primary-50 rounded-xl text-primary-600 shadow-sm border border-primary-100/50">
                            <Sparkles size={16} />
                          </div>
                          <span className="text-sm font-black text-brand-900 group-hover:text-primary-700 transition-colors">{config.intent}</span>
                        </div>
                      </td>
                      <td className="px-10 py-6 whitespace-nowrap">
                        <span className="text-[11px] font-black text-brand-600 font-mono bg-white border border-brand-100 px-3 py-1.5 rounded-lg shadow-sm">
                          {config.primaryModel}
                        </span>
                      </td>
                      <td className="px-10 py-6 whitespace-nowrap">
                        <div className="flex flex-wrap gap-2">
                          {config.fallbackModels.length > 0 ? (
                            config.fallbackModels.map((m, idx) => (
                              <span key={idx} className="text-[10px] font-black text-brand-400 border border-brand-100 px-2 py-1 rounded-md bg-brand-50/50">
                                {m}
                              </span>
                            ))
                          ) : (
                            <span className="text-[10px] text-brand-200 font-black uppercase tracking-widest italic">Default Stack</span>
                          )}
                        </div>
                      </td>
                      <td className="px-10 py-6 whitespace-nowrap">
                        <span className={`px-4 py-1.5 rounded-full text-[10px] font-black uppercase tracking-widest flex items-center gap-2 w-fit border ${
                          config.isEnabled
                            ? 'bg-success-50 text-success-700 border-success-100 shadow-sm shadow-success-100/50'
                            : 'bg-brand-50 text-brand-400 border-brand-100 opacity-60'
                        }`}>
                          <div className={`w-2 h-2 rounded-full ${config.isEnabled ? 'bg-success-500 animate-pulse' : 'bg-brand-300'}`} />
                          {config.isEnabled ? 'Active' : 'Offline'}
                        </span>
                      </td>
                      <td className="px-10 py-6 whitespace-nowrap text-right">
                        <Button
                          onClick={(e) => { e.stopPropagation(); handleEdit(config); }}
                          variant="ghost"
                          size="sm"
                          leftIcon={<Edit2 size={16} />}
                          className="text-brand-300 hover:text-primary-600 hover:bg-primary-50"
                        >
                          Modify
                        </Button>
                      </td>
                    </motion.tr>
                  ))}
                </AnimatePresence>
              )}
            </motion.tbody>
          </table>
        </div>
      </motion.div>

      {/* Edit Modal */}
      <AnimatePresence>
        {editingConfig && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-6">
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setEditingConfig(null)}
              className="absolute inset-0 bg-brand-900/60 backdrop-blur-md"
            />
            <motion.div
              initial={{ opacity: 0, scale: 0.9, y: 40 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.9, y: 40 }}
              className="relative w-full max-w-3xl bg-white rounded-[3rem] shadow-premium-xl overflow-hidden border border-white/20"
            >
              <div className="p-10 md:p-14">
                <div className="flex items-center justify-between mb-12">
                  <div className="flex items-center gap-6">
                    <div className="w-20 h-20 bg-linear-to-br from-primary-500 to-primary-700 rounded-3xl shadow-xl shadow-primary-200/50 text-white flex items-center justify-center">
                      <Cpu size={40} />
                    </div>
                    <div>
                      <h2 className="text-3xl font-black text-brand-900 tracking-tightest">
                        {editingConfig.id ? 'Edit Policy' : 'Provision Policy'}
                      </h2>
                      <p className="text-brand-400 font-bold text-lg">Define orchestrator routing logic.</p>
                    </div>
                  </div>
                  <button
                    onClick={() => setEditingConfig(null)}
                    className="w-12 h-12 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-brand-50 rounded-2xl transition-all duration-300"
                  >
                    <X size={32} />
                  </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
                  <div className="space-y-8">
                    <div>
                      <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Intent Key</label>
                      <input
                        type="text"
                        className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-8 focus:ring-primary-500/5 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 disabled:opacity-50"
                        value={editingConfig.intent}
                        onChange={(e) => setEditingConfig({ ...editingConfig, intent: e.target.value })}
                        disabled={!!editingConfig.id}
                        placeholder="e.g. intelligent_ranking"
                      />
                    </div>

                    <div>
                      <div className="flex justify-between items-center mb-3">
                        <label className="text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] ml-1">Primary Provider</label>
                        <select
                          aria-label="Sort models"
                          value={modelSort}
                          onChange={(e) => setModelSort(e.target.value as SortOption)}
                          className="bg-brand-50 border border-brand-100 rounded-lg text-[10px] font-black text-brand-400 px-2 py-1 outline-none focus:border-primary-500 uppercase tracking-widest"
                        >
                          <option value="name">Alpha</option>
                          <option value="price_asc">Price Asc</option>
                          <option value="price_desc">Price Desc</option>
                        </select>
                      </div>
                      <select
                        aria-label="Primary Model"
                        className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-8 focus:ring-primary-500/5 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 font-mono appearance-none"
                        value={editingConfig.primaryModel}
                        onChange={(e) => setEditingConfig({ ...editingConfig, primaryModel: e.target.value })}
                      >
                        <option value="">Select compute node...</option>
                        {sortedModels.map(model => (
                          <option key={model.id} value={model.id}>
                            {model.name} (${model.promptPrice}/1M)
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  <div className="space-y-8">
                    <div>
                      <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Fallback Stack</label>
                      <div className="flex gap-3">
                        <input
                          type="text"
                          className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-8 focus:ring-primary-500/5 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 font-mono"
                          value={editingConfig.fallbackModels.join(', ')}
                          onChange={(e) => setEditingConfig({ ...editingConfig, fallbackModels: e.target.value.split(',').map(s => s.trim()).filter(s => s) })}
                          placeholder="node-a, node-b"
                        />
                         <select
                            aria-label="Add Fallback Model"
                            className="w-1/3 px-4 py-5 bg-brand-50 border border-brand-100 rounded-2xl font-black text-brand-900 text-xs outline-none hover:bg-white transition-colors cursor-pointer"
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
                            <option value="">+ ADD</option>
                            {sortedModels.map(model => (
                                <option key={model.id} value={model.id}>{model.name}</option>
                            ))}
                         </select>
                      </div>
                    </div>

                    <div>
                      <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Operational State</label>
                      <button
                        onClick={() => setEditingConfig({ ...editingConfig, isEnabled: !editingConfig.isEnabled })}
                        className={`w-full flex items-center justify-between px-8 py-5 rounded-2xl border transition-all duration-500 ${
                          editingConfig.isEnabled
                            ? 'bg-success-50 border-success-200 text-success-700 shadow-premium shadow-success-100/30'
                            : 'bg-brand-50 border-brand-100 text-brand-300'
                        }`}
                      >
                        <span className="font-black uppercase tracking-widest">{editingConfig.isEnabled ? 'Active & Routing' : 'Deactivated'}</span>
                        {editingConfig.isEnabled ? <Check size={24} /> : <X size={24} />}
                      </button>
                    </div>
                  </div>
                </div>

                <div className="mt-10">
                  <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Policy Justification</label>
                  <textarea
                    className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-8 focus:ring-primary-500/5 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 min-h-[120px] placeholder:text-brand-200"
                    value={editingConfig.description}
                    onChange={(e) => setEditingConfig({ ...editingConfig, description: e.target.value })}
                    placeholder="Provide context for this routing policy..."
                  />
                </div>

                <div className="mt-14 flex flex-col sm:flex-row items-center justify-between gap-8">
                  <div className="flex items-center gap-3 text-warning-600 bg-warning-50 px-5 py-2.5 rounded-full border border-warning-100/50">
                    <AlertCircle size={20} />
                    <span className="text-[10px] font-black uppercase tracking-widest">Immediate Propagation</span>
                  </div>
                  <div className="flex gap-5 w-full sm:w-auto">
                    <Button
                      onClick={() => setEditingConfig(null)}
                      variant="ghost"
                      className="text-brand-400 font-black px-8"
                    >
                      Discard
                    </Button>
                    <Button
                      onClick={handleSave}
                      isLoading={isSaving}
                      leftIcon={<Save size={20} />}
                      className="shadow-premium shadow-primary-200/50 px-10"
                    >
                      Commit Policy
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
