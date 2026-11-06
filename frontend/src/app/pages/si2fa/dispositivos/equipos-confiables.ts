import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, Dispositivo2Fa } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

/** Equipos (navegadores) marcados como «confiables»: omiten el código TOTP durante unos días. */
@Component({
  selector: 'app-si2fa-equipos-confiables',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <div class="si2fa-toolbar-row" style="margin-bottom:14px">
      <div class="ui-field" style="min-width:200px">
        <label class="ui-label">ID MFA (opcional)</label>
        <input class="ui-control" [(ngModel)]="idMfa" type="number" placeholder="Filtrar por ID MFA" />
      </div>
      <label class="ui-label" style="display:flex;align-items:center;gap:8px;margin-bottom:10px">
        <input type="checkbox" [(ngModel)]="soloVigentes" /> Solo vigentes
      </label>
      <button class="ui-btn primary" (click)="cargar()"><i class="fa-solid fa-magnifying-glass"></i> Consultar</button>
    </div>

    <div *ngIf="loading" class="si2fa-muted"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

    <div *ngIf="!loading" class="ui-table-wrap">
      <table class="ui-table">
        <thead>
          <tr><th>ID</th><th>Identificación</th><th>Usuario</th><th>Equipo</th><th>Expira</th><th>Vigente</th><th>Registrado</th><th></th></tr>
        </thead>
        <tbody>
          <tr *ngFor="let d of dispositivos">
            <td>{{ d.idDevice }}</td>
            <td>{{ d.identificacion }}</td>
            <td>{{ d.usuarioInstitucional }}</td>
            <td class="si2fa-mono">{{ (d.deviceHash || '').slice(0,16) }}…</td>
            <td>{{ d.expiraAt ? (d.expiraAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
            <td><span class="si2fa-badge" [ngClass]="d.vigente ? 'green' : 'gray'">{{ d.vigente ? 'Sí' : 'Expirado' }}</span></td>
            <td>{{ d.creadoAt ? (d.creadoAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
            <td><button class="ui-btn si2fa-sm info" (click)="revocar(d)"><i class="fa-solid fa-trash"></i> Quitar confianza</button></td>
          </tr>
          <tr *ngIf="!dispositivos.length"><td colspan="8" class="empty-row">Sin equipos confiables.</td></tr>
        </tbody>
      </table>
    </div>
  `
})
export class Si2faEquiposConfiablesComponent implements OnInit {
  dispositivos: Dispositivo2Fa[] = [];
  idMfa: number | null = null;
  soloVigentes = true;
  loading = false;

  constructor(private service: Si2faService, private toast: ToastService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading = true;
    this.service.getDispositivos(this.idMfa ?? undefined, this.soloVigentes).subscribe({
      next: (d) => { this.dispositivos = d; this.loading = false; },
      error: () => { this.loading = false; this.toast.error('Error', 'No fue posible cargar los equipos.'); }
    });
  }

  revocar(d: Dispositivo2Fa): void {
    if (!confirm(`¿Quitar la confianza a este equipo de ${d.usuarioInstitucional}? Volverá a pedirle el código TOTP.`)) return;
    this.service.revocarDispositivo(d.idDevice).subscribe({
      next: (r) => { this.toast.success('Listo', r.message || 'Equipo quitado.'); this.cargar(); },
      error: () => this.toast.error('Error', 'No se pudo quitar.')
    });
  }
}
