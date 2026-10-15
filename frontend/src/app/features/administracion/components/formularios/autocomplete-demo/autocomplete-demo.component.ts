import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { UiAutocompleteComponent } from '../../../../../shared/components/ui-autocomplete/ui-autocomplete.component';
import { UiButtonComponent } from '../../../../../shared/components/ui-button/ui-button.component';
import { UiAutocompleteOption } from '../../../../../shared/interfaces/ui-autocomplete-option.interface';
import { getFormErrorMessage } from '../../../../../shared/utils/form-error.util';

@Component({
  selector: 'app-autocomplete-demo',
  standalone: true,
  imports: [ReactiveFormsModule, UiAutocompleteComponent, UiButtonComponent],
  templateUrl: './autocomplete-demo.component.html',
  styleUrl: './autocomplete-demo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutocompleteDemoComponent {
  readonly formulario = new FormGroup({
    funcionario: new FormControl<number | null>(null, Validators.required),
  });

  readonly funcionarios: readonly UiAutocompleteOption<number>[] = [
    {
      value: 1001,
      label: 'TC. MARÍA CAMILA ORTIZ',
      description: 'DICAR · Dirección de Carabineros',
      searchText: '1001 maria camila ortiz',
    },
    {
      value: 1002,
      label: 'IT. ÓSCAR JAVIER MORENO DÍAZ',
      description: 'OFITE · Oficina de Tecnologías de la Información',
      searchText: '1002 oscar javier moreno diaz',
    },
    {
      value: 1003,
      label: 'PT. LAURA SOFÍA GÓMEZ RUIZ',
      description: 'GUSOF · Grupo Desarrollo de Software',
      searchText: '1003 laura sofia gomez ruiz',
    },
  ];

  readonly ejemplo = `<app-ui-autocomplete
  formControlName="funcionario"
  label="Remitir a funcionario por nombre"
  placeholder="Escriba nombre, apellido o grado"
  [options]="funcionarios"
  [required]="true"
  [error]="errorFuncionario()"
/>`;

  errorFuncionario(): string {
    return getFormErrorMessage(this.formulario.controls.funcionario);
  }

  validar(): void {
    this.formulario.markAllAsTouched();
  }
}
