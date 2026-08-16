import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AiConnectionOptions } from '../models/ai-provider';
import { TailoredResult } from '../models/tailored-result';

@Injectable({ providedIn: 'root' })
export class ResumeApiService {
  private readonly http = inject(HttpClient);

  tailorResume(
    resume: File,
    jobDescription: string,
    connection: AiConnectionOptions
  ): Observable<TailoredResult> {
    const formData = new FormData();
    formData.append('resume', resume, resume.name);
    formData.append('jobDescription', jobDescription);
    formData.append('provider', connection.provider);
    formData.append('model', connection.model);

    if (connection.customBaseUrl) {
      formData.append('customBaseUrl', connection.customBaseUrl);
    }

    const headers = new HttpHeaders({
      'X-AI-Key': connection.apiKey
    });

    return this.http.post<TailoredResult>('/api/tailor', formData, { headers });
  }
}
