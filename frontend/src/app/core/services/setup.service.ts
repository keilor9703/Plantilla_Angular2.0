import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SetupStatus {
  configured: boolean;
  provider?: string;
  connectionStringConfigured: boolean;
  pipUser?: string;
  nombreSistema?: string;
  siglaSistema?: string;
  databaseOrService?: string;
  devUser?: string;
  devIdentificacion?: string;
  devNombres?: string;
  devApellidos?: string;
  devGrad?: string;
}

export interface SetupTestRequest {
  provider: string;
  host?: string;
  port?: number;
  databaseOrService?: string;
  user?: string;
  password?: string;
  tablespaceData?: string;
  tablespaceIndexes?: string;
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
  devUser?: string;
  devPassword?: string;
  devIdentificacion?: string;
  devNombres?: string;
  devApellidos?: string;
  devEmail?: string;
  devGrad?: string;
}

export interface SetupResponse {
  success: boolean;
  message: string;
  requiresMigration?: boolean;
  errors?: string[];
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

  validateDevUser(request: SetupTestRequest): Observable<SetupResponse> {
    return this.http.post<SetupResponse>(`${this.apiBase}/validate-dev-user`, request);
  }

  confirmDevUser(request: SetupTestRequest): Observable<SetupResponse> {
    return this.http.post<SetupResponse>(`${this.apiBase}/confirm-dev-user`, request);
  }

  testPipCredentials(request: SetupTestRequest): Observable<SetupResponse> {
    return this.http.post<SetupResponse>(`${this.apiBase}/test-pip-credentials`, request);
  }

  migrateDatabase(request: SetupTestRequest): Observable<SetupResponse> {
    return this.http.post<SetupResponse>(`${this.apiBase}/migrate-database`, request);
  }

  saveConfig(request: SetupTestRequest): Observable<SetupResponse> {
    return this.http.post<SetupResponse>(`${this.apiBase}/save-config`, request);
  }

  shutdownBackend(): Observable<SetupResponse> {
    return this.http.post<SetupResponse>(`${this.apiBase}/shutdown`, {});
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
