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
  isPrompt?: boolean;
  promptLabel?: string;
  promptPlaceholder?: string;
}

@Injectable({ providedIn: 'root' })
export class AlertService {
  private subject = new Subject<AlertMessage>();
  alert$ = this.subject.asObservable();
  private confirmResolver?: (value: boolean) => void;
  private promptResolver?: (value: string | null) => void;

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

  /**
   * Muestra un diálogo de confirmación con campo de texto (motivo / observación).
   * Devuelve el texto ingresado si el usuario confirma, o null si cancela/cierra.
   */
  prompt(
    title: string,
    message: string,
    promptLabel: string = 'Motivo de la eliminación:',
    promptPlaceholder: string = 'Escriba la justificación u observación...',
    okText: string = 'Eliminar',
    cancelText: string = 'Cancelar',
    type: AlertType = 'warning'
  ): Promise<string | null> {
    this.subject.next({
      type,
      title,
      message,
      okText,
      cancelText,
      isConfirm: true,
      isPrompt: true,
      promptLabel,
      promptPlaceholder
    });
    return new Promise<string | null>(resolve => {
      this.promptResolver = resolve;
    });
  }

  /** Responde al diálogo de confirmación (invocado por el componente que muestra la alerta). */
  respond(result: boolean): void {
    if (this.confirmResolver) {
      this.confirmResolver(result);
      this.confirmResolver = undefined;
    }
  }

  /** Responde al diálogo prompt de texto. */
  respondPrompt(result: string | null): void {
    if (this.promptResolver) {
      this.promptResolver(result);
      this.promptResolver = undefined;
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
