import { Injectable, signal } from '@angular/core';

/** Identificador opaco de una operación registrada en el indicador global. */
export type GlobalLoadingHandle = symbol;

/** Configuración descriptiva de una operación que mantiene activa la carga global. */
export interface GlobalLoadingOptions {
  readonly message?: string;
  readonly priority?: number;
}

interface LoadingOperation {
  readonly message: string;
  readonly priority: number;
  readonly sequence: number;
  readonly safetyTimer?: ReturnType<typeof setTimeout>;
}

/**
 * Coordina el estado de carga de toda la aplicación.
 *
 * Cada navegación obtiene un identificador independiente. El overlay
 * se oculta cuando las operaciones han finalizado, con temporizadores de
 * seguridad para evitar bloqueos permanentes.
 */
@Injectable({ providedIn: 'root' })
export class GlobalLoadingService {
  private static readonly defaultMessage = 'Cargando módulo...';
  private static readonly showDelayMs = 120;
  private static readonly minimumVisibleMs = 300;
  private static readonly maxLifetimeMs = 5000;

  private readonly operations = new Map<GlobalLoadingHandle, LoadingOperation>();
  private readonly visibleState = signal(false);
  private readonly messageState = signal(GlobalLoadingService.defaultMessage);
  private readonly activeCountState = signal(0);

  private sequence = 0;
  private visibleSince = 0;
  private showTimer?: ReturnType<typeof setTimeout>;
  private hideTimer?: ReturnType<typeof setTimeout>;

  /** Indica si el overlay global debe estar visible. */
  readonly visible = this.visibleState.asReadonly();

  /** Mensaje correspondiente a la operación activa de mayor prioridad. */
  readonly message = this.messageState.asReadonly();

  /** Cantidad de operaciones concurrentes pendientes. */
  readonly activeCount = this.activeCountState.asReadonly();

  /**
   * Registra una operación y devuelve el identificador que deberá finalizarse.
   */
  begin(options: GlobalLoadingOptions = {}): GlobalLoadingHandle {
    const handle = Symbol('global-loading-operation');

    // Timer de seguridad: expira automáticamente si no se finaliza explícitamente
    const safetyTimer = setTimeout(() => {
      this.end(handle);
    }, GlobalLoadingService.maxLifetimeMs);

    this.operations.set(handle, {
      message: this.normalizeMessage(options.message),
      priority: options.priority ?? 0,
      sequence: ++this.sequence,
      safetyTimer,
    });

    this.activeCountState.set(this.operations.size);
    this.refreshMessage();
    this.cancelHide();

    if (!this.visibleState() && this.showTimer === undefined) {
      this.showTimer = setTimeout(() => {
        this.showTimer = undefined;
        if (this.operations.size > 0) {
          this.visibleSince = Date.now();
          this.visibleState.set(true);
        }
      }, GlobalLoadingService.showDelayMs);
    }

    return handle;
  }

  /**
   * Finaliza una operación previamente registrada. Es idempotente.
   */
  end(handle: GlobalLoadingHandle): void {
    const op = this.operations.get(handle);
    if (!op) {
      return;
    }

    if (op.safetyTimer) {
      clearTimeout(op.safetyTimer);
    }
    this.operations.delete(handle);

    this.activeCountState.set(this.operations.size);
    this.refreshMessage();

    if (this.operations.size > 0) {
      return;
    }

    this.cancelShow();
    if (!this.visibleState()) {
      this.messageState.set(GlobalLoadingService.defaultMessage);
      return;
    }

    const elapsed = Date.now() - this.visibleSince;
    const remaining = Math.max(GlobalLoadingService.minimumVisibleMs - elapsed, 0);
    this.hideTimer = setTimeout(() => this.hide(), remaining);
  }

  /**
   * Restablece completamente el indicador ocultando el overlay de inmediato.
   */
  reset(): void {
    for (const op of this.operations.values()) {
      if (op.safetyTimer) {
        clearTimeout(op.safetyTimer);
      }
    }
    this.operations.clear();
    this.activeCountState.set(0);
    this.cancelShow();
    this.cancelHide();
    this.hide();
  }

  private hide(): void {
    this.hideTimer = undefined;
    if (this.operations.size > 0) {
      return;
    }

    this.visibleState.set(false);
    this.visibleSince = 0;
    this.messageState.set(GlobalLoadingService.defaultMessage);
  }

  private refreshMessage(): void {
    let selected: LoadingOperation | undefined;
    for (const operation of this.operations.values()) {
      if (
        selected === undefined ||
        operation.priority > selected.priority ||
        (operation.priority === selected.priority && operation.sequence > selected.sequence)
      ) {
        selected = operation;
      }
    }

    this.messageState.set(selected?.message ?? GlobalLoadingService.defaultMessage);
  }

  private normalizeMessage(message: string | undefined): string {
    return message?.trim() || GlobalLoadingService.defaultMessage;
  }

  private cancelShow(): void {
    if (this.showTimer !== undefined) {
      clearTimeout(this.showTimer);
      this.showTimer = undefined;
    }
  }

  private cancelHide(): void {
    if (this.hideTimer !== undefined) {
      clearTimeout(this.hideTimer);
      this.hideTimer = undefined;
    }
  }
}
