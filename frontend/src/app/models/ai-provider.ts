export type AiProviderId =
  | 'openai'
  | 'anthropic'
  | 'gemini'
  | 'xai'
  | 'groq'
  | 'deepseek'
  | 'mistral'
  | 'openrouter'
  | 'custom';

export interface AiProviderDefinition {
  id: AiProviderId;
  label: string;
  defaultModel: string;
  keyPlaceholder: string;
  description: string;
  customEndpoint: boolean;
}

export interface AiConnectionOptions {
  provider: AiProviderId;
  apiKey: string;
  model: string;
  customBaseUrl?: string;
}

export const AI_PROVIDERS: readonly AiProviderDefinition[] = [
  {
    id: 'openai',
    label: 'OpenAI',
    defaultModel: 'gpt-4o-mini',
    keyPlaceholder: 'sk-...',
    description: 'Direct OpenAI API.',
    customEndpoint: false
  },
  {
    id: 'anthropic',
    label: 'Anthropic / Claude',
    defaultModel: 'claude-sonnet-5',
    keyPlaceholder: 'sk-ant-...',
    description: 'Direct Claude Messages API.',
    customEndpoint: false
  },
  {
    id: 'gemini',
    label: 'Google Gemini',
    defaultModel: 'gemini-3.7-flash',
    keyPlaceholder: 'Gemini API key',
    description: 'Direct Google Gemini API.',
    customEndpoint: false
  },
  {
    id: 'xai',
    label: 'xAI / Grok',
    defaultModel: 'grok-4.6',
    keyPlaceholder: 'xAI API key',
    description: 'Direct xAI API.',
    customEndpoint: false
  },
  {
    id: 'groq',
    label: 'Groq',
    defaultModel: 'llama-3.3-70b-versatile',
    keyPlaceholder: 'gsk_...',
    description: 'Groq-hosted OpenAI-compatible models.',
    customEndpoint: false
  },
  {
    id: 'deepseek',
    label: 'DeepSeek',
    defaultModel: 'deepseek-v4-pro',
    keyPlaceholder: 'DeepSeek API key',
    description: 'Direct DeepSeek API.',
    customEndpoint: false
  },
  {
    id: 'mistral',
    label: 'Mistral AI',
    defaultModel: 'mistral-small-latest',
    keyPlaceholder: 'Mistral API key',
    description: 'Direct Mistral API.',
    customEndpoint: false
  },
  {
    id: 'openrouter',
    label: 'OpenRouter',
    defaultModel: 'openai/gpt-4o-mini',
    keyPlaceholder: 'sk-or-v1-...',
    description: 'One key for models from many providers.',
    customEndpoint: false
  },
  {
    id: 'custom',
    label: 'Other (OpenAI-compatible)',
    defaultModel: '',
    keyPlaceholder: 'Provider API key',
    description: 'A public HTTPS endpoint exposing an OpenAI-compatible chat/completions API.',
    customEndpoint: true
  }
] as const;
