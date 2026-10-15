import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { AlertContentTone, AlertRichContent } from '../../models/alert-options.model';

const ICONS_BY_TONE: Readonly<Record<AlertContentTone, string>> = {
  info: 'fa-solid fa-circle-info',
  success: 'fa-solid fa-circle-check',
  warning: 'fa-solid fa-triangle-exclamation',
  danger: 'fa-solid fa-circle-exclamation',
};

/**
 * Presenta información estructurada dentro de SweetAlert2. El componente no
 * abre ni cierra la alerta: únicamente controla su contenido visual seguro.
 */
@Component({
  selector: 'app-ui-alert-content',
  standalone: true,
  templateUrl: './ui-alert-content.component.html',
  styleUrl: './ui-alert-content.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiAlertContentComponent {
  readonly content = input.required<AlertRichContent>();
  readonly icon = computed(() => ICONS_BY_TONE[this.content().tone]);
}
