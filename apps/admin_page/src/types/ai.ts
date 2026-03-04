export interface AiModelConfig {
  id: string;
  feature: string;
  modelId: string;
  description: string;
  isEnabled: boolean;
  safetySettings?: string;
  systemPrompt?: string;
  temperature?: number;
  maxTokens?: number;
}

export interface UpdateAiModelConfigDto {
  feature: string;
  modelId: string;
  description: string;
  isEnabled: boolean;
  safetySettings?: string;
  systemPrompt?: string;
  temperature?: number;
  maxTokens?: number;
}
