import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface AuditoriaItem {
  fecha: string;
  usuario: string;
  modulo: string;
  accion: string;
  registro: string;
  resultado: string;
  maquina: string;
  observaciones?: string;
}

export interface AuditoriaPageResult {
  items: AuditoriaItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AuditoriaFiltro {
  fechaDesde?: string;
  fechaHasta?: string;
  usuario?: string;
  modulo?: string;
  accion?: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class AuditoriaService {
  private readonly baseUrl = `${environment.apiBaseUrl}/Auditoria`;

  constructor(private http: HttpClient) {}

  getBitacora(filtro: AuditoriaFiltro): Observable<AuditoriaPageResult> {
    let params = new HttpParams()
      .set('page', filtro.page)
      .set('pageSize', filtro.pageSize);

    if (filtro.fechaDesde) params = params.set('fechaDesde', filtro.fechaDesde);
    if (filtro.fechaHasta) params = params.set('fechaHasta', filtro.fechaHasta);
    if (filtro.usuario) params = params.set('usuario', filtro.usuario);
    if (filtro.modulo) params = params.set('modulo', filtro.modulo);
    if (filtro.accion) params = params.set('accion', filtro.accion);

    return this.http.get<AuditoriaPageResult>(`${this.baseUrl}/bitacora`, { params });
  }
}

