import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface DtoHechoNoticioso {
  noticiaId: string;
  idUnidad: number;
  unidadStr?: string;
  titular: string;
  fecha?: string;
  idCaracter?: number;
  caracterStr?: string;
  idTema?: number;
  temaStr?: string;
  idDescTema?: number;
  descTemaStr?: string;
  idCobertura?: number;
  coberturaStr?: string;
  idTendencia?: number;
  tendenciaStr?: string;
  idDependencia?: number;
  dependenciaStr?: string;
  siglaDependenciaStr?: string;
  idModalidad?: number;
  modalidadStr?: string;
  idEstacion?: number;
  estacionStr?: string;
  vigente?: boolean;
  contadorMedios: number;
  identificacionCreacion?: number;
  maquinaCreacion?: string;
  fechaCreacion?: string;
  identificacionModifica?: number;
  maquinaModifica?: string;
  fechaModifica?: string;
}

export interface DtoHechoNoticiosoRequest {
  idUnidad: number;
  titular: string;
  fecha?: string;
  idCaracter?: number;
  idTema?: number;
  idDescTema?: number;
  idCobertura?: number;
  idTendencia?: number;
  idDependencia?: number;
  idModalidad?: number;
  idEstacion?: number;
}

export interface DtoHechoNoticiosoResult {
  success: boolean;
  message: string;
  id?: string;
}

export interface DtoMedioCoest {
  medioId: string;
  noticiaId: string;
  idMedio?: number;
  medioStr?: string;
  idDescMedio?: string;
  descMedioStr?: string;
  titular?: string;
  url?: string;
  fecha?: string;
  idVoceria?: number;
  voceriaStr?: string;
  idReplica?: number;
  replicaStr?: string;
  cantidadReplica?: number;
  idEvidencia?: number;
  evidenciaStr?: string;
  duracion?: number;
  vigente?: boolean;
  identificacionCreacion?: number;
  maquinaCreacion?: string;
  fechaCreacion?: string;
  intervinientes: DtoIntervinienteCoest[];
}

export interface DtoMedioCoestRequest {
  noticiaId: string;
  idMedio?: number;
  idDescMedio?: string;
  titular: string;
  url?: string;
  fecha?: string;
  idEvidencia?: number;
  duracion?: number;
}

export interface DtoMedioCoestResult {
  success: boolean;
  message: string;
  id?: string;
}

export interface DtoIntervinienteCoest {
  intervinienteId: string;
  medioId: string;
  grado?: number;
  gradoStr?: string;
  nombre?: string;
  cargo?: string;
  vigente?: boolean;
  identificacionCreacion?: number;
  maquinaCreacion?: string;
  fechaCreacion?: string;
}

export interface DtoIntervinienteCoestRequest {
  medioId: string;
  grado?: number;
  nombre?: string;
  cargo?: string;
}

export interface DtoIntervinienteCoestResult {
  success: boolean;
  message: string;
  id?: string;
}

export interface DtoDominioCoest {
  dominioId: number;
  descripcion: string;
}

export interface DtoCatalogoMedio {
  ctrlMediosId: string;
  descripcion: string;
  tipoMedioId: number;
  tipoMedioStr?: string;
}

export interface DtoDependencia {
  dependenciaId: number;
  descripcion: string;
}

export interface DtoAnioItem {
  anio: number;
}

@Injectable({
  providedIn: 'root'
})
export class RecoleccionService {
  private readonly hechosUrl = `${environment.apiBaseUrl}/HechoNoticioso`;
  private readonly mediosUrl = `${environment.apiBaseUrl}/MedioCoest`;
  private readonly intervinientesUrl = `${environment.apiBaseUrl}/IntervinienteCoest`;
  private readonly dominiosUrl = `${environment.apiBaseUrl}/DominioCoest`;

  constructor(private http: HttpClient) {}

  // --- HECHOS NOTICIOSOS ---

  getHechos(anio: number = 0): Observable<DtoHechoNoticioso[]> {
    return this.http.get<DtoHechoNoticioso[]>(`${this.hechosUrl}/listado?anio=${anio}`);
  }

  getHechosPorUnidad(siglaUnidad: string, anio: number = 0): Observable<DtoHechoNoticioso[]> {
    return this.http.get<DtoHechoNoticioso[]>(`${this.hechosUrl}/unidad/${siglaUnidad}?anio=${anio}`);
  }

  getHechoById(id: string): Observable<DtoHechoNoticioso> {
    return this.http.get<DtoHechoNoticioso>(`${this.hechosUrl}/${id}`);
  }

  getAnios(): Observable<DtoAnioItem[]> {
    return this.http.get<DtoAnioItem[]>(`${this.hechosUrl}/anios`);
  }

  createHecho(payload: DtoHechoNoticiosoRequest): Observable<DtoHechoNoticiosoResult> {
    return this.http.post<DtoHechoNoticiosoResult>(this.hechosUrl, payload);
  }

  deleteHecho(id: string): Observable<DtoHechoNoticiosoResult> {
    return this.http.delete<DtoHechoNoticiosoResult>(`${this.hechosUrl}/${id}`);
  }

  // --- MEDIOS DE COMUNICACION ---

  getMediosByNoticia(noticiaId: string): Observable<DtoMedioCoest[]> {
    return this.http.get<DtoMedioCoest[]>(`${this.mediosUrl}/noticia/${noticiaId}`);
  }

  createMedio(payload: DtoMedioCoestRequest): Observable<DtoMedioCoestResult> {
    return this.http.post<DtoMedioCoestResult>(this.mediosUrl, payload);
  }

  deleteMedio(medioId: string): Observable<DtoMedioCoestResult> {
    return this.http.delete<DtoMedioCoestResult>(`${this.mediosUrl}/${medioId}`);
  }

  // --- INTERVINIENTES ---

  createInterviniente(payload: DtoIntervinienteCoestRequest): Observable<DtoIntervinienteCoestResult> {
    return this.http.post<DtoIntervinienteCoestResult>(this.intervinientesUrl, payload);
  }

  deleteInterviniente(intervinienteId: string): Observable<DtoIntervinienteCoestResult> {
    return this.http.delete<DtoIntervinienteCoestResult>(`${this.intervinientesUrl}/${intervinienteId}`);
  }

  // --- DOMINIOS Y CATALOGOS ---

  getDominios(padreId: number): Observable<DtoDominioCoest[]> {
    return this.http.get<DtoDominioCoest[]>(`${this.dominiosUrl}/${padreId}`);
  }

  getMediosCatalogo(): Observable<DtoCatalogoMedio[]> {
    return this.http.get<DtoCatalogoMedio[]>(`${this.dominiosUrl}/medios`);
  }

  getMediosPorTipoYUnidad(tipoMedioId: number, idUnidad: number): Observable<DtoCatalogoMedio[]> {
    return this.http.get<DtoCatalogoMedio[]>(`${this.dominiosUrl}/medios/tipo/${tipoMedioId}/unidad/${idUnidad}`);
  }

  getDependencias(): Observable<DtoDependencia[]> {
    return this.http.get<DtoDependencia[]>(`${this.dominiosUrl}/dependencias`);
  }

  getUnidades(): Observable<DtoDominioCoest[]> {
    return this.http.get<DtoDominioCoest[]>(`${this.dominiosUrl}/unidades`);
  }
}
