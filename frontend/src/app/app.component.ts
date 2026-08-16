import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal
} from '@angular/core';
import { finalize } from 'rxjs';
import {
  AI_PROVIDERS,
  AiConnectionOptions,
  AiProviderDefinition,
  AiProviderId
} from './models/ai-provider';
import { ApiErrorResponse, TailoredResult } from './models/tailored-result';
import { DownloadService } from './services/download.service';
import { ResumeApiService } from './services/resume-api.service';

interface StoredAiSettings {
  provider?: AiProviderId;
  models?: Partial<Record<AiProviderId, string>>;
  customBaseUrl?: string;
}

type StoredApiKeys = Partial<Record<AiProviderId, string>>;

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
  private static readonly AiKeysStorageKey = 'resumeforge_ai_keys_v1';
  private static readonly AiSettingsStorageKey = 'resumeforge_ai_settings_v1';
  private static readonly LegacyOpenAiKeyStorageKey = 'resumeforge_openai_key';
  private static readonly MaxPdfSizeBytes = 10 * 1024 * 1024;

  private readonly resumeApi = inject(ResumeApiService);
  private readonly downloadService = inject(DownloadService);
  private readonly initialSettings = this.readStoredSettings();
  private readonly initialProvider = this.resolveProvider(this.initialSettings.provider);
  private apiKeys: StoredApiKeys = this.readStoredApiKeys();
  private modelOverrides: Partial<Record<AiProviderId, string>> = {
    ...this.initialSettings.models
  };

  readonly providers = AI_PROVIDERS;
  readonly selectedFile = signal<File | null>(null);
  readonly jobDescription = signal('');
  readonly provider = signal<AiProviderId>(this.initialProvider);
  readonly model = signal(
    this.modelOverrides[this.initialProvider] ??
      this.getProviderDefinition(this.initialProvider).defaultModel
  );
  readonly apiKey = signal(this.apiKeys[this.initialProvider] ?? '');
  readonly customBaseUrl = signal(this.initialSettings.customBaseUrl ?? '');
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly result = signal<TailoredResult | null>(null);
  readonly copyMessage = signal('');

  private copyMessageTimer: number | undefined;

  readonly selectedProvider = computed(() => this.getProviderDefinition(this.provider()));
  readonly isCustomProvider = computed(() => this.selectedProvider().customEndpoint);
  readonly customEndpointValid = computed(() => {
    if (!this.isCustomProvider()) {
      return true;
    }

    try {
      const url = new URL(this.customBaseUrl().trim());
      return url.protocol === 'https:' && url.hostname.length > 0;
    } catch {
      return false;
    }
  });

  readonly canSubmit = computed(
    () =>
      this.selectedFile() !== null &&
      this.jobDescription().trim().length >= 50 &&
      this.apiKey().trim().length > 0 &&
      this.model().trim().length > 0 &&
      this.customEndpointValid() &&
      !this.isLoading()
  );

  readonly prettyResult = computed(() => {
    const current = this.result();
    return current ? JSON.stringify(current, null, 2) : '';
  });

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.errorMessage.set('');
    this.result.set(null);

    if (!file) {
      this.selectedFile.set(null);
      return;
    }

    if (file.type !== 'application/pdf' || !file.name.toLowerCase().endsWith('.pdf')) {
      this.selectedFile.set(null);
      input.value = '';
      this.errorMessage.set('Only PDF files are supported.');
      return;
    }

    if (file.size > AppComponent.MaxPdfSizeBytes) {
      this.selectedFile.set(null);
      input.value = '';
      this.errorMessage.set('The PDF must be 10 MB or smaller.');
      return;
    }

    this.selectedFile.set(file);
  }

  onJobDescriptionInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.jobDescription.set(textarea.value);
  }

  onProviderChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const nextProvider = this.resolveProvider(select.value as AiProviderId);

    this.provider.set(nextProvider);
    this.model.set(
      this.modelOverrides[nextProvider] ?? this.getProviderDefinition(nextProvider).defaultModel
    );
    this.apiKey.set(this.apiKeys[nextProvider] ?? '');
    this.errorMessage.set('');
    this.result.set(null);
    this.persistSettings();
  }

  onModelInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value;

    this.model.set(value);
    this.modelOverrides[this.provider()] = value;
    this.persistSettings();
  }

  onApiKeyInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value;

    this.apiKey.set(value);

    if (value.trim()) {
      this.apiKeys[this.provider()] = value.trim();
    } else {
      delete this.apiKeys[this.provider()];
    }

    this.persistApiKeys();
  }

  onCustomBaseUrlInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.customBaseUrl.set(input.value);
    this.persistSettings();
  }

  submit(): void {
    const file = this.selectedFile();
    const jobDescription = this.jobDescription().trim();
    const apiKey = this.apiKey().trim();
    const model = this.model().trim();

    if (!file || jobDescription.length < 50 || !apiKey || !model || !this.customEndpointValid()) {
      this.errorMessage.set(
        'Add a PDF resume, a complete job description, a valid provider configuration, and an API key.'
      );
      return;
    }

    const connection: AiConnectionOptions = {
      provider: this.provider(),
      apiKey,
      model,
      customBaseUrl: this.isCustomProvider() ? this.customBaseUrl().trim() : undefined
    };

    this.errorMessage.set('');
    this.result.set(null);
    this.isLoading.set(true);

    this.resumeApi
      .tailorResume(file, jobDescription, connection)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (result) => this.result.set(result),
        error: (error: unknown) => this.errorMessage.set(this.getErrorMessage(error))
      });
  }

  async downloadResumeDocx(): Promise<void> {
    const current = this.result();
    if (!current) {
      return;
    }

    try {
      await this.downloadService.downloadResumeDocx(current);
    } catch {
      this.errorMessage.set('Could not create the resume DOCX file in this browser.');
    }
  }

  downloadResumePdf(): void {
    const current = this.result();
    if (!current) {
      return;
    }

    try {
      this.downloadService.downloadResumePdf(current);
    } catch {
      this.errorMessage.set('Could not create the resume PDF file in this browser.');
    }
  }

  async downloadCoverLetterDocx(): Promise<void> {
    const current = this.result();
    if (!current) {
      return;
    }

    try {
      await this.downloadService.downloadCoverLetterDocx(current);
    } catch {
      this.errorMessage.set('Could not create the cover letter DOCX file in this browser.');
    }
  }

  downloadCoverLetterPdf(): void {
    const current = this.result();
    if (!current) {
      return;
    }

    try {
      this.downloadService.downloadCoverLetterPdf(current);
    } catch {
      this.errorMessage.set('Could not create the cover letter PDF file in this browser.');
    }
  }

  copyResume(): void {
    const current = this.result();
    if (current) {
      void this.copyText(
        this.downloadService.buildResumePlainText(current.resume),
        'Full resume copied'
      );
    }
  }

  copySummary(): void {
    const current = this.result();
    if (current) {
      void this.copyText(current.resume.summary, 'Summary copied');
    }
  }

  copyExperienceBullets(): void {
    const current = this.result();
    if (!current) {
      return;
    }

    const bullets = current.resume.experience
      .flatMap((experience) => experience.bulletPoints)
      .filter((point) => point.trim().length > 0)
      .map((point) => `• ${point.trim()}`)
      .join('\n');

    void this.copyText(bullets, 'Experience bullets copied');
  }

  copyCoverLetter(): void {
    const current = this.result();
    if (current) {
      void this.copyText(
        this.downloadService.buildCoverLetterPlainText(
          current.resume,
          current.coverLetterDocument
        ),
        'Cover letter copied'
      );
    }
  }

  clearApiKey(): void {
    delete this.apiKeys[this.provider()];
    this.persistApiKeys();
    this.apiKey.set('');
  }

  private async copyText(text: string, successMessage: string): Promise<void> {
    if (!text.trim()) {
      return;
    }

    try {
      if (navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(text);
      } else {
        this.fallbackCopy(text);
      }

      this.showCopyMessage(successMessage);
    } catch {
      try {
        this.fallbackCopy(text);
        this.showCopyMessage(successMessage);
      } catch {
        this.errorMessage.set('Could not copy the text. Your browser may be blocking clipboard access.');
      }
    }
  }

  private fallbackCopy(text: string): void {
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.setAttribute('readonly', '');
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    document.body.appendChild(textarea);
    textarea.select();

    const copied = document.execCommand('copy');
    document.body.removeChild(textarea);

    if (!copied) {
      throw new Error('Clipboard copy failed.');
    }
  }

  private showCopyMessage(message: string): void {
    this.copyMessage.set(message);

    if (this.copyMessageTimer !== undefined) {
      window.clearTimeout(this.copyMessageTimer);
    }

    this.copyMessageTimer = window.setTimeout(() => {
      this.copyMessage.set('');
      this.copyMessageTimer = undefined;
    }, 1800);
  }

  private getProviderDefinition(provider: AiProviderId): AiProviderDefinition {
    return AI_PROVIDERS.find((candidate) => candidate.id === provider) ?? AI_PROVIDERS[0];
  }

  private resolveProvider(provider: AiProviderId | undefined): AiProviderId {
    return AI_PROVIDERS.some((candidate) => candidate.id === provider) ? provider! : 'openai';
  }

  private readStoredSettings(): StoredAiSettings {
    try {
      const raw = localStorage.getItem(AppComponent.AiSettingsStorageKey);
      return raw ? (JSON.parse(raw) as StoredAiSettings) : {};
    } catch {
      return {};
    }
  }

  private readStoredApiKeys(): StoredApiKeys {
    try {
      const raw = localStorage.getItem(AppComponent.AiKeysStorageKey);
      const keys = raw ? (JSON.parse(raw) as StoredApiKeys) : {};
      const legacyOpenAiKey = localStorage.getItem(AppComponent.LegacyOpenAiKeyStorageKey);

      if (!keys.openai && legacyOpenAiKey?.trim()) {
        keys.openai = legacyOpenAiKey.trim();
        localStorage.setItem(AppComponent.AiKeysStorageKey, JSON.stringify(keys));
        localStorage.removeItem(AppComponent.LegacyOpenAiKeyStorageKey);
      }

      return keys;
    } catch {
      return {};
    }
  }

  private persistApiKeys(): void {
    try {
      localStorage.setItem(AppComponent.AiKeysStorageKey, JSON.stringify(this.apiKeys));
    } catch {
      this.errorMessage.set('The browser could not save the API key locally.');
    }
  }

  private persistSettings(): void {
    try {
      const settings: StoredAiSettings = {
        provider: this.provider(),
        models: this.modelOverrides,
        customBaseUrl: this.customBaseUrl().trim()
      };

      localStorage.setItem(AppComponent.AiSettingsStorageKey, JSON.stringify(settings));
    } catch {
      this.errorMessage.set('The browser could not save the AI provider settings locally.');
    }
  }

  private getErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'The request failed unexpectedly.';
    }

    const apiError = error.error as Partial<ApiErrorResponse> | string | null;
    if (apiError && typeof apiError === 'object' && typeof apiError.message === 'string') {
      return apiError.message;
    }

    if (typeof apiError === 'string' && apiError.trim()) {
      return apiError;
    }

    if (error.status === 0) {
      return 'Cannot reach the ResumeForge API. Check that the backend is running.';
    }

    return `Request failed with HTTP ${error.status}.`;
  }
}
