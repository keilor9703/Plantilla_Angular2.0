export type SweetAlertIcon = 'warning' | 'error' | 'success' | 'info' | 'question';

export type AlertIntent = 'primary' | 'success' | 'warning' | 'danger';
export type AlertContentTone = 'info' | 'success' | 'warning' | 'danger';

/** Etiqueta breve que destaca el dato principal del mensaje. */
export interface AlertContentBadge {
  readonly text: string;
  readonly tone?: AlertContentTone;
}

/** Dato de solo lectura presentado dentro de una alerta enriquecida. */
export interface AlertContentDetail {
  readonly label: string;
  readonly value: string;
  readonly icon?: string;
  readonly wide?: boolean;
  readonly badge?: AlertContentBadge;
}

/**
 * Contenido visual reutilizable para confirmaciones que requieren contexto.
 * Todos los valores se renderizan como texto de Angular, sin HTML dinámico.
 */
export interface AlertRichContent {
  readonly tone: AlertContentTone;
  readonly eyebrow?: string;
  readonly headline: string;
  readonly description?: string;
  readonly badge?: AlertContentBadge;
  readonly details?: readonly AlertContentDetail[];
  readonly note?: string;
}

/**
 * Contrato estable de la aplicación. Los consumidores no dependen de la
 * configuración interna de SweetAlert2.
 */
export interface AlertOptions {
  readonly title: string;
  readonly message?: string;
  readonly icon?: SweetAlertIcon;
  readonly confirmText?: string;
  readonly allowOutsideClick?: boolean;
  readonly allowEscapeKey?: boolean;
}

export interface ConfirmAlertOptions extends AlertOptions {
  readonly cancelText?: string;
  readonly intent?: AlertIntent;
  readonly focusCancel?: boolean;
}

export interface RichAlertOptions extends Omit<AlertOptions, 'message' | 'icon'> {
  readonly content: AlertRichContent;
}

export interface RichConfirmAlertOptions
  extends Omit<ConfirmAlertOptions, 'message' | 'icon'> {
  readonly content: AlertRichContent;
}
