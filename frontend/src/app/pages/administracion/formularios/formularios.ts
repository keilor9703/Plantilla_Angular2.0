import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../../../core/services/toast.service';
import { AlertService } from '../../../core/services/alert.service';


@Component({
  selector: 'app-formularios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './formularios.html',
  styleUrls: ['./formularios.scss'],
})
export class Formularios {
  constructor(private toast: ToastService, private alert: AlertService) {}

  textValue = '';
  emailValue = '';
  numberValue: number | null = null;
  selectValue = '';
  textareaValue = '';
  checked = false;
  radioValue = 'op1';
  dateValue = '';

  onBuscar(): void {
  this.toast.info('Buscar', 'Consultando registros...');
}

onNuevo(): void {
  this.toast.success('Nuevo', 'Formulario listo para crear un registro.');
}

  async onEliminar(): Promise<void> {
    const confirmed = await this.alert.confirm(
      'Confirmar eliminación',
      '¿Desea eliminar el registro seleccionado?',
      'Eliminar',
      'Cancelar'
    );

    if (confirmed) {
      this.toast.success('Eliminar', 'Registro eliminado.');
      // TODO: implementar la lógica real de eliminación aquí
    } else {
      this.toast.info('Eliminar', 'Operación cancelada.');
    }
  }
  btnAdvertencia() {
    this.alert.warning('Advertencia', 'No se encontraron datos para el EKOGUI proporcionado');
  }

  btnError() {
    this.alert.error('Error', 'No se encontraron datos para el numero  proporcionado');
  }

    btnInfo() {
    this.alert.info('Información', 'No se encontraron datos para el numero  proporcionado');
  }
 minimized = false;
  visible = true;

  toggleMinimize(): void {
    this.minimized = !this.minimized;
  }

  closePanel(): void {
    this.visible = false;
  }
}


