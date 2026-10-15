import { DestroyRef, Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationSkipped,
  NavigationStart,
  Router,
} from '@angular/router';

import { GlobalLoadingHandle, GlobalLoadingService } from './global-loading.service';

/**
 * Traduce el ciclo de vida del Router en operaciones del indicador global.
 * Solo escucha eventos directos de navegación por id para ignorar preloads pasivos.
 */
@Injectable({ providedIn: 'root' })
export class RouterLoadingService {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly loading = inject(GlobalLoadingService);
  private readonly navigationHandles = new Map<number, GlobalLoadingHandle>();
  private initialized = false;

  /** Inicia una sola vez la observación global de eventos de navegación. */
  start(): void {
    if (this.initialized) {
      return;
    }

    this.initialized = true;
    this.router.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.startNavigation(event.id);
        return;
      }

      if (
        event instanceof NavigationEnd ||
        event instanceof NavigationCancel ||
        event instanceof NavigationError ||
        event instanceof NavigationSkipped
      ) {
        this.endNavigation(event.id);
      }
    });
  }

  private startNavigation(navigationId: number): void {
    this.endNavigation(navigationId);
    const handle = this.loading.begin({
      message: 'Abriendo módulo...',
      priority: 100,
    });
    this.navigationHandles.set(navigationId, handle);
  }

  private endNavigation(navigationId: number): void {
    const handle = this.navigationHandles.get(navigationId);
    if (handle !== undefined) {
      this.navigationHandles.delete(navigationId);
      this.loading.end(handle);
    }

    // Si no quedan navegaciones pendientes, asegurarse de que todo esté limpio
    if (this.navigationHandles.size === 0) {
      this.loading.reset();
    }
  }
}
