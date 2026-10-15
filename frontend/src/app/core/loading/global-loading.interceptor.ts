import { HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { GlobalLoadingService } from './global-loading.service';

/**
 * Permite excluir expresamente solicitudes silenciosas, como telemetría o
 * sondeos periódicos, para que no bloqueen visualmente la aplicación.
 */
export const SKIP_GLOBAL_LOADING = new HttpContextToken<boolean>(() => false);

/**
 * Mantiene activo el indicador global durante el ciclo completo de cada
 * solicitud HttpClient, incluidos errores y cancelaciones por desuscripción.
 */
export const globalLoadingInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(SKIP_GLOBAL_LOADING)) {
    return next(request);
  }

  const loading = inject(GlobalLoadingService);
  const handle = loading.begin({
    message: isReadOnlyRequest(request.method)
      ? 'Consultando información...'
      : 'Procesando solicitud...',
    priority: 20,
  });

  return next(request).pipe(finalize(() => loading.end(handle)));
};

function isReadOnlyRequest(method: string): boolean {
  const normalizedMethod = method.toUpperCase();
  return (
    normalizedMethod === 'GET' ||
    normalizedMethod === 'HEAD' ||
    normalizedMethod === 'OPTIONS'
  );
}
