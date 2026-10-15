import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { RadioStation } from '../models/radio.model';

export interface DtoRadioEmisora {
  idEmisora?: number | string;
  id?: number | string;
  nombre?: string;
  name?: string;
  streamUrl?: string;
  logoUrl?: string | null;
  orden?: number;
  activo?: number;
}

const DEFAULT_STATIONS: readonly RadioStation[] = [
  {
    id: 'radio-policia-nacional',
    name: 'Radio Policía Nacional',
    streamUrl: (environment as any).radioStreamUrl || 'https://live.truckers.fm/',
    logoUrl: '/imagenes/radio-policia-bogota.svg',
  },
];

@Injectable({ providedIn: 'root' })
export class RadioService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/Radio`;

  /**
   * Consulta el catálogo de emisoras públicas desde la API backend.
   * Si la consulta falla o no devuelve datos, aplica las emisoras institucionales por defecto.
   */
  getPublicas(): Observable<readonly RadioStation[]> {
    return this.http.get<DtoRadioEmisora[]>(`${this.baseUrl}/publicas`).pipe(
      map((items) => {
        if (!Array.isArray(items) || items.length === 0) {
          return DEFAULT_STATIONS;
        }

        const mapped = items
          .map((item, index) => this.mapToRadioStation(item, index))
          .filter((station) => Boolean(station.streamUrl));

        return mapped.length > 0 ? mapped : DEFAULT_STATIONS;
      }),
      catchError((error) => {
        console.warn('Error consultando /api/Radio/publicas, usando fallback:', error);
        return of(DEFAULT_STATIONS);
      }),
    );
  }

  getById(id: string): Observable<RadioStation | null> {
    return this.getPublicas().pipe(
      map((stations) => stations.find((s) => s.id === id) ?? null),
    );
  }

  private mapToRadioStation(item: DtoRadioEmisora, index: number): RadioStation {
    const rawId = item.idEmisora ?? item.id ?? index + 1;
    const name = (item.nombre ?? item.name ?? '').trim() || `Emisora ${index + 1}`;
    const streamUrl = (item.streamUrl ?? '').trim();
    const logoUrl = item.logoUrl?.trim() || '/imagenes/radio-policia-bogota.svg';

    return {
      id: String(rawId),
      name,
      streamUrl,
      logoUrl,
    };
  }
}
