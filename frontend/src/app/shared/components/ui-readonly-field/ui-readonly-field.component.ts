import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type UiReadonlyFieldSize = 'sm' | 'md';

let nextUiReadonlyFieldId = 0;

/**
 * Presenta un dato no editable con la misma lectura visual de un control de formulario.
 * El valor permanece enfocable y seleccionable para facilitar su copia.
 */
@Component({
  selector: 'app-ui-readonly-field',
  standalone: true,
  templateUrl: './ui-readonly-field.component.html',
  styleUrl: './ui-readonly-field.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiReadonlyFieldComponent {
  private readonly generatedId = `ui-readonly-field-${nextUiReadonlyFieldId++}`;

  readonly label = input.required<string>();
  readonly value = input<string | number | null | undefined>('');
  readonly placeholder = input('Sin información');
  readonly controlId = input('');
  readonly size = input<UiReadonlyFieldSize>('md');

  readonly resolvedId = computed(() => this.controlId().trim() || this.generatedId);
  readonly displayValue = computed(() => {
    const currentValue = this.value();
    return currentValue === null || currentValue === undefined ? '' : String(currentValue);
  });
  readonly resolvedTitle = computed(() => this.displayValue() || this.placeholder());
}
