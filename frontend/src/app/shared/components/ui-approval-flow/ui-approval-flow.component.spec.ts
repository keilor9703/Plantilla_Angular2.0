import { TestBed } from '@angular/core/testing';

import { UiApprovalFlowComponent } from './ui-approval-flow.component';
import { UiApprovalProcess } from './ui-approval-flow.types';

describe('UiApprovalFlowComponent', () => {
  const process: UiApprovalProcess = {
    id: 'permiso-42',
    reference: '42',
    title: 'Actividad personal',
    status: 'in-review',
    statusLabel: 'En revisión',
    startAt: '23 sep 2026 · 08:00',
    endAt: '23 sep 2026 · 10:00',
    currentInbox: 'Concepto 1',
    observation: 'Necesito atender una diligencia personal.',
    stages: [],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [UiApprovalFlowComponent] });
  });

  it('separa el motivo de la justificación del solicitante', () => {
    const fixture = TestBed.createComponent(UiApprovalFlowComponent);
    fixture.componentRef.setInput('process', process);
    fixture.detectChanges();

    const context = fixture.nativeElement.querySelector('.ui-approval-flow__request-context');
    const observation = fixture.nativeElement.querySelector('.ui-approval-flow__observation');

    expect(context.textContent).toContain('Motivo');
    expect(context.textContent).toContain('Actividad personal');
    expect(observation.textContent).toContain('Justificación del solicitante');
    expect(observation.textContent).toContain('Necesito atender una diligencia personal.');
  });

  it('permite personalizar etiquetas y presenta un vacío explícito', () => {
    const fixture = TestBed.createComponent(UiApprovalFlowComponent);
    fixture.componentRef.setInput('process', { ...process, observation: undefined });
    fixture.componentRef.setInput('observationContextLabel', 'Tipo de solicitud');
    fixture.componentRef.setInput('observationContextValue', 'Permiso institucional');
    fixture.componentRef.setInput('emptyObservationText', 'Sin detalle registrado.');
    fixture.detectChanges();

    const context = fixture.nativeElement.querySelector('.ui-approval-flow__request-context');
    const observation = fixture.nativeElement.querySelector('.ui-approval-flow__observation');

    expect(context.textContent).toContain('Tipo de solicitud');
    expect(context.textContent).toContain('Permiso institucional');
    expect(observation.classList).toContain('is-empty');
    expect(observation.textContent).toContain('Sin detalle registrado.');
  });
});
