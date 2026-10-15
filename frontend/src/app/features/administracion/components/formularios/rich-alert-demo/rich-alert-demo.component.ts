import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import {
  UiButtonComponent,
  UiButtonVariant,
} from '../../../../../shared/components/ui-button/ui-button.component';
import {
  AlertContentTone,
  AlertRichContent,
} from '../../../../../shared/models/alert-options.model';
import { AlertService } from '../../../../../shared/services/alert.service';

interface RichAlertDemoOption {
  readonly tone: AlertContentTone;
  readonly label: string;
  readonly icon: string;
  readonly headline: string;
  readonly description: string;
  readonly buttonVariant: UiButtonVariant;
}

/** Catálogo interactivo y documentación del contenido enriquecido para alertas. */
@Component({
  selector: 'app-rich-alert-demo',
  standalone: true,
  imports: [UiButtonComponent],
  templateUrl: './rich-alert-demo.component.html',
  styleUrl: './rich-alert-demo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RichAlertDemoComponent {
  private readonly alerts = inject(AlertService);

  readonly selectedTone = signal<AlertContentTone>('info');
  readonly lastResult = signal('Seleccione un tipo y pruebe uno de los dos comportamientos.');

  readonly options: readonly RichAlertDemoOption[] = [
    {
      tone: 'info',
      label: 'Información',
      icon: 'fa-solid fa-circle-info',
      headline: 'La solicitud está lista para ser revisada.',
      description: 'Presenta contexto relevante sin comunicar éxito, riesgo o rechazo.',
      buttonVariant: 'info',
    },
    {
      tone: 'success',
      label: 'Favorable',
      icon: 'fa-solid fa-circle-check',
      headline: 'El concepto se registrará como favorable.',
      description: 'Utilice este estado para una decisión positiva o una operación válida.',
      buttonVariant: 'success',
    },
    {
      tone: 'warning',
      label: 'Preventiva',
      icon: 'fa-solid fa-triangle-exclamation',
      headline: 'La duración registrada requiere revisión.',
      description: 'Advierte una condición atípica que el usuario todavía puede confirmar.',
      buttonVariant: 'warning',
    },
    {
      tone: 'danger',
      label: 'Desfavorable',
      icon: 'fa-solid fa-circle-exclamation',
      headline: 'El concepto se registrará como no favorable.',
      description: 'Reserva este estado para rechazo, bloqueo o una consecuencia sensible.',
      buttonVariant: 'danger',
    },
  ];

  readonly selectedOption = computed(
    () => this.options.find((option) => option.tone === this.selectedTone()) ?? this.options[0],
  );

  readonly usageExample = `const confirmado = await this.alertas.confirmRich({
  title: 'Revise la información',
  confirmText: 'Confirmar',
  cancelText: 'Volver',
  intent: 'success',
  content: {
    tone: 'success',
    eyebrow: 'Revisión del concepto 1',
    headline: 'El concepto se registrará como favorable.',
    description: 'Confirme que los datos sean correctos.',
    badge: { text: 'Favorable', tone: 'success' },
    details: [
      { label: 'Solicitante', value: 'Ana Gómez', icon: 'fa-regular fa-user' },
      { label: 'Duración', value: '2 horas', icon: 'fa-regular fa-clock' },
      { label: 'Periodo', value: '07/09/2026, 08:00 — 10:00', wide: true }
    ],
    note: 'Los datos se registrarán exactamente como aparecen.'
  }
});

if (!confirmado) return;
await guardar();`;

  selectTone(tone: AlertContentTone): void {
    this.selectedTone.set(tone);
  }

  /** Demuestra un mensaje enriquecido que solo requiere reconocimiento. */
  async showInformation(): Promise<void> {
    const option = this.selectedOption();
    await this.alerts.showRich({
      title: `${option.label}: alerta enriquecida`,
      confirmText: 'Entendido',
      content: this.createContent(option),
    });
    this.lastResult.set(`Se cerró la alerta ${option.label.toLowerCase()} de información.`);
  }

  /** Demuestra una decisión esperada con await antes de ejecutar la operación. */
  async showConfirmation(): Promise<void> {
    const option = this.selectedOption();
    const confirmed = await this.alerts.confirmRich({
      title: `Confirmar estado ${option.label.toLowerCase()}`,
      confirmText: 'Confirmar operación',
      cancelText: 'Volver',
      intent: option.tone === 'info' ? 'primary' : option.tone,
      focusCancel: true,
      content: this.createContent(option),
    });

    this.lastResult.set(
      confirmed
        ? `El usuario confirmó la demostración ${option.label.toLowerCase()}.`
        : 'La demostración fue cancelada y no se ejecutó ninguna operación.',
    );
  }

  /** Genera una ficha completa para que todos los tipos prueben el mismo contrato. */
  private createContent(option: RichAlertDemoOption): AlertRichContent {
    return {
      tone: option.tone,
      eyebrow: 'Componente UI · contenido estructurado',
      headline: option.headline,
      description: option.description,
      badge: { text: option.label, tone: option.tone },
      details: [
        {
          label: 'Solicitante',
          value: 'Ana María Gómez',
          icon: 'fa-regular fa-user',
        },
        {
          label: 'Duración',
          value: '2 horas',
          icon: 'fa-regular fa-clock',
          badge: { text: '2 horas', tone: option.tone },
        },
        {
          label: 'Periodo solicitado',
          value: 'lunes, 7 de septiembre de 2026, 8:00 a. m. — 10:00 a. m.',
          icon: 'fa-regular fa-calendar-check',
          wide: true,
        },
        {
          label: 'Observación',
          value: 'Ejemplo de información extensa presentada sin concatenar HTML.',
          icon: 'fa-regular fa-message',
          wide: true,
        },
      ],
      note: 'Los valores se interpolan como texto seguro y el componente se destruye al cerrar.',
    };
  }
}
