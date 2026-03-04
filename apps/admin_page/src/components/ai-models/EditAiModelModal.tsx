import React, { useState, useMemo } from 'react';
import { motion } from 'framer-motion';
import { Cpu, X, Check, AlertCircle, Save } from 'lucide-react';
import Button from '../Button';
import ConfirmationDialog from '../ConfirmationDialog';
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
  const [initialConfig] = useState<AiModelConfig>(() => ({ ...editingConfig }));
  const [showDiscardConfirmation, setShowDiscardConfirmation] = useState(false);

  const isDirty = useMemo(() => {
    return JSON.stringify(initialConfig) !== JSON.stringify(editingConfig);
  }, [initialConfig, editingConfig]);

  const handleCloseAttempt = () => {
    if (isSaving) return;
    if (isDirty) {
      setShowDiscardConfirmation(true);
    } else {
      onClose();
    }
  };

  return (
    <>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-6">
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={handleCloseAttempt}
          className="absolute inset-0 bg-brand-900/60 backdrop-blur-md"
        />
        <motion.div
          initial={{ opacity: 0, scale: 0.9, y: 40 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.9, y: 40 }}
          transition={{ type: "spring", stiffness: 300, damping: 30 }}
          className="relative w-full max-w-3xl bg-white rounded-5xl shadow-premium-xl overflow-hidden border border-white/20 flex flex-col max-h-[90vh]"
          role="dialog"
          aria-modal="true"
          aria-labelledby="modal-title"
        >
          <div className="p-10 md:p-14 overflow-y-auto custom-scrollbar pb-32">
            <div className="flex items-center justify-between mb-12 sticky top-0 bg-white/80 backdrop-blur-md z-10 py-2 -mt-2">
              <div className="flex items-center gap-6">
                <div className="w-20 h-20 bg-linear-to-br from-primary-500 to-primary-700 rounded-3xl shadow-xl shadow-primary-200/50 text-white flex items-center justify-center transition-transform hover:rotate-6 duration-500">
                  <Cpu size={40} />
                </div>
                <div>
                  <h2 id="modal-title" className="text-3xl font-black text-brand-900 tracking-tightest">
                    {editingConfig.id ? 'Edit Feature' : 'Configure Feature'}
                  </h2>
                  <p className="text-brand-400 font-bold text-lg">Configure LLM parameters.</p>
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
                  <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Feature Key</label>
                  <input
                    type="text"
                    className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 disabled:opacity-50"
                    value={editingConfig.feature}
                    onChange={(e) => onChange({ ...editingConfig, feature: e.target.value })}
                    disabled={!!editingConfig.id}
                    placeholder="e.g. quick_summary"
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
                <div className="flex gap-4">
                  <div className="flex-1">
                    <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Temperature</label>
                    <input
                      type="number"
                      step="0.1"
                      min="0"
                      max="2"
                      className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 font-mono"
                      value={editingConfig.temperature ?? ''}
                      onChange={(e) => onChange({ ...editingConfig, temperature: e.target.value ? parseFloat(e.target.value) : undefined })}
                      placeholder="e.g. 0.7"
                    />
                  </div>
                  <div className="flex-1">
                    <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Max Tokens</label>
                    <input
                      type="number"
                      step="1"
                      min="1"
                      className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 font-mono"
                      value={editingConfig.maxTokens ?? ''}
                      onChange={(e) => onChange({ ...editingConfig, maxTokens: e.target.value ? parseInt(e.target.value, 10) : undefined })}
                      placeholder="e.g. 2048"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Operational State</label>
                  <button
                    onClick={() => onChange({ ...editingConfig, isEnabled: !editingConfig.isEnabled })}
                    className={`w-full flex items-center justify-between px-8 py-5 rounded-2xl border transition-all duration-500 group/toggle ${
                      editingConfig.isEnabled
                        ? 'bg-success-50 border-success-200 text-success-700 shadow-premium shadow-success-100/30'
                        : 'bg-brand-50 border-brand-100 text-brand-300'
                    }`}
                  >
                    <span className="font-black uppercase tracking-widest">{editingConfig.isEnabled ? 'Active' : 'Deactivated'}</span>
                    <div className={`p-1 rounded-lg transition-all duration-500 ${editingConfig.isEnabled ? 'bg-success-100 text-success-600' : 'bg-brand-100 text-brand-300'}`}>
                        {editingConfig.isEnabled ? <Check size={20} /> : <X size={20} />}
                    </div>
                  </button>
                </div>
              </div>
            </div>

            <div className="mt-10">
              <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">System Prompt Override</label>
              <textarea
                className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 min-h-[160px] font-mono text-sm placeholder:text-brand-200"
                value={editingConfig.systemPrompt || ''}
                onChange={(e) => onChange({ ...editingConfig, systemPrompt: e.target.value })}
                placeholder="Optional: Provide a custom system prompt to override the default..."
              />
            </div>

            <div className="mt-10">
              <label className="block text-[10px] font-black text-brand-300 uppercase tracking-[0.3em] mb-3 ml-1">Configuration Justification</label>
              <textarea
                className="w-full px-6 py-5 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 min-h-[120px] placeholder:text-brand-200"
                value={editingConfig.description}
                onChange={(e) => onChange({ ...editingConfig, description: e.target.value })}
                placeholder="Provide context for this configuration..."
              />
            </div>
          </div>

          <div className="absolute bottom-0 left-0 right-0 p-10 md:px-14 md:py-8 bg-white/80 backdrop-blur-xl border-t border-brand-100 flex flex-col sm:flex-row items-center justify-between gap-8 z-20">
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
                Save Configuration
              </Button>
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
        message="You have unsaved modifications to this AI configuration. Discarding will permanently lose all current progress."
        confirmLabel="Discard Changes"
        isDestructive
      />
    </>
  );
};

export default EditAiModelModal;
