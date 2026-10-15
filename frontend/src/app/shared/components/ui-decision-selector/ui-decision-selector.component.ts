import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  forwardRef,
  input,
  output,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export type UiDecisionValue = 'SI' | 'NO';

let siguienteIdDecision = 0;

/**
 * Selector binario accesible para decisiones explícitas. Se integra con
 * Reactive Forms y nunca convierte un valor vacío en una aprobación implícita.
 */
@Component({
  selector: 'app-ui-decision-selector',
  standalone: true,
  templateUrl: './ui-decision-selector.component.html',
  styleUrl: './ui-decision-selector.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiDecisionSelectorComponent),
      multi: true,
    },
  ],
})
export class UiDecisionSelectorComponent implements ControlValueAccessor {
  readonly etiqueta = input('Decisión');
  readonly ayuda = input('');
  readonly error = input('');
  readonly obligatorio = input(false, { transform: booleanAttribute });
  readonly mostrarAsterisco = input(true, { transform: booleanAttribute });
  readonly deshabilitado = input(false, { transform: booleanAttribute });
  readonly etiquetaSi = input('Favorable');
  readonly descripcionSi = input('Emitir concepto positivo');
  readonly etiquetaNo = input('No favorable');
  readonly descripcionNo = input('Emitir concepto negativo');
  readonly cambioDecision = output<UiDecisionValue>();

  readonly idGrupo = `ui-decision-${siguienteIdDecision++}`;
  readonly valor = signal<UiDecisionValue | null>(null);
  private readonly deshabilitadoPorFormulario = signal(false);
  readonly estaDeshabilitado = computed(
    () => this.deshabilitado() || this.deshabilitadoPorFormulario(),
  );
  readonly descritoPor = computed(() => {
    if (this.error()) return `${this.idGrupo}-error`;
    if (this.ayuda()) return `${this.idGrupo}-ayuda`;
    return null;
  });

  private alCambiar: (valor: UiDecisionValue | null) => void = () => undefined;
  private alTocar: () => void = () => undefined;

  writeValue(valor: UiDecisionValue | null | undefined): void {
    this.valor.set(valor ?? null);
  }

  registerOnChange(fn: (valor: UiDecisionValue | null) => void): void {
    this.alCambiar = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.alTocar = fn;
  }

  setDisabledState(deshabilitado: boolean): void {
    this.deshabilitadoPorFormulario.set(deshabilitado);
  }

  seleccionar(valor: UiDecisionValue): void {
    if (this.estaDeshabilitado() || this.valor() === valor) return;

    this.valor.set(valor);
    this.alCambiar(valor);
    this.alTocar();
    this.cambioDecision.emit(valor);
  }

  manejarTeclado(evento: KeyboardEvent): void {
    if (this.estaDeshabilitado()) return;

    if (evento.key === 'ArrowLeft' || evento.key === 'ArrowUp' || evento.key === 'Home') {
      evento.preventDefault();
      this.seleccionar('SI');
    }

    if (evento.key === 'ArrowRight' || evento.key === 'ArrowDown' || evento.key === 'End') {
      evento.preventDefault();
      this.seleccionar('NO');
    }
  }
}
