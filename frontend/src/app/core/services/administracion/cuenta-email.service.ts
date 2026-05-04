import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface DtoCuentaEmail {
  idCuenta: number;
  idDominioGrupo: number;
  nombreCuenta: string;
  email: string;
  servidorSmtp: string;
  puerto: number;
  usuarioSmtp: string;
  claveSmtp: string;
  usarSsl: number;
  vigente: number;
  // Opcional: Nombre del grupo para la tabla
  nombreGrupo?: string;
}

export interface DtoCuentaEmailRequest {
  IdDominioGrupo: number;
  NombreCuenta: string;
  Email: string;
  ServidorSmtp: string;
  Puerto: number;
  UsuarioSmtp: string;
  ClaveSmtp: string;
  UsarSsl: number;
  Vigente: number;
}

@Injectable({
  providedIn: 'root'
})
export class CuentaEmailService {
  private baseUrl = `${environment.apiBaseUrl}/CuentaEmail`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<DtoCuentaEmail[]> {
    return this.http.get<DtoCuentaEmail[]>(this.baseUrl);
  }

  getById(id: number): Observable<DtoCuentaEmail> {
    return this.http.get<DtoCuentaEmail>(`${this.baseUrl}/${id}`);
  }

  create(request: DtoCuentaEmailRequest): Observable<any> {
    return this.http.post(this.baseUrl, request);
  }

  update(id: number, request: DtoCuentaEmailRequest): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }
}
