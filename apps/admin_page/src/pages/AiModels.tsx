import React, { useEffect, useState } from 'react';
import { aiService, type AiModelConfig } from '../services/api';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import ModelSelector from '../components/ModelSelector';

const AiModels: React.FC = () => {
  const [configs, setConfigs] = useState<AiModelConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingConfig, setEditingConfig] = useState<AiModelConfig | null>(null);
  const [selectingModelFor, setSelectingModelFor] = useState<'primary' | 'fallback' | null>(null);

  useEffect(() => {
    loadConfigs();
  }, []);

  const loadConfigs = async () => {
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

  const handleSave = async () => {
    if (!editingConfig || !editingConfig.intent) { showToast("Intent is required", "error"); return; }
    try {
      await aiService.updateConfig(editingConfig.intent, editingConfig);
      showToast('Config saved', 'success');
      setEditingConfig(null);
      loadConfigs();
    } catch (error) {
       console.error(error);
       showToast('Failed to save config', 'error');
    }
  };

  const handleModelSelect = (modelId: string) => {
    if (editingConfig && selectingModelFor === 'primary') {
        setEditingConfig({ ...editingConfig, primaryModel: modelId });
    } else if (editingConfig && selectingModelFor === 'fallback') {
        // Prevent duplicates
        if (!editingConfig.fallbackModels.includes(modelId)) {
            setEditingConfig({
                ...editingConfig,
                fallbackModels: [...editingConfig.fallbackModels, modelId]
            });
        }
    }
    setSelectingModelFor(null);
  };

  if (loading) return <div className="p-6">Loading...</div>;

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-bold text-gray-800">AI Model Configurations</h1>
          <Button onClick={() => {
              // Creating a new config template
              setEditingConfig({
                  id: '',
                  intent: '',
                  primaryModel: 'openai/gpt-4o-mini',
                  fallbackModels: [],
                  description: '',
                  isEnabled: true,
                  safetySettings: ''
              });
          }}>Add Configuration</Button>
      </div>

      <div className="overflow-x-auto bg-white rounded-lg shadow">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Intent</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Primary Model</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Fallbacks</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {configs.length === 0 ? (
                <tr>
                    <td colSpan={5} className="px-6 py-4 text-center text-gray-500">No configurations found. Add one to override defaults.</td>
                </tr>
            ) : configs.map((config) => (
              <tr key={config.id || config.intent}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{config.intent}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{config.primaryModel}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{config.fallbackModels.join(', ')}</td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${config.isEnabled ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                    {config.isEnabled ? 'Enabled' : 'Disabled'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  <Button onClick={() => handleEdit(config)} variant="secondary" size="sm">Edit</Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {editingConfig && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full flex justify-center items-center z-50">
          <div className="bg-white p-8 rounded-lg shadow-xl w-full max-w-md max-h-[90vh] overflow-y-auto">
            <h2 className="text-xl font-bold mb-4">{editingConfig.id ? 'Edit' : 'Add'} Configuration</h2>

            <div className="mb-4">
              <label className="block text-gray-700 text-sm font-bold mb-2">Intent</label>
              <input
                type="text"
                className="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={editingConfig.intent}
                onChange={(e) => setEditingConfig({ ...editingConfig, intent: e.target.value })}
                disabled={!!editingConfig.id} // Disable editing intent for existing configs
                placeholder="e.g. quick_summary"
              />
            </div>

            <div className="mb-4">
              <label className="block text-gray-700 text-sm font-bold mb-2">Primary Model</label>
              <div className="flex gap-2">
                <input
                    type="text"
                    className="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500"
                    value={editingConfig.primaryModel}
                    onChange={(e) => setEditingConfig({ ...editingConfig, primaryModel: e.target.value })}
                    placeholder="openai/gpt-4o"
                />
                <Button onClick={() => setSelectingModelFor('primary')} variant="secondary" size="sm">Browse</Button>
              </div>
            </div>

            <div className="mb-4">
               <label className="block text-gray-700 text-sm font-bold mb-2">Fallback Models (comma separated)</label>
               <div className="flex gap-2">
                <input
                    type="text"
                    className="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500"
                    value={editingConfig.fallbackModels.join(', ')}
                    onChange={(e) => setEditingConfig({ ...editingConfig, fallbackModels: e.target.value.split(',').map(s => s.trim()).filter(s => s) })}
                    placeholder="openai/gpt-4o-mini, anthropic/claude-3-haiku"
                />
                <Button onClick={() => setSelectingModelFor('fallback')} variant="secondary" size="sm">Add</Button>
               </div>
            </div>

            <div className="mb-4">
              <label className="block text-gray-700 text-sm font-bold mb-2">Description</label>
              <textarea
                className="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={editingConfig.description}
                onChange={(e) => setEditingConfig({ ...editingConfig, description: e.target.value })}
              />
            </div>

            <div className="mb-6">
               <label className="flex items-center cursor-pointer">
                 <input
                   type="checkbox"
                   className="sr-only peer"
                   checked={editingConfig.isEnabled}
                   onChange={(e) => setEditingConfig({ ...editingConfig, isEnabled: e.target.checked })}
                 />
                 <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600 relative"></div>
                 <span className="ml-3 text-sm font-medium text-gray-900">Enabled</span>
               </label>
            </div>

            <div className="flex justify-end gap-3">
              <Button onClick={() => setEditingConfig(null)} variant="danger">Cancel</Button>
              <Button onClick={handleSave}>Save</Button>
            </div>
          </div>
        </div>
      )}

      {selectingModelFor && (
        <ModelSelector
            onSelect={handleModelSelect}
            onClose={() => setSelectingModelFor(null)}
        />
      )}
    </div>
  );
};

export default AiModels;
