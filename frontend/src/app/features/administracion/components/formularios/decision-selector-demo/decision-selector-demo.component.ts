import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import {
  UiDecisionSelectorComponent,
  UiDecisionValue,
} from '../../../../../shared/components/ui-decision-selector/ui-decision-selector.component';
import { getFormErrorMessage } from '../../../../../shared/utils/form-error.util';

@Component({
  selector: 'app-decision-selector-demo',
  standalone: true,
  imports: [ReactiveFormsModule, UiDecisionSelectorComponent],
  templateUrl: './decision-selector-demo.component.html',
  styleUrl: './decision-selector-demo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DecisionSelectorDemoComponent {
  readonly formulario = new FormGroup({
    concepto: new FormControl<UiDecisionValue | null>(null, Validators.required),
  });

  readonly ejemplo = `<form [formGroup]="formulario">
  <app-ui-decision-selector
    formControlName="concepto"
    etiqueta="Concepto"
    ayuda="Seleccione una decisión explícita."
    [obligatorio]="true"
    [error]="errorConcepto()"
  />
</form>`;

  errorConcepto(): string {
    return getFormErrorMessage(this.formulario.controls.concepto);
  }

  validar(): void {
    this.formulario.markAllAsTouched();
  }
}
