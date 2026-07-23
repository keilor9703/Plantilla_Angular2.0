import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface DtoDepartamento {
  idDepartamento: number;
  codigoDane: string;
  nombre: string;
}

export interface DtoMunicipio {
  idMunicipio: number;
  idDepartamento: number;
  codigoDane: string;
  nombre: string;
  longitud?: number | null;
  latitud?: number | null;
}

export interface DtoUnidadOrg {
  consecutivo: number;
  sigla: string;
  nombre: string;
  siglaPapa?: string;
  descripcionDependencia?: string;
  vigente?: string;
  direccion?: string;
}

@Injectable({ providedIn: 'root' })
export class UbicacionService {
  private readonly baseUrl = `${environment.apiBaseUrl}/Ubicacion`;

  constructor(private http: HttpClient) {}

  getDepartamentos(): Observable<DtoDepartamento[]> {
    return this.http.get<DtoDepartamento[]>(`${this.baseUrl}/departamentos`);
  }

  getMunicipios(idDepartamento?: number | null): Observable<DtoMunicipio[]> {
    if (!idDepartamento || idDepartamento <= 0) {
      return this.http.get<DtoMunicipio[]>(`${this.baseUrl}/municipios`);
    }

    return this.http.get<DtoMunicipio[]>(`${this.baseUrl}/municipios?idDepartamento=${idDepartamento}`);
  }

  getUnidadesOrganigrama(): Observable<DtoUnidadOrg[]> {
    return this.http.get<DtoUnidadOrg[]>(`${this.baseUrl}/UnidadesOrganigrama`);
  }
}
