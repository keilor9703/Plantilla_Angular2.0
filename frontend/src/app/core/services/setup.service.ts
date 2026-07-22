import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SetupStatus {
  configured: boolean;
  provider?: string;
  connectionStringConfigured: boolean;
}

export interface SetupTestRequest {
  provider: string;
  host?: string;
  port?: number;
  databaseOrService?: string;
  user?: string;
  password?: string;
  connectionStringRaw?: string;
  pipUser?: string;
  pipPassword?: string;
  jwtKey?: string;
  jwtIssuer?: string;
  jwtAudience?: string;
  sistema?: string;
  nombreSistema?: string;
  sigla?: string;
  nombreUnidad?: string;
  unidadSistemaId?: number;
  logoFileName?: string;
  faviconFileName?: string;
  fondo1FileName?: string;
}

export interface SetupResponse {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class SetupService {
  private apiBase = `${environment.apiBaseUrl}/setup`;

  constructor(private http: HttpClient) {}

  getStatus(): Observable<SetupStatus> {
    return this.http.get<SetupStatus>(`${this.apiBase}/status`);
  }

  getUnidades(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiBase}/unidades`);
  }

  testConnection(request: SetupTestRequest): Observable<SetupResponse> {
    return this.http.post<SetupResponse>(`${this.apiBase}/test-connection`, request);
  }

  saveConfig(request: SetupTestRequest): Observable<SetupResponse> {
    return this.http.post<SetupResponse>(`${this.apiBase}/save-config`, request);
  }

  uploadLogo(file: File): Observable<any> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<any>(`${environment.apiBaseUrl}/Branding/upload-logo`, fd);
  }

  uploadFavicon(file: File): Observable<any> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<any>(`${environment.apiBaseUrl}/Branding/upload-favicon`, fd);
  }
}
