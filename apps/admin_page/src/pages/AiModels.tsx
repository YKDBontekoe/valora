import React, { useEffect, useState } from 'react';
import { aiService, type AiModelConfig } from '../services/api';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import { motion, AnimatePresence } from 'framer-motion';
import { Settings, Plus, Edit3, CheckCircle2, XCircle, Save, X } from 'lucide-react';
import Skeleton from '../components/Skeleton';

const containerVariants = {
  visible: {
    transition: {
      staggerChildren: 0.05
    }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 10 },
  visible: { opacity: 1, y: 0 }
};

const AiModels: React.FC = () => {
  const [configs, setConfigs] = useState<AiModelConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingConfig, setEditingConfig] = useState<AiModelConfig | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    loadConfigs();
  }, []);

  const loadConfigs = async () => {
    setLoading(true);
    try {
      const data = await aiService.getConfigs();
      setConfigs(data);
    } catch (error) {
      console.error(error);
      showToast('Failed to load AI configs', 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleEdit = (config: AiModelConfig) => {
    setEditingConfig({ ...config });
  };

  const closeModal = () => {
    if (!isSaving) {
      setEditingConfig(null);
    }
  };

  const handleSave = async () => {
    if (!editingConfig || !editingConfig.intent) {
      showToast("Intent is required", "error");
      return;
    }

    if (!editingConfig.primaryModel || !editingConfig.primaryModel.includes('/')) {
        showToast("Primary model must be in 'provider/model-name' format", "error");
        return;
    }

    setIsSaving(true);
    try {
      await aiService.updateConfig(editingConfig.intent, editingConfig);
      showToast(`Configuration for '${editingConfig.intent}' updated successfully`, 'success');
      setEditingConfig(null);
      loadConfigs();
    } catch (error) {
       console.error(error);
       showToast(`Failed to save configuration for '${editingConfig.intent}'`, 'error');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="space-y-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-4xl font-black text-brand-900 tracking-tight">AI Model Configurations</h1>
          <p className="text-brand-500 mt-2 font-medium">Manage intent-based model overrides and fallback strategies.</p>
        </div>
        <Button
          variant="primary"
          leftIcon={<Plus size={18} />}
          onClick={() => {
            setEditingConfig({
                id: '',
                intent: '',
                primaryModel: 'openai/gpt-4o-mini',
                fallbackModels: [],
                description: '',
                isEnabled: true,
                safetySettings: ''
            });
          }}
        >
            Add Configuration
        </Button>
      </div>

      <div className="bg-white shadow-premium rounded-3xl overflow-hidden border border-brand-100 relative">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50/50">
              <tr>
                <th className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Intent</th>
                <th className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Primary Model</th>
                <th className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Fallback Strategies</th>
                <th className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Status</th>
                <th className="px-8 py-5 text-right text-[10px] font-black text-brand-400 uppercase tracking-widest">Actions</th>
              </tr>
            </thead>
            <motion.tbody
              variants={containerVariants}
              initial="hidden"
              animate="visible"
              className="bg-white divide-y divide-brand-100"
            >
              <AnimatePresence mode="wait">
                {loading ? (
                  <motion.tr key="loading" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
                    <td colSpan={5} className="p-0">
                      {[...Array(5)].map((_, i) => (
                        <div key={`skeleton-${i}`} className="px-8 py-6 flex items-center justify-between border-b border-brand-100 last:border-0">
                          <Skeleton variant="text" width="20%" height={16} />
                          <Skeleton variant="text" width="30%" height={16} />
                          <Skeleton variant="text" width="15%" height={16} />
                          <Skeleton variant="rectangular" width={80} height={24} className="rounded-lg" />
                          <Skeleton variant="circular" width={32} height={32} />
                        </div>
                      ))}
                    </td>
                  </motion.tr>
                ) : configs.length === 0 ? (
                  <motion.tr key="empty" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
                      <td colSpan={5} className="px-8 py-20 text-center">
                          <div className="flex flex-col items-center gap-4">
                              <div className="p-4 bg-brand-50 rounded-full">
                                  <Settings className="h-8 w-8 text-brand-200" />
                              </div>
                              <span className="text-brand-500 font-bold">No custom configurations found. Defaults will be used.</span>
                          </div>
                      </td>
                  </motion.tr>
                ) : (
                  configs.map((config) => (
                    <motion.tr
                      key={config.id || config.intent}
                      variants={itemVariants}
                      initial="hidden"
                      animate="visible"
                      exit={{ opacity: 0, scale: 0.98 }}
                      className="hover:bg-brand-50/50 transition-all duration-200 group cursor-default hover:scale-[1.005] hover:shadow-sm"
                    >
                      <td className="px-8 py-5 whitespace-nowrap text-sm font-black text-brand-900">{config.intent}</td>
                      <td className="px-8 py-5 whitespace-nowrap text-sm font-bold text-brand-600">{config.primaryModel}</td>
                      <td className="px-8 py-5 whitespace-nowrap">
                        <div className="flex flex-wrap gap-1">
                          {config.fallbackModels.length > 0 ? config.fallbackModels.map((m, idx) => (
                             <span key={idx} className="px-2 py-0.5 bg-brand-50 text-brand-500 text-[10px] font-bold rounded-md border border-brand-100">{m}</span>
                          )) : <span className="text-brand-300 italic text-xs">No fallbacks</span>}
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap">
                        <span className={`inline-flex items-center gap-1.5 px-3 py-1 text-[10px] font-black uppercase tracking-wider rounded-lg border ${
                          config.isEnabled
                            ? 'bg-success-50 text-success-700 border-success-100 shadow-sm shadow-success-100/50'
                            : 'bg-error-50 text-error-700 border-error-100'
                        }`}>
                          {config.isEnabled ? <CheckCircle2 size={12} /> : <XCircle size={12} />}
                          {config.isEnabled ? 'Active' : 'Disabled'}
                        </span>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-right">
                        <motion.button
                          whileHover={{ scale: 1.1 }}
                          whileTap={{ scale: 0.9 }}
                          onClick={() => handleEdit(config)}
                          className="p-2.5 rounded-xl text-brand-400 hover:text-primary-600 hover:bg-primary-50 hover:shadow-sm transition-all cursor-pointer"
                        >
                          <Edit3 className="h-5 w-5" />
                        </motion.button>
                      </td>
                    </motion.tr>
                  ))
                )}
              </AnimatePresence>
            </motion.tbody>
          </table>
        </div>
      </div>

      <AnimatePresence>
        {editingConfig && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 bg-brand-900/40 backdrop-blur-sm"
              onClick={closeModal}
            />
            <motion.div
              initial={{ opacity: 0, scale: 0.95, y: 20 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 20 }}
              className="relative w-full max-w-lg bg-white rounded-[2rem] shadow-premium-xl border border-brand-100 overflow-hidden z-10"
            >
              <div className="p-8">
                <div className="flex items-center justify-between mb-8">
                  <div className="flex items-center gap-3">
                    <div className="p-3 bg-primary-50 text-primary-600 rounded-2xl">
                      <Settings size={24} />
                    </div>
                    <div>
                      <h2 className="text-2xl font-black text-brand-900 tracking-tight">
                        {editingConfig.id ? 'Edit' : 'Add'} Configuration
                      </h2>
                      <p className="text-xs font-bold text-brand-400 uppercase tracking-widest mt-1">AI Service Override</p>
                    </div>
                  </div>
                  <button
                    onClick={closeModal}
                    disabled={isSaving}
                    className="p-2 text-brand-400 hover:text-brand-900 transition-colors rounded-xl hover:bg-brand-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <X size={20} />
                  </button>
                </div>

                <div className="space-y-6">
                  <div>
                    <label className="block text-[10px] font-black text-brand-400 mb-2 ml-1 uppercase tracking-[0.2em]">Intent identifier</label>
                    <input
                      type="text"
                      className="w-full px-5 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900 disabled:opacity-50"
                      value={editingConfig.intent}
                      onChange={(e) => setEditingConfig({ ...editingConfig, intent: e.target.value })}
                      disabled={!!editingConfig.id || isSaving}
                      placeholder="e.g. context_report"
                    />
                  </div>

                  <div className="grid grid-cols-1 gap-6">
                    <div>
                      <label className="block text-[10px] font-black text-brand-400 mb-2 ml-1 uppercase tracking-[0.2em]">Primary Model</label>
                      <input
                        type="text"
                        className="w-full px-5 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900"
                        value={editingConfig.primaryModel}
                        onChange={(e) => setEditingConfig({ ...editingConfig, primaryModel: e.target.value })}
                        disabled={isSaving}
                        placeholder="openai/gpt-4o"
                      />
                    </div>

                    <div>
                      <label className="block text-[10px] font-black text-brand-400 mb-2 ml-1 uppercase tracking-[0.2em]">Fallback Sequence (comma separated)</label>
                      <input
                        type="text"
                        className="w-full px-5 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900"
                        value={editingConfig.fallbackModels.join(', ')}
                        onChange={(e) => setEditingConfig({ ...editingConfig, fallbackModels: e.target.value.split(',').map(s => s.trim()).filter(s => s) })}
                        disabled={isSaving}
                        placeholder="openai/gpt-4o-mini, anthropic/claude-3-haiku"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-[10px] font-black text-brand-400 mb-2 ml-1 uppercase tracking-[0.2em]">Description & Purpose</label>
                    <textarea
                      className="w-full px-5 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900 min-h-[100px]"
                      value={editingConfig.description}
                      onChange={(e) => setEditingConfig({ ...editingConfig, description: e.target.value })}
                      disabled={isSaving}
                      placeholder="Explain why this override exists..."
                    />
                  </div>

                  <div className="flex items-center justify-between p-4 bg-brand-50/50 rounded-2xl border border-brand-100">
                    <div className="flex items-center gap-3">
                      <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${editingConfig.isEnabled ? 'bg-success-100 text-success-600' : 'bg-brand-200 text-brand-400'}`}>
                        {editingConfig.isEnabled ? <CheckCircle2 size={20} /> : <XCircle size={20} />}
                      </div>
                      <div>
                        <span className="block text-sm font-black text-brand-900">Enable Configuration</span>
                        <span className="text-[10px] font-bold text-brand-400 uppercase tracking-widest">Active status for this intent</span>
                      </div>
                    </div>
                    <label className="relative inline-flex items-center cursor-pointer">
                      <input
                        type="checkbox"
                        className="sr-only peer"
                        checked={editingConfig.isEnabled}
                        onChange={(e) => setEditingConfig({ ...editingConfig, isEnabled: e.target.checked })}
                        disabled={isSaving}
                      />
                      <div className="w-11 h-6 bg-brand-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-brand-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
                    </label>
                  </div>
                </div>

                <div className="flex gap-4 mt-10">
                  <Button
                    variant="outline"
                    onClick={closeModal}
                    className="flex-1"
                    disabled={isSaving}
                  >
                    Discard
                  </Button>
                  <Button
                    variant="primary"
                    onClick={handleSave}
                    className="flex-1"
                    isLoading={isSaving}
                    leftIcon={!isSaving && <Save size={18} />}
                  >
                    Save Changes
                  </Button>
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
