import { UiStatusVariant } from '../../interfaces/ui-status.interface';

export type UiApprovalProcessStatus =
  | 'in-review'
  | 'approved'
  | 'rejected'
  | 'returned'
  | 'cancelled';

export type UiApprovalStageStatus =
  | 'approved'
  | 'pending'
  | 'rejected'
  | 'returned'
  | 'not-required';

export interface UiApprovalStage {
  readonly id: string;
  readonly label: string;
  readonly actor: string;
  readonly role?: string;
  readonly status: UiApprovalStageStatus;
  readonly decidedAt?: string;
  readonly note?: string;
}

export type UiApprovalTimeState = 'upcoming' | 'active' | 'finished';

export interface UiApprovalTimeSummary {
  readonly label: string;
  readonly icon: string;
  readonly state: UiApprovalTimeState;
}

/**
 * Contrato visual genérico para trámites que avanzan por una cadena de aprobación.
 * No contiene reglas de negocio: el módulo consumidor traduce su DTO a este modelo.
 */
export interface UiApprovalProcess {
  readonly id: string | number;
  readonly reference: string;
  readonly title: string;
  readonly subtitle?: string;
  readonly status: UiApprovalProcessStatus;
  readonly statusLabel: string;
  readonly startAt: string;
  readonly endAt: string;
  readonly currentInbox: string;
  readonly observation?: string;
  readonly source?: string;
  /** Estado temporal opcional; cuando existe reemplaza la fuente técnica en el pie. */
  readonly timeSummary?: UiApprovalTimeSummary;
  readonly stages: readonly UiApprovalStage[];
}

export interface UiApprovalVisualState {
  readonly label: string;
  readonly icon: string;
  readonly variant: UiStatusVariant;
}
