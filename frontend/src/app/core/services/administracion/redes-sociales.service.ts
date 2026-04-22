import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface DtoRedesSociales {
  id: number;
  redSocial: string;
  unidadResponsable: string;
  nombreCuenta: string;
  urlCuenta: string;
  actaAsignacion: string;
  nombreResponsable: string;
  telResponsable: string;
  emailResponsable: string;
  emailCuenta: string;
  telAsociado: string;
  vigente: number;
  fechaCreacion?: string;
  usuarioCreacion?: string;
}

export interface DtoDominio {
  id?: number;
  idDominio?: number;
  idPadre?: number;
  descripcion: string;
}

export interface DtoUnidad {
  value: number;
  text: string;
  descripcion: string;
}

export interface DtoRedesSocialesRequest {
  redSocial: string | number;
  unidadResponsable: string | number;
  nombreCuenta: string;
  urlCuenta: string;
  actaAsignacion: string;
  nombreResponsable: string;
  telResponsable: string;
  emailResponsable: string;
  emailCuenta: string;
  telAsociado: string;
  vigente: number;
}

@Injectable({ providedIn: 'root' })
export class RedesSocialesService {
  private readonly baseUrl = `${environment.apiBaseUrl}/RedesSociales`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<DtoRedesSociales[]> {
    return this.http.get<DtoRedesSociales[]>(this.baseUrl);
  }

  getById(id: number): Observable<DtoRedesSociales> {
    return this.http.get<DtoRedesSociales>(`${this.baseUrl}/${id}`);
  }

  create(request: DtoRedesSocialesRequest): Observable<DtoRedesSociales> {
    return this.http.post<DtoRedesSociales>(this.baseUrl, request);
  }

  update(id: number, request: DtoRedesSocialesRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, { ...request, id });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getDominios(): Observable<DtoDominio[]> {
    return this.http.get<DtoDominio[]>(`${this.baseUrl}/dominios`);
  }

  getUnidades(): Observable<DtoUnidad[]> {
    return this.http.get<DtoUnidad[]>(`${this.baseUrl}/unidades`);
  }
}
