import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface HomeStats {
  usuariosActivos: number;
  reportesGenerados: number;
  alertasSistema: number;
}

export interface DtoSliderPublico {
  id: number;
  titulo?: string;
  descripcion?: string;
  imagenUrl?: string;
  redireccionUrl?: string;
  orden: number;
  activo: boolean;
}

export interface DtoModalVigente {
  idModal: number;
  titulo?: string;
  subtitulo?: string;
  imagenUrl?: string;
  redireccionUrl?: string;
  orden: number;
  vigente: number;
}

@Injectable({ providedIn: 'root' })
export class HomeService {
  private readonly baseUrl = `${environment.apiBaseUrl}/Home`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<HomeStats> {
    return this.http.get<HomeStats>(`${this.baseUrl}/stats`);
  }

  getSlidersPublicos(): Observable<DtoSliderPublico[]> {
    return this.http.get<DtoSliderPublico[]>(`${this.baseUrl}/sliders`);
  }

  getModalesVigentes(): Observable<DtoModalVigente[]> {
    return this.http.get<DtoModalVigente[]>(`${this.baseUrl}/modales`);
  }
}
