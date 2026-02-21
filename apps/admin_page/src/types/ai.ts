export interface AiModelConfig {
  id: string;
  intent: string;
  primaryModel: string;
  fallbackModels: string[];
  description: string;
  isEnabled: boolean;
  safetySettings?: string;
}

export interface UpdateAiModelConfigDto {
  intent: string;
  primaryModel: string;
  fallbackModels: string[];
  description: string;
  isEnabled: boolean;
  safetySettings?: string;
}
