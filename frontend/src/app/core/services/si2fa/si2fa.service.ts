import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// ================== Interfaces ==================
export interface Conteo { clave: string; total: number; }
export interface SerieTiempo { fecha: string; verificacionesOk: number; verificacionesFallidas: number; total: number; }

export interface Metricas2Fa {
  totalUsuarios: number;
  usuariosHabilitados: number;
  usuariosEnrolados: number;
  usuariosPendientesEnrolar: number;
  usuariosBloqueados: number;
  sistemasTotales: number;
  sistemasActivos: number;
  ventanaDias: number;
  verificacionesOk: number;
  verificacionesFallidas: number;
  enrolamientos: number;
  restablecimientos: number;
  dispositivosConfiablesActivos: number;
  actividadPorSistema: Conteo[];
  eventosPorTipo: Conteo[];
  serieDiaria: SerieTiempo[];
}

export interface Sistema2Fa {
  idSistema: number;
  sigla: string;
  nombre: string;
  descripcion?: string;
  habilitado: number;
  secretoEnmascarado?: string;
  creadoAt?: string;
  actualizadoAt?: string;
  rotadoAt?: string;
  usuarioCreacion?: string;
  usuarioModifica?: string;
}
export interface Sistema2FaRequest { sigla: string; nombre: string; descripcion?: string; habilitado: number; }
export interface Sistema2FaResult { success: boolean; message?: string; id: number; secreto?: string; }

export interface Usuario2Fa {
  idMfa: number;
  identificacion: number;
  usuarioInstitucional: string;
  mfaHabilitado: number;
  requireReenroll: number;
  enrolado: boolean;
  intentosFallidos: number;
  bloqueoHasta?: string;
  bloqueado: boolean;
  ultimoOk?: string;
  dispositivosConfiables: number;
  creadoAt?: string;
  actualizadoAt?: string;
}
export interface Dispositivo2Fa {
  idDevice: number;
  idMfa: number;
  deviceHash?: string;
  expiraAt?: string;
  vigente: boolean;
  creadoAt?: string;
  identificacion?: number;
  usuarioInstitucional?: string;
}
export interface Usuario2FaDetalle extends Usuario2Fa { dispositivos: Dispositivo2Fa[]; }
export interface Accion2FaResult { success: boolean; message?: string; }

export interface Auditoria2Fa {
  idAuditoria: number;
  identificacion?: number;
  usuarioInstitucional?: string;
  sistema?: string;
  evento?: string;
  detalle?: string;
  ipOrigen?: string;
  creadoAt?: string;
}
export interface Auditoria2FaPagina {
  total: number;
  pagina: number;
  tamanoPagina: number;
  items: Auditoria2Fa[];
}
export interface Auditoria2FaFiltro {
  identificacion?: number;
  usuario?: string;
  sistema?: string;
  evento?: string;
  desde?: string;
  hasta?: string;
  pagina?: number;
  tamano?: number;
}

export interface Parametro2Fa {
  clave: string;
  valor: string;
  tipo: string;
  descripcion?: string;
  unidad?: string;
  grupo?: string;
  minValor?: number;
  maxValor?: number;
  actualizadoAt?: string;
  usuarioModifica?: string;
}
export interface Parametro2FaResult { success: boolean; message?: string; }

export interface Salud2Fa {
  up: boolean;
  estado: string;      // OPERATIVO | CAIDO
  latenciaMs: number;
  mensaje?: string;
  consultado: string;
}

@Injectable({ providedIn: 'root' })
export class Si2faService {
  private readonly base = `${environment.apiBaseUrl}/admin`;

  constructor(private http: HttpClient) {}

  // -------- Dashboard --------
  getMetricas(dias = 30): Observable<Metricas2Fa> {
    return this.http.get<Metricas2Fa>(`${this.base}/dashboard/metricas`, { params: { dias } });
  }

  getSalud(): Observable<Salud2Fa> {
    return this.http.get<Salud2Fa>(`${this.base}/dashboard/salud`);
  }

  // -------- Sistemas --------
  getSistemas(): Observable<Sistema2Fa[]> {
    return this.http.get<Sistema2Fa[]>(`${this.base}/sistemas`);
  }
  getSistema(id: number): Observable<Sistema2Fa> {
    return this.http.get<Sistema2Fa>(`${this.base}/sistemas/${id}`);
  }
  crearSistema(req: Sistema2FaRequest): Observable<Sistema2FaResult> {
    return this.http.post<Sistema2FaResult>(`${this.base}/sistemas`, req);
  }
  actualizarSistema(id: number, req: Sistema2FaRequest): Observable<Sistema2FaResult> {
    return this.http.put<Sistema2FaResult>(`${this.base}/sistemas/${id}`, req);
  }
  cambiarEstadoSistema(id: number, habilitado: number): Observable<Sistema2FaResult> {
    return this.http.put<Sistema2FaResult>(`${this.base}/sistemas/${id}/estado`, { sigla: '', nombre: 'x', habilitado });
  }
  rotarSecreto(id: number): Observable<Sistema2FaResult> {
    return this.http.post<Sistema2FaResult>(`${this.base}/sistemas/${id}/rotar-secreto`, {});
  }

  // -------- Usuarios --------
  getUsuarios(filtro?: string, pagina = 1, tamano = 50): Observable<Usuario2Fa[]> {
    let params = new HttpParams().set('pagina', pagina).set('tamano', tamano);
    if (filtro) params = params.set('filtro', filtro);
    return this.http.get<Usuario2Fa[]>(`${this.base}/usuarios2fa`, { params });
  }
  getUsuario(idMfa: number): Observable<Usuario2FaDetalle> {
    return this.http.get<Usuario2FaDetalle>(`${this.base}/usuarios2fa/${idMfa}`);
  }
  desbloquear(idMfa: number): Observable<Accion2FaResult> {
    return this.http.post<Accion2FaResult>(`${this.base}/usuarios2fa/${idMfa}/desbloquear`, {});
  }
  cambiarEstadoMfa(idMfa: number, estado: number): Observable<Accion2FaResult> {
    return this.http.post<Accion2FaResult>(`${this.base}/usuarios2fa/${idMfa}/estado`, { estado });
  }
  forzarReenrolamiento(idMfa: number): Observable<Accion2FaResult> {
    return this.http.post<Accion2FaResult>(`${this.base}/usuarios2fa/${idMfa}/forzar-reenrolamiento`, {});
  }
  restablecer(idMfa: number): Observable<Accion2FaResult> {
    return this.http.post<Accion2FaResult>(`${this.base}/usuarios2fa/${idMfa}/restablecer`, {});
  }
  limpiarDispositivos(idMfa: number): Observable<Accion2FaResult> {
    return this.http.post<Accion2FaResult>(`${this.base}/usuarios2fa/${idMfa}/limpiar-dispositivos`, {});
  }

  // -------- Dispositivos --------
  getDispositivos(idMfa?: number, soloVigentes = true): Observable<Dispositivo2Fa[]> {
    let params = new HttpParams().set('soloVigentes', soloVigentes);
    if (idMfa != null) params = params.set('idMfa', idMfa);
    return this.http.get<Dispositivo2Fa[]>(`${this.base}/dispositivos`, { params });
  }
  revocarDispositivo(idDevice: number): Observable<Accion2FaResult> {
    return this.http.delete<Accion2FaResult>(`${this.base}/dispositivos/${idDevice}`);
  }

  // -------- Auditoría --------
  getAuditoria(f: Auditoria2FaFiltro): Observable<Auditoria2FaPagina> {
    return this.http.get<Auditoria2FaPagina>(`${this.base}/auditoria2fa`, { params: this.toParams(f) });
  }
  exportarAuditoria(f: Auditoria2FaFiltro): Observable<Blob> {
    return this.http.get(`${this.base}/auditoria2fa/exportar`, { params: this.toParams(f), responseType: 'blob' });
  }

  // -------- Parámetros --------
  getParametros(): Observable<Parametro2Fa[]> {
    return this.http.get<Parametro2Fa[]>(`${this.base}/parametros`);
  }
  actualizarParametro(clave: string, valor: string): Observable<Parametro2FaResult> {
    return this.http.put<Parametro2FaResult>(`${this.base}/parametros/${encodeURIComponent(clave)}`, { valor });
  }

  private toParams(f: Auditoria2FaFiltro): HttpParams {
    let params = new HttpParams();
    if (f.identificacion != null) params = params.set('identificacion', f.identificacion);
    if (f.usuario) params = params.set('usuario', f.usuario);
    if (f.sistema) params = params.set('sistema', f.sistema);
    if (f.evento) params = params.set('evento', f.evento);
    if (f.desde) params = params.set('desde', f.desde);
    if (f.hasta) params = params.set('hasta', f.hasta);
    if (f.pagina != null) params = params.set('pagina', f.pagina);
    if (f.tamano != null) params = params.set('tamano', f.tamano);
    return params;
  }
}
