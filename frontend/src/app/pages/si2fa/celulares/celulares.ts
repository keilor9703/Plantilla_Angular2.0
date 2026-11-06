import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, CelularApp } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

type Objetivo = { tipo: 'uno'; cel: CelularApp } | { tipo: 'todos'; cel: CelularApp };

@Component({
  selector: 'app-si2fa-celulares',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <div class="si2fa-toolbar-row" style="margin-bottom:14px">
            <div class="ui-field" style="min-width:260px">
              <label class="ui-label">Cédula o usuario</label>
              <input class="ui-control" [(ngModel)]="filtro" (keyup.enter)="cargar()" placeholder="Ej: 1012345678 o juan.perez" />
            </div>
            <label class="ui-label" style="display:flex;align-items:center;gap:8px;margin-bottom:10px">
              <input type="checkbox" [(ngModel)]="soloActivos" /> Solo activos
            </label>
            <button class="ui-btn primary" (click)="cargar()"><i class="fa-solid fa-magnifying-glass"></i> Consultar</button>
          </div>

          <div *ngIf="loading" class="si2fa-muted"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

          <div *ngIf="!loading" class="ui-table-wrap">
            <table class="ui-table">
              <thead>
                <tr>
                  <th>Cédula</th><th>Usuario</th><th>Celular</th><th>Plataforma</th><th>Estado</th>
                  <th>Registrado</th><th>Último uso</th><th></th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let c of celulares">
                  <td>{{ c.identificacion }}</td>
                  <td>{{ c.usuarioInstitucional }}</td>
                  <td>{{ c.alias || '—' }}<br /><small class="si2fa-muted">{{ c.modelo }}</small></td>
                  <td>{{ c.plataforma || '—' }}</td>
                  <td>
                    <span class="si2fa-badge" [ngClass]="c.activo ? 'green' : 'gray'">{{ c.activo ? 'Activo' : 'Revocado' }}</span>
                    <div *ngIf="!c.activo" class="si2fa-muted" style="font-size:.75rem">
                      {{ c.revocadoPor }}<span *ngIf="c.revocadoAt"> · {{ c.revocadoAt | date:'yyyy-MM-dd HH:mm' }}</span>
                    </div>
                  </td>
                  <td>{{ c.creadoAt ? (c.creadoAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
                  <td>{{ c.ultimoUsoAt ? (c.ultimoUsoAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
                  <td style="white-space:nowrap">
                    <ng-container *ngIf="c.activo">
                      <button class="ui-btn si2fa-sm info" (click)="abrir({ tipo: 'uno', cel: c })"><i class="fa-solid fa-ban"></i> Revocar</button>
                      <button class="ui-btn si2fa-sm outline" (click)="abrir({ tipo: 'todos', cel: c })" title="Revoca todos los celulares activos de este funcionario">Todos</button>
                    </ng-container>
                  </td>
                </tr>
                <tr *ngIf="!celulares.length"><td colspan="8" class="empty-row">Sin celulares.</td></tr>
              </tbody>
            </table>
          </div>


    <div class="si2fa-backdrop" *ngIf="objetivo" (click)="cerrar()">
      <div class="ui-panel si2fa-dialog" (click)="$event.stopPropagation()">
        <div class="ui-panel__head">
          <div class="ui-panel__title" style="font-size:1rem">
            {{ objetivo.tipo === 'uno' ? 'Revocar celular' : 'Revocar todos los celulares del funcionario' }}
          </div>
        </div>
        <div class="ui-panel__body">
          <p>
            <b>{{ objetivo.cel.usuarioInstitucional }}</b> ({{ objetivo.cel.identificacion }})
            <span *ngIf="objetivo.tipo === 'uno'"> — {{ objetivo.cel.alias || objetivo.cel.modelo }}</span>
          </p>
          <p class="si2fa-muted">
            El celular dejará de poder aprobar ingresos con QR y notificaciones de inmediato y el funcionario deberá registrarlo de nuevo.
            <b>No cambia</b> su contraseña ni su código TOTP (eso se gestiona en Usuarios 2FA). La acción queda en auditoría con su motivo.
          </p>
          <div class="ui-field" style="margin-bottom:12px">
            <label class="ui-label">Motivo (obligatorio) *</label>
            <textarea class="ui-control" rows="3" maxlength="300" [(ngModel)]="motivo"
              placeholder="Ej: celular robado, reporte del funcionario / caso 123"></textarea>
          </div>
          <div style="display:flex;gap:8px;justify-content:flex-end">
            <button class="ui-btn outline" (click)="cerrar()" [disabled]="enviando">Cancelar</button>
            <button class="ui-btn primary" (click)="confirmar()" [disabled]="enviando || motivo.trim().length < 5">
              <i class="fa-solid" [ngClass]="enviando ? 'fa-spinner fa-spin' : 'fa-ban'"></i> Revocar
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class Si2faCelularesComponent implements OnInit {
  celulares: CelularApp[] = [];
  filtro = '';
  soloActivos = true;
  loading = false;
  objetivo: Objetivo | null = null;
  motivo = '';
  enviando = false;

  constructor(private service: Si2faService, private toast: ToastService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading = true;
    this.service.getCelularesApp(this.filtro.trim() || undefined, this.soloActivos).subscribe({
      next: (d) => { this.celulares = d; this.loading = false; },
      error: () => { this.loading = false; this.toast.error('Error', 'No fue posible cargar los celulares.'); }
    });
  }

  abrir(o: Objetivo): void { this.objetivo = o; this.motivo = ''; }
  cerrar(): void { if (!this.enviando) this.objetivo = null; }

  confirmar(): void {
    const o = this.objetivo;
    const motivo = this.motivo.trim();
    if (!o || motivo.length < 5 || this.enviando) return;
    this.enviando = true;
    const req = o.tipo === 'uno'
      ? this.service.revocarCelularApp(o.cel.idDispositivo, motivo)
      : this.service.revocarCelularesDeUsuario(o.cel.identificacion, motivo);
    req.subscribe({
      next: (r) => { this.enviando = false; this.objetivo = null; this.toast.success('Listo', r.message || 'Celular revocado.'); this.cargar(); },
      error: (e) => { this.enviando = false; this.toast.error('Error', e?.error?.message || 'No se pudo revocar.'); }
    });
  }
}
