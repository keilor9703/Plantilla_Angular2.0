import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface DtoEvento {
  idEvento: number;
  unidad: string;
  titulo: string;
  subtitulo?: string | null;
  descripcion?: string | null;
  lugar?: string | null;
  ciudad?: string | null;
  fechaInicio: string;
  fechaFin: string;
  horaInicio?: string | null;
  horaFin?: string | null;
  imagenPortada?: string | null;
  enlaceEvento?: string | null;
  tipoEvento?: string | null;
  orden: number;
  vigente: number;
  fechaCreacion: string;
  usuarioCreacion: string;
  maquinaCreacion?: string | null;
  fechaModifica?: string | null;
  usuarioModifica?: string | null;
  maquinaModifica?: string | null;
}

export interface DtoEventoRequest {
  unidad: string;
  titulo: string;
  subtitulo?: string | null;
  descripcion?: string | null;
  lugar?: string | null;
  ciudad?: string | null;
  fechaInicio: string;
  fechaFin: string;
  horaInicio?: string | null;
  horaFin?: string | null;
  imagenPortada?: string | null;
  enlaceEvento?: string | null;
  tipoEvento?: string | null;
  orden: number;
  vigente: number;
}

export interface DtoEventoApiResponse {
  success: boolean;
  message: string;
  id?: number;
}

@Injectable({ providedIn: 'root' })
export class EventoService {
  private readonly baseUrl = `${environment.apiBaseUrl}/Evento`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<DtoEvento[]> {
    return this.http.get<DtoEvento[]>(this.baseUrl);
  }

  getActivos(fecha?: string): Observable<DtoEvento[]> {
    let params = new HttpParams();
    if (fecha?.trim()) {
      params = params.set('fecha', fecha.trim());
    }

    return this.http.get<DtoEvento[]>(`${this.baseUrl}/activos`, { params });
  }

  getById(id: number): Observable<DtoEvento> {
    return this.http.get<DtoEvento>(`${this.baseUrl}/${id}`);
  }

  create(payload: DtoEventoRequest): Observable<DtoEventoApiResponse> {
    return this.http.post<DtoEventoApiResponse>(this.baseUrl, payload);
  }

  update(id: number, payload: DtoEventoRequest): Observable<DtoEventoApiResponse> {
    return this.http.put<DtoEventoApiResponse>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number): Observable<DtoEventoApiResponse> {
    return this.http.delete<DtoEventoApiResponse>(`${this.baseUrl}/${id}`);
  }
}
