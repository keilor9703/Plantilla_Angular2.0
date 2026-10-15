import { DOCUMENT } from '@angular/common';
import { DestroyRef, Directive, ElementRef, inject } from '@angular/core';

/**
 * Coordina una región con scroll interno y el contenedor desplazable de la
 * página. Mientras ambos pueden avanzar, la rueda desplaza los dos; cuando la
 * región llega a un límite, el navegador conserva el encadenamiento natural
 * hacia el contenedor exterior.
 *
 * No intercepta el gesto ni llama preventDefault, por lo que mantiene el
 * comportamiento nativo del teclado, la barra de desplazamiento y la rueda.
 */
@Directive({
  selector: '[appUiCoordinatedScroll]',
  standalone: true,
})
export class UiCoordinatedScrollDirective {
  private static readonly FactorAcompanamiento = 0.24;
  private static readonly PasoMaximoPorFrame = 32;

  private readonly elemento = inject(ElementRef<HTMLElement>).nativeElement;
  private readonly documento = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private contenedorExterno: HTMLElement | null = null;
  private desplazamientoPendiente = 0;
  private cuadroAnimacion: number | null = null;

  private readonly manejarRueda = (evento: WheelEvent): void => {
    if (
      evento.defaultPrevented ||
      evento.ctrlKey ||
      Math.abs(evento.deltaY) <= Math.abs(evento.deltaX) ||
      this.elemento.closest('[aria-modal="true"]')
    ) {
      return;
    }

    const desplazamiento = this.normalizarDelta(evento);
    if (desplazamiento === 0 || !this.puedeDesplazarse(this.elemento, desplazamiento)) {
      return;
    }

    const exterior = this.obtenerContenedorExterno();
    if (!exterior || !this.puedeDesplazarse(exterior, desplazamiento)) {
      return;
    }

    this.programarAcompanamiento(exterior, desplazamiento);
  };

  constructor() {
    this.elemento.addEventListener('wheel', this.manejarRueda, { passive: true });
    this.destroyRef.onDestroy(() => {
      this.elemento.removeEventListener('wheel', this.manejarRueda);
      this.cancelarAcompanamientoPendiente();
    });
  }

  /**
   * Traslada una fracción del gesto al contenedor exterior. Agrupar los eventos
   * en requestAnimationFrame evita saltos cuando una rueda o trackpad emite
   * varios deltas durante el mismo repintado.
   */
  private programarAcompanamiento(exterior: HTMLElement, delta: number): void {
    const ventana = this.documento.defaultView;
    if (!ventana) return;

    const aporte = delta * UiCoordinatedScrollDirective.FactorAcompanamiento;
    this.desplazamientoPendiente = this.limitarPaso(
      this.desplazamientoPendiente + aporte,
    );
    if (this.cuadroAnimacion !== null) return;

    this.cuadroAnimacion = ventana.requestAnimationFrame(() => {
      const desplazamiento = this.desplazamientoPendiente;
      this.desplazamientoPendiente = 0;
      this.cuadroAnimacion = null;

      if (this.puedeDesplazarse(exterior, desplazamiento)) {
        exterior.scrollBy({ top: desplazamiento, behavior: 'auto' });
      }
    });
  }

  private cancelarAcompanamientoPendiente(): void {
    if (this.cuadroAnimacion !== null) {
      this.documento.defaultView?.cancelAnimationFrame(this.cuadroAnimacion);
    }
    this.cuadroAnimacion = null;
    this.desplazamientoPendiente = 0;
  }

  private limitarPaso(delta: number): number {
    const limite = UiCoordinatedScrollDirective.PasoMaximoPorFrame;
    return Math.max(-limite, Math.min(limite, delta));
  }

  /** Localiza el primer ancestro que administra el desplazamiento vertical. */
  private obtenerContenedorExterno(): HTMLElement | null {
    if (this.contenedorExterno?.isConnected) return this.contenedorExterno;

    const ventana = this.documento.defaultView;
    if (!ventana) return null;

    let candidato = this.elemento.parentElement;
    while (candidato) {
      const overflowY = ventana.getComputedStyle(candidato).overflowY;
      if (/^(auto|scroll|overlay)$/u.test(overflowY)) {
        this.contenedorExterno = candidato;
        return candidato;
      }
      candidato = candidato.parentElement;
    }

    const raiz = this.documento.scrollingElement;
    this.contenedorExterno = raiz instanceof HTMLElement ? raiz : null;
    return this.contenedorExterno;
  }

  /** Convierte deltas expresados en líneas o páginas a píxeles aproximados. */
  private normalizarDelta(evento: WheelEvent): number {
    if (evento.deltaMode === WheelEvent.DOM_DELTA_LINE) return evento.deltaY * 16;
    if (evento.deltaMode === WheelEvent.DOM_DELTA_PAGE) {
      return evento.deltaY * this.elemento.clientHeight;
    }
    return evento.deltaY;
  }

  private puedeDesplazarse(elemento: HTMLElement, delta: number): boolean {
    const tolerancia = 1;
    return delta > 0
      ? elemento.scrollTop + elemento.clientHeight < elemento.scrollHeight - tolerancia
      : elemento.scrollTop > tolerancia;
  }
}
