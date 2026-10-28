import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, Dispositivo2Fa } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-si2fa-dispositivos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <div class="si2fa-header">
      <div>
        <h1><i class="fa-solid fa-mobile-screen"></i> Dispositivos Confiables</h1>
        <div class="sub">Equipos registrados para omitir el segundo factor</div>
      </div>
    </div>

    <div class="toolbar">
      <input [(ngModel)]="idMfa" type="number" placeholder="Filtrar por ID MFA (opcional)" />
      <label style="font-size:.85rem;display:flex;align-items:center;gap:.35rem">
        <input type="checkbox" [(ngModel)]="soloVigentes" style="min-width:auto" /> Solo vigentes
      </label>
      <button class="btn primary" (click)="cargar()"><i class="fa-solid fa-magnifying-glass"></i> Consultar</button>
    </div>

    <div *ngIf="loading" class="loading"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

    <div *ngIf="!loading" class="table-wrap">
      <table class="tbl">
        <thead>
          <tr><th>ID</th><th>Identificación</th><th>Usuario</th><th>Hash</th><th>Expira</th><th>Vigente</th><th>Registrado</th><th></th></tr>
        </thead>
        <tbody>
          <tr *ngFor="let d of dispositivos">
            <td>{{ d.idDevice }}</td>
            <td>{{ d.identificacion }}</td>
            <td>{{ d.usuarioInstitucional }}</td>
            <td style="font-family:monospace;font-size:.75rem">{{ (d.deviceHash || '').slice(0,16) }}…</td>
            <td>{{ d.expiraAt ? (d.expiraAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
            <td><span class="badge" [ngClass]="d.vigente ? 'green' : 'gray'">{{ d.vigente ? 'Sí' : 'Expirado' }}</span></td>
            <td>{{ d.creadoAt ? (d.creadoAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
            <td><button class="btn danger sm" (click)="revocar(d)"><i class="fa-solid fa-trash"></i> Revocar</button></td>
          </tr>
          <tr *ngIf="!dispositivos.length"><td colspan="8" class="empty">Sin dispositivos.</td></tr>
        </tbody>
      </table>
    </div>
  `
})
export class Si2faDispositivosComponent implements OnInit {
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
      error: () => { this.loading = false; this.toast.error('Error', 'No fue posible cargar los dispositivos.'); }
    });
  }

  revocar(d: Dispositivo2Fa): void {
    if (!confirm(`¿Revocar el dispositivo de ${d.usuarioInstitucional}?`)) return;
    this.service.revocarDispositivo(d.idDevice).subscribe({
      next: (r) => { this.toast.success('Listo', r.message || 'Dispositivo revocado.'); this.cargar(); },
      error: () => this.toast.error('Error', 'No se pudo revocar.')
    });
  }
}
