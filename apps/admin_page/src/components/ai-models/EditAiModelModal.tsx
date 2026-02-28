import React from 'react';
import { motion } from 'framer-motion';
import { Cpu, X, Check, AlertCircle, Save } from 'lucide-react';
import Button from '../Button';
import type { AiModelConfig, AiModel, SortOption } from '../../services/api';

interface EditAiModelModalProps {
  editingConfig: AiModelConfig;
  isSaving: boolean;
  sortedModels: AiModel[];
  modelSort: SortOption;
  onModelSortChange: (sort: SortOption) => void;
  onClose: () => void;
  onSave: () => void;
  onChange: (config: AiModelConfig) => void;
}

const EditAiModelModal: React.FC<EditAiModelModalProps> = ({
  editingConfig,
  isSaving,
  sortedModels,
  modelSort,
  onModelSortChange,
  onClose,
  onSave,
  onChange,
}) => {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-6">
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
        className="absolute inset-0 bg-brand-900/60 backdrop-blur-md"
      />
      <motion.div
        initial={{ opacity: 0, scale: 0.9, y: 40 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.9, y: 40 }}
        className="relative w-full max-w-3xl bg-white rounded-[3rem] shadow-premium-xl overflow-hidden border border-white/20"
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
        <div className="p-10 md:p-14">
          <div className="flex items-center justify-between mb-12">
            <div className="flex items-center gap-6">
              <div className="w-20 h-20 bg-linear-to-br from-primary-500 to-primary-700 rounded-3xl shadow-xl shadow-primary-200/50 text-white flex items-center justify-center">
                <Cpu size={40} />
              </div>
              <div>
                <h2 id="modal-title" className="text-3xl font-black text-brand-900 tracking-tightest">
                  {editingConfig.id ? 'Edit Policy' : 'Provision Policy'}
                </h2>
                <p className="text-brand-400 font-bold text-lg">Define orchestrator routing logic.</p>
              </div>
            </div>
            <button
              onClick={onClose}
              aria-label="Close modal"
              className="w-12 h-12 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-brand-50 rounded-2xl transition-all duration-300"
            >
              <X size={32} aria-hidden="true" />
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
                  onChange={(e) => onChange({ ...editingConfig, intent: e.target.value })}
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
                    onChange={(e) => onModelSortChange(e.target.value as SortOption)}
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
                  onChange={(e) => onChange({ ...editingConfig, primaryModel: e.target.value })}
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
                    onChange={(e) => onChange({ ...editingConfig, fallbackModels: e.target.value.split(',').map(s => s.trim()).filter(s => s) })}
                    placeholder="node-a, node-b"
                  />
                  <select
                    aria-label="Add Fallback Model"
                    className="w-1/3 px-4 py-5 bg-brand-50 border border-brand-100 rounded-2xl font-black text-brand-900 text-xs outline-none hover:bg-white transition-colors cursor-pointer"
                    onChange={(e) => {
                      if (e.target.value) {
                        if (!editingConfig.fallbackModels.includes(e.target.value)) {
                          const newFallbacks = [...editingConfig.fallbackModels, e.target.value];
                          onChange({ ...editingConfig, fallbackModels: newFallbacks });
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
                  onClick={() => onChange({ ...editingConfig, isEnabled: !editingConfig.isEnabled })}
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
              onChange={(e) => onChange({ ...editingConfig, description: e.target.value })}
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
                onClick={onClose}
                variant="ghost"
                className="text-brand-400 font-black px-8"
              >
                Discard
              </Button>
              <Button
                onClick={onSave}
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
  );
};

export default EditAiModelModal;
