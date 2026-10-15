import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';

import { UiBadgeComponent } from '../ui-badge/ui-badge.component';
import { UiButtonComponent } from '../ui-button/ui-button.component';
import { UiPanelHeaderComponent } from '../ui-panel-header/ui-panel-header.component';
import { UiSectionHeaderComponent } from '../ui-section-header/ui-section-header.component';
import {
  UiApprovalProcess,
  UiApprovalProcessStatus,
  UiApprovalStageStatus,
  UiApprovalVisualState,
} from './ui-approval-flow.types';

export type UiApprovalHeaderAppearance = 'divider' | 'institutional';

const PROCESS_STATE: Record<UiApprovalProcessStatus, UiApprovalVisualState> = {
  'in-review': {
    label: 'En revisión',
    icon: 'fa-solid fa-clock',
    variant: 'warning',
  },
  approved: {
    label: 'Aprobado',
    icon: 'fa-solid fa-check',
    variant: 'success',
  },
  rejected: {
    label: 'No autorizado',
    icon: 'fa-solid fa-xmark',
    variant: 'danger',
  },
  returned: {
    label: 'Devuelto',
    icon: 'fa-solid fa-rotate-left',
    variant: 'info',
  },
  cancelled: {
    label: 'Cancelado',
    icon: 'fa-solid fa-ban',
    variant: 'neutral',
  },
};

const STAGE_STATE: Record<UiApprovalStageStatus, UiApprovalVisualState> = {
  approved: {
    label: 'Aprobado',
    icon: 'fa-solid fa-check',
    variant: 'success',
  },
  pending: {
    label: 'Pendiente',
    icon: 'fa-solid fa-hourglass-half',
    variant: 'warning',
  },
  rejected: {
    label: 'No autorizado',
    icon: 'fa-solid fa-xmark',
    variant: 'danger',
  },
  returned: {
    label: 'Devuelto',
    icon: 'fa-solid fa-rotate-left',
    variant: 'info',
  },
  'not-required': {
    label: 'No requerido',
    icon: 'fa-solid fa-minus',
    variant: 'neutral',
  },
};

@Component({
  selector: 'app-ui-approval-flow',
  standalone: true,
  imports: [UiBadgeComponent, UiButtonComponent, UiPanelHeaderComponent, UiSectionHeaderComponent],
  templateUrl: './ui-approval-flow.component.html',
  styleUrl: './ui-approval-flow.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiApprovalFlowComponent {
  readonly process = input.required<UiApprovalProcess>();
  readonly headerEyebrow = input<string>();
  readonly headerTitle = input('');
  readonly headerAppearance = input<UiApprovalHeaderAppearance>('divider');
  readonly compact = input(false, { transform: booleanAttribute });
  readonly showObservation = input(true, { transform: booleanAttribute });
  /** Etiquetas configurables para reutilizar el resumen en otros flujos administrativos. */
  readonly observationContextLabel = input('Motivo');
  readonly observationContextValue = input('');
  readonly observationLabel = input('Justificación del solicitante');
  readonly emptyObservationText = input('No se registró una justificación para esta solicitud.');
  readonly showSource = input(true, { transform: booleanAttribute });
  readonly showDetailsAction = input(false, { transform: booleanAttribute });
  readonly detailsLabel = input('Ver detalle');

  readonly detailsRequested = output<UiApprovalProcess>();

  /** Permite reemplazar el radicado cuando el dominio consumidor no maneja ese identificador. */
  readonly resolvedHeaderEyebrow = computed(
    () => this.headerEyebrow()?.trim() || this.process().reference,
  );
  readonly resolvedPanelTitle = computed(() => this.headerTitle().trim() || this.process().title);
  readonly resolvedPanelDescription = computed(() => {
    if (!this.headerTitle().trim()) return this.process().subtitle || '';
    return [this.process().title, this.process().subtitle].filter(Boolean).join(' · ');
  });
  readonly resolvedObservationContext = computed(
    () => this.observationContextValue().trim() || this.process().title,
  );
  readonly processState = computed(() => PROCESS_STATE[this.process().status]);
  readonly requiredStages = computed(
    () => this.process().stages.filter((stage) => stage.status !== 'not-required').length,
  );
  readonly resolvedStages = computed(
    () =>
      this.process().stages.filter(
        (stage) => stage.status !== 'pending' && stage.status !== 'not-required',
      ).length,
  );
  readonly progress = computed(() => {
    const total = this.requiredStages();
    return total === 0 ? 0 : Math.round((this.resolvedStages() / total) * 100);
  });
  readonly trackingSummary = computed(() => {
    const status = this.process().status;
    if (status === 'approved') return 'Ruta completada · autorización concedida';
    if (status === 'rejected') return 'Ruta completada · permiso no autorizado';
    if (status === 'returned') return 'Devuelto para realizar ajustes';
    if (status === 'cancelled') return 'Trámite cancelado';
    return `${this.resolvedStages()} de ${this.requiredStages()} etapas resueltas`;
  });

  stageState(status: UiApprovalStageStatus): UiApprovalVisualState {
    return STAGE_STATE[status];
  }

  requestDetails(): void {
    this.detailsRequested.emit(this.process());
  }
}
