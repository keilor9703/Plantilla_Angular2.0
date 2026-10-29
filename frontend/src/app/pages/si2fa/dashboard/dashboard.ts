import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, Metricas2Fa, Salud2Fa, etiquetaEvento } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-si2fa-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <section class="home-view">
      <div class="ui-panel">
        <div class="ui-panel__head">
          <div class="ui-panel__title"><i class="fa-solid fa-gauge-high"></i> Tablero 2FA</div>
          <div class="si2fa-toolbar-row">
            <div class="ui-field" style="min-width:150px">
              <label class="ui-label">Ventana</label>
              <select class="ui-control" [(ngModel)]="dias" (change)="cargar()">
                <option [ngValue]="7">7 días</option>
                <option [ngValue]="30">30 días</option>
                <option [ngValue]="90">90 días</option>
                <option [ngValue]="365">1 año</option>
              </select>
            </div>
            <button class="ui-btn secondary" (click)="cargar()"><i class="fa-solid fa-rotate"></i> Actualizar</button>
          </div>
        </div>
        <div class="ui-panel__subtitle">Estado y actividad del servicio de doble factor de autenticación</div>

        <div class="ui-panel__body">
          <!-- Estado de salud del servicio 2FA (monitoreo en tiempo real) -->
          <div class="si2fa-health" [class.up]="salud?.up === true" [class.down]="salud?.up === false">
            <span class="dot"></span>
            <div class="txt">
              <span class="st">
                <i class="fa-solid" [ngClass]="salud?.up ? 'fa-circle-check' : (salud?.up === false ? 'fa-triangle-exclamation' : 'fa-spinner fa-spin')"></i>
                Servicio 2FA:
                {{ saludConsultada ? (salud?.up ? 'Operativo' : 'No disponible') : 'Consultando…' }}
              </span>
              <span class="sub">
                <ng-container *ngIf="saludConsultada; else sinDato">
                  {{ salud?.mensaje || (salud?.up ? 'El servicio responde correctamente.' : 'El servicio no responde.') }}
                  · Última verificación {{ ultimaVerificacion | date:'HH:mm:ss' }}
                </ng-container>
                <ng-template #sinDato>Verificación automática cada {{ saludIntervaloSeg }} s</ng-template>
              </span>
            </div>
            <div class="lat" *ngIf="salud?.up">
              <div class="v">{{ salud?.latenciaMs }}<span style="font-size:.6em"> ms</span></div>
              <div class="l">Latencia</div>
            </div>
          </div>

          <div *ngIf="loading" class="si2fa-muted"><i class="fa-solid fa-spinner fa-spin"></i> Cargando métricas…</div>

          <ng-container *ngIf="!loading && m">
            <div class="si2fa-kpis">
              <div class="si2fa-kpi info"><div class="v">{{ m.totalUsuarios }}</div><div class="l">Usuarios registrados</div></div>
              <div class="si2fa-kpi ok"><div class="v">{{ m.usuariosEnrolados }}</div><div class="l">Enrolados</div></div>
              <div class="si2fa-kpi warn"><div class="v">{{ m.usuariosPendientesEnrolar }}</div><div class="l">Pendientes de enrolar</div></div>
              <div class="si2fa-kpi danger"><div class="v">{{ m.usuariosBloqueados }}</div><div class="l">Bloqueados</div></div>
              <div class="si2fa-kpi"><div class="v">{{ m.sistemasActivos }}/{{ m.sistemasTotales }}</div><div class="l">Sistemas activos</div></div>
              <div class="si2fa-kpi info"><div class="v">{{ m.dispositivosConfiablesActivos }}</div><div class="l">Dispositivos confiables</div></div>
            </div>

            <div class="si2fa-kpis">
              <div class="si2fa-kpi ok"><div class="v">{{ m.verificacionesOk }}</div><div class="l">Verificaciones OK ({{ m.ventanaDias }}d)</div></div>
              <div class="si2fa-kpi danger"><div class="v">{{ m.verificacionesFallidas }}</div><div class="l">Verificaciones fallidas ({{ m.ventanaDias }}d)</div></div>
              <div class="si2fa-kpi ok"><div class="v">{{ m.enrolamientos }}</div><div class="l">Enrolamientos ({{ m.ventanaDias }}d)</div></div>
              <div class="si2fa-kpi warn"><div class="v">{{ m.restablecimientos }}</div><div class="l">Restablecimientos ({{ m.ventanaDias }}d)</div></div>
            </div>

            <div class="ui-grid">
              <div class="ui-col-6">
                <div class="ui-panel" style="margin:0">
                  <div class="ui-panel__head"><div class="ui-panel__title" style="font-size:1rem"><i class="fa-solid fa-layer-group"></i> Actividad por sistema</div></div>
                  <div class="ui-panel__body">
                    <div *ngIf="!m.actividadPorSistema.length" class="si2fa-muted">Sin actividad en el periodo.</div>
                    <div *ngFor="let c of m.actividadPorSistema" class="si2fa-bar">
                      <span class="n" [title]="c.clave">{{ c.clave }}</span>
                      <span class="track"><span [style.width.%]="pct(c.total, maxSistema)"></span></span>
                      <span class="t">{{ c.total }}</span>
                    </div>
                  </div>
                </div>
              </div>

              <div class="ui-col-6">
                <div class="ui-panel" style="margin:0">
                  <div class="ui-panel__head"><div class="ui-panel__title" style="font-size:1rem"><i class="fa-solid fa-list"></i> Eventos por tipo</div></div>
                  <div class="ui-panel__body">
                    <div *ngIf="!m.eventosPorTipo.length" class="si2fa-muted">Sin eventos en el periodo.</div>
                    <div *ngFor="let c of m.eventosPorTipo" class="si2fa-bar">
                      <span class="n" [title]="c.clave">{{ etiquetaEvento(c.clave) }}</span>
                      <span class="track"><span [style.width.%]="pct(c.total, maxEvento)"></span></span>
                      <span class="t">{{ c.total }}</span>
                    </div>
                  </div>
                </div>
              </div>

              <div class="ui-col-12">
                <div class="ui-panel" style="margin:0">
                  <div class="ui-panel__head"><div class="ui-panel__title" style="font-size:1rem"><i class="fa-solid fa-chart-column"></i> Verificaciones por día</div></div>
                  <div class="ui-panel__body">
                    <div *ngIf="!m.serieDiaria.length" class="si2fa-muted">Sin datos en el periodo.</div>
                    <div *ngFor="let d of m.serieDiaria" class="si2fa-bar">
                      <span class="n">{{ d.fecha }}</span>
                      <span class="track">
                        <span style="background:#16a34a" [style.width.%]="pct(d.verificacionesOk, maxDia)"></span>
                        <span style="background:#dc2626" [style.width.%]="pct(d.verificacionesFallidas, maxDia)"></span>
                      </span>
                      <span class="t">{{ d.total }}</span>
                    </div>
                    <div style="margin-top:.5rem">
                      <span class="si2fa-badge green">OK</span> &nbsp; <span class="si2fa-badge red">Fallidas</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </ng-container>
        </div>
      </div>
    </section>
  `
})
export class Si2faDashboardComponent implements OnInit, OnDestroy {
  m: Metricas2Fa | null = null;
  dias = 30;
  loading = false;

  maxSistema = 1; maxEvento = 1; maxDia = 1;

  // Monitoreo de salud del servicio 2FA en tiempo real.
  salud: Salud2Fa | null = null;
  saludConsultada = false;
  ultimaVerificacion: Date | null = null;
  readonly saludIntervaloSeg = 15;
  private saludTimer: any = null;

  constructor(private service: Si2faService, private toast: ToastService) {}

  ngOnInit(): void {
    this.cargar();
    this.consultarSalud();
    this.saludTimer = setInterval(() => this.consultarSalud(), this.saludIntervaloSeg * 1000);
  }

  ngOnDestroy(): void {
    if (this.saludTimer) { clearInterval(this.saludTimer); this.saludTimer = null; }
  }

  consultarSalud(): void {
    this.service.getSalud().subscribe({
      next: (s) => {
        this.salud = s;
        this.saludConsultada = true;
        this.ultimaVerificacion = new Date();
      },
      error: () => {
        this.salud = { up: false, estado: 'CAIDO', latenciaMs: 0, mensaje: 'No fue posible contactar el servicio.', consultado: new Date().toISOString() };
        this.saludConsultada = true;
        this.ultimaVerificacion = new Date();
      }
    });
  }

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

  etiquetaEvento = etiquetaEvento;
}
