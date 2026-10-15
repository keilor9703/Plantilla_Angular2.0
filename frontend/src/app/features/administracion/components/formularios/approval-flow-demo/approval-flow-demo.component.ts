import { ChangeDetectionStrategy, Component } from '@angular/core';

import { UiApprovalFlowComponent } from '../../../../../shared/components/ui-approval-flow/ui-approval-flow.component';
import { UiApprovalProcess } from '../../../../../shared/components/ui-approval-flow/ui-approval-flow.types';

@Component({
  selector: 'app-approval-flow-demo',
  standalone: true,
  imports: [UiApprovalFlowComponent],
  templateUrl: './approval-flow-demo.component.html',
  styleUrl: './approval-flow-demo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApprovalFlowDemoComponent {
  readonly example: UiApprovalProcess = {
    id: 'demo-approval',
    reference: 'TRM-2026-0042',
    title: 'Actividad personal',
    subtitle: 'Solicitud con aprobación multinivel',
    status: 'in-review',
    statusLabel: 'En revisión',
    startAt: '28 ago 2026 · 08:00',
    endAt: '28 ago 2026 · 12:00',
    currentInbox: 'Segundo concepto',
    observation:
      'Solicito autorización para atender una diligencia personal durante el horario registrado. Las actividades prioritarias quedarán coordinadas previamente.',
    source: 'Datos de demostración',
    stages: [
      {
        id: 'first',
        label: 'Concepto 1',
        actor: 'Responsable inmediato',
        role: 'Primer nivel',
        status: 'approved',
        decidedAt: '25 ago 2026 · 09:20',
        note: 'Concepto favorable. Se verificó la continuidad del servicio y se coordinó la entrega de actividades prioritarias al equipo responsable.',
      },
      {
        id: 'second',
        label: 'Concepto 2',
        actor: 'Jefe de dependencia',
        role: 'Segundo nivel',
        status: 'pending',
        note: 'Registro disponible en su bandeja. Este ejemplo demuestra cómo se presenta un concepto extenso sin recortar información ni ensanchar la vista.',
      },
      {
        id: 'final',
        label: 'Autorización',
        actor: 'Director responsable',
        role: 'Decisión final',
        status: 'pending',
      },
    ],
  };
}
