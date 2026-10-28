import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, Metricas2Fa } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-si2fa-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <div class="si2fa-header">
      <div>
        <h1><i class="fa-solid fa-gauge-high"></i> Tablero 2FA</h1>
        <div class="sub">Estado y actividad del servicio de doble factor de autenticación</div>
      </div>
      <div class="toolbar" style="margin:0">
        <label style="font-size:.85rem;color:#6b7280">Ventana:</label>
        <select [(ngModel)]="dias" (change)="cargar()">
          <option [ngValue]="7">7 días</option>
          <option [ngValue]="30">30 días</option>
          <option [ngValue]="90">90 días</option>
          <option [ngValue]="365">1 año</option>
        </select>
        <button class="btn secondary sm" (click)="cargar()"><i class="fa-solid fa-rotate"></i> Actualizar</button>
      </div>
    </div>

    <div *ngIf="loading" class="loading"><i class="fa-solid fa-spinner fa-spin"></i> Cargando métricas…</div>

    <ng-container *ngIf="!loading && m">
      <div class="kpi-grid">
        <div class="card kpi info"><div class="value">{{ m.totalUsuarios }}</div><div class="label">Usuarios registrados</div></div>
        <div class="card kpi ok"><div class="value">{{ m.usuariosEnrolados }}</div><div class="label">Enrolados</div></div>
        <div class="card kpi warn"><div class="value">{{ m.usuariosPendientesEnrolar }}</div><div class="label">Pendientes de enrolar</div></div>
        <div class="card kpi danger"><div class="value">{{ m.usuariosBloqueados }}</div><div class="label">Bloqueados</div></div>
        <div class="card kpi"><div class="value">{{ m.sistemasActivos }}/{{ m.sistemasTotales }}</div><div class="label">Sistemas activos</div></div>
        <div class="card kpi info"><div class="value">{{ m.dispositivosConfiablesActivos }}</div><div class="label">Dispositivos confiables</div></div>
      </div>

      <div class="kpi-grid">
        <div class="card kpi ok"><div class="value">{{ m.verificacionesOk }}</div><div class="label">Verificaciones OK ({{ m.ventanaDias }}d)</div></div>
        <div class="card kpi danger"><div class="value">{{ m.verificacionesFallidas }}</div><div class="label">Verificaciones fallidas ({{ m.ventanaDias }}d)</div></div>
        <div class="card kpi ok"><div class="value">{{ m.enrolamientos }}</div><div class="label">Enrolamientos ({{ m.ventanaDias }}d)</div></div>
        <div class="card kpi warn"><div class="value">{{ m.restablecimientos }}</div><div class="label">Restablecimientos ({{ m.ventanaDias }}d)</div></div>
      </div>

      <div class="grid-2">
        <div class="card">
          <h3>Actividad por sistema</h3>
          <div *ngIf="!m.actividadPorSistema.length" class="empty">Sin actividad en el periodo.</div>
          <div *ngFor="let c of m.actividadPorSistema" class="bar-row">
            <span class="name" [title]="c.clave">{{ c.clave }}</span>
            <span class="bar"><span [style.width.%]="pct(c.total, maxSistema)"></span></span>
            <span class="n">{{ c.total }}</span>
          </div>
        </div>

        <div class="card">
          <h3>Eventos por tipo</h3>
          <div *ngIf="!m.eventosPorTipo.length" class="empty">Sin eventos en el periodo.</div>
          <div *ngFor="let c of m.eventosPorTipo" class="bar-row">
            <span class="name" [title]="c.clave">{{ c.clave }}</span>
            <span class="bar"><span [style.width.%]="pct(c.total, maxEvento)"></span></span>
            <span class="n">{{ c.total }}</span>
          </div>
        </div>
      </div>

      <div class="card" style="margin-top:1rem">
        <h3>Verificaciones por día</h3>
        <div *ngIf="!m.serieDiaria.length" class="empty">Sin datos en el periodo.</div>
        <div *ngFor="let d of m.serieDiaria" class="bar-row">
          <span class="name">{{ d.fecha }}</span>
          <span class="bar" style="display:flex;background:transparent;gap:2px">
            <span style="background:#16a34a;border-radius:7px 0 0 7px" [style.width.%]="pct(d.verificacionesOk, maxDia)"></span>
            <span style="background:#dc2626" [style.width.%]="pct(d.verificacionesFallidas, maxDia)"></span>
          </span>
          <span class="n">{{ d.total }}</span>
        </div>
        <div style="margin-top:.5rem;font-size:.78rem;color:#6b7280">
          <span class="badge green">OK</span> &nbsp; <span class="badge red">Fallidas</span>
        </div>
      </div>
    </ng-container>
  `
})
export class Si2faDashboardComponent implements OnInit {
  m: Metricas2Fa | null = null;
  dias = 30;
  loading = false;

  maxSistema = 1; maxEvento = 1; maxDia = 1;

  constructor(private service: Si2faService, private toast: ToastService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading = true;
    this.service.getMetricas(this.dias).subscribe({
      next: (data) => {
        this.m = data;
        this.maxSistema = Math.max(1, ...data.actividadPorSistema.map(x => x.total));
        this.maxEvento = Math.max(1, ...data.eventosPorTipo.map(x => x.total));
        this.maxDia = Math.max(1, ...data.serieDiaria.map(x => x.total));
        this.loading = false;
      },
      error: () => { this.loading = false; this.toast.error('Error', 'No fue posible cargar las métricas.'); }
    });
  }

  pct(v: number, max: number): number { return max > 0 ? Math.round((v / max) * 100) : 0; }
}
