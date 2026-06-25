import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type AlertType = 'success' | 'warning' | 'error' | 'info';

export interface AlertMessage {
  type: AlertType;
  title: string;
  message: string;
  okText?: string;
  cancelText?: string;
  isConfirm?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AlertService {
  private subject = new Subject<AlertMessage>();
  alert$ = this.subject.asObservable();
  private confirmResolver?: (value: boolean) => void;

  show(type: AlertType, title: string, message: string, okText = 'OK'): void {
    this.subject.next({ type, title, message, okText });
  }

  /**
   * Muestra un diálogo de confirmación y devuelve una promesa resuelta
   * con true si el usuario confirma, o false si cancela/cierra.
   */
  confirm(title: string, message: string, okText = 'OK', cancelText = 'Cancelar', type: AlertType = 'warning'): Promise<boolean> {
    this.subject.next({ type, title, message, okText, cancelText, isConfirm: true });
    return new Promise<boolean>(resolve => {
      this.confirmResolver = resolve;
    });
  }

  /** Responde al diálogo de confirmación (invocado por el componente que muestra la alerta). */
  respond(result: boolean): void {
    if (this.confirmResolver) {
      this.confirmResolver(result);
      this.confirmResolver = undefined;
    }
  }

  warning(title: string, message: string, okText = 'OK'): void {
    this.show('warning', title, message, okText);
  }

  error(title: string, message: string, okText = 'OK'): void {
    this.show('error', title, message, okText);
  }

  info(title: string, message: string, okText = 'OK'): void {
    this.show('info', title, message, okText);
  }

  success(title: string, message: string, okText = 'OK'): void {
    this.show('success', title, message, okText);
  }
}
