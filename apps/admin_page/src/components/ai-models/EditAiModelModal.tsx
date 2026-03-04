import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Cpu, X, Save, AlertCircle } from 'lucide-react';
import Button from '../Button';
import type { AiModelConfig, AiModel, SortOption } from '../../services/api';
import ConfirmationDialog from '../ConfirmationDialog';

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
  onChange
}) => {
  const [showDiscardConfirmation, setShowDiscardConfirmation] = useState(false);
  const [initialConfig] = useState<AiModelConfig>(() => ({ ...editingConfig }));

  const hasUnsavedChanges = JSON.stringify(initialConfig) !== JSON.stringify(editingConfig);

  const handleCloseAttempt = () => {
    if (isSaving) return;
    if (hasUnsavedChanges) {
      setShowDiscardConfirmation(true);
    } else {
      onClose();
    }
  };

  return (
    <>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.3 }}
          className="absolute inset-0 bg-brand-900/40 backdrop-blur-sm"
          onClick={handleCloseAttempt}
        />

        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          transition={{ type: 'spring', stiffness: 260, damping: 30 }}
          className="relative w-full max-w-3xl bg-white rounded-5xl shadow-premium-xl overflow-hidden border border-white/20 flex flex-col max-h-[90vh]"
          role="dialog"
          aria-modal="true"
          aria-labelledby="modal-title"
        >
          <div className="p-10 md:p-14 pb-32 overflow-y-auto custom-scrollbar">
            <div className="flex items-center justify-between mb-12 sticky top-0 bg-white/80 backdrop-blur-md z-10 py-2 -mt-2">
              <div className="flex items-center gap-6">
                <div className="w-20 h-20 bg-linear-to-br from-primary-500 to-primary-700 rounded-3xl shadow-xl shadow-primary-200/50 text-white flex items-center justify-center transition-transform hover:rotate-6 duration-500">
                  <Cpu size={40} />
                </div>
                <div>
                  <h2 id="modal-title" className="text-3xl font-black text-brand-900 tracking-tightest">
                    {editingConfig.id ? 'Edit Configuration' : 'Provision Configuration'}
                  </h2>
                  <p className="text-brand-400 font-bold text-lg">Define LLM settings.</p>
                </div>
              </div>
              <button
                onClick={handleCloseAttempt}
                aria-label="Close modal"
                className="w-12 h-12 flex items-center justify-center text-brand-300 hover:text-brand-900 hover:bg-brand-50 rounded-2xl transition-all duration-300 disabled:opacity-30 disabled:cursor-not-allowed"
                disabled={isSaving}
              >
                <X size={32} aria-hidden="true" />
              </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
              <div className="space-y-8">
                <div>
                  <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Feature</label>
                  <input
                    type="text"
                    className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 disabled:opacity-50"
                    value={editingConfig.feature}
                    onChange={(e) => onChange({ ...editingConfig, feature: e.target.value })}
                    disabled={!!editingConfig.id}
                    placeholder="e.g. chat"
                  />
                </div>

                <div>
                  <div className="flex justify-between items-center mb-3">
                    <label className="text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] ml-1">Model</label>
                    <select
                      aria-label="Sort models"
                      value={modelSort}
                      onChange={(e) => onModelSortChange(e.target.value as SortOption)}
                      className="bg-brand-50 border border-brand-100 rounded-lg text-[10px] font-black text-brand-400 px-2 py-1 outline-none focus:border-primary-500 uppercase tracking-widest cursor-pointer"
                    >
                      <option value="name">Alpha</option>
                      <option value="price_asc">Price Asc</option>
                      <option value="price_desc">Price Desc</option>
                    </select>
                  </div>
                  <select
                    aria-label="Model"
                    className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 font-mono appearance-none cursor-pointer"
                    value={editingConfig.modelId}
                    onChange={(e) => onChange({ ...editingConfig, modelId: e.target.value })}
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
                  <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Temperature ({editingConfig.temperature})</label>
                  <input
                    type="range"
                    min="0"
                    max="2"
                    step="0.1"
                    className="w-full h-2 bg-brand-100 rounded-lg appearance-none cursor-pointer accent-primary-600"
                    value={editingConfig.temperature}
                    onChange={(e) => onChange({ ...editingConfig, temperature: parseFloat(e.target.value) })}
                  />
                </div>

                <div>
                  <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Max Tokens</label>
                  <input
                    type="number"
                    min="1"
                    max="100000"
                    className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 font-mono"
                    value={editingConfig.maxTokens}
                    onChange={(e) => onChange({ ...editingConfig, maxTokens: parseInt(e.target.value) || 2000 })}
                  />
                </div>
              </div>
            </div>

            <div className="mt-10">
              <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">System Prompt (Optional Base Override)</label>
              <textarea
                className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 min-h-[120px] placeholder:text-brand-200"
                value={editingConfig.systemPrompt}
                onChange={(e) => onChange({ ...editingConfig, systemPrompt: e.target.value })}
                placeholder="Provide a custom system prompt override..."
              />
            </div>

            <div className="mt-14 flex flex-col sm:flex-row items-center justify-between gap-8 sticky bottom-0 bg-white/80 backdrop-blur-md py-4 -mb-4">
              <div className="flex items-center gap-3 text-warning-600 bg-warning-50 px-5 py-2.5 rounded-full border border-warning-100/50">
                <AlertCircle size={20} />
                <span className="text-[10px] font-black uppercase tracking-widest">Immediate Propagation</span>
              </div>
              <div className="flex gap-5 w-full sm:w-auto">
                <Button
                  onClick={handleCloseAttempt}
                  variant="ghost"
                  className="text-brand-400 font-black px-8"
                  disabled={isSaving}
                >
                  Discard
                </Button>
                <Button
                  onClick={onSave}
                  isLoading={isSaving}
                  leftIcon={<Save size={20} />}
                  className="shadow-premium shadow-primary-200/50 px-10"
                >
                  Commit Config
                </Button>
              </div>
            </div>
          </div>
        </motion.div>
      </div>

      <ConfirmationDialog
        isOpen={showDiscardConfirmation}
        onClose={() => setShowDiscardConfirmation(false)}
        onConfirm={() => {
          setShowDiscardConfirmation(false);
          onClose();
        }}
        title="Discard Changes?"
        message="You have unsaved modifications to this configuration. Discarding will permanently lose all current progress."
        confirmLabel="Discard Changes"
        isDestructive
      />
    </>
  );
};

export default EditAiModelModal;
