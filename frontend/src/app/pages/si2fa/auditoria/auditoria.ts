import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, Auditoria2Fa, Auditoria2FaFiltro } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-si2fa-auditoria',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <section class="home-view">
      <div class="ui-panel">
        <div class="ui-panel__head">
          <div class="ui-panel__title"><i class="fa-solid fa-clipboard-list"></i> Auditoría 2FA</div>
          <button class="ui-btn secondary" (click)="exportar()" [disabled]="exportando">
            <i class="fa-solid fa-file-csv"></i> Exportar CSV
          </button>
        </div>
        <div class="ui-panel__subtitle">Trazabilidad de los eventos del servicio de doble autenticación</div>

        <div class="ui-panel__body">
          <div class="ui-grid" style="margin-bottom:14px">
            <div class="ui-field ui-col-3">
              <label class="ui-label">Identificación</label>
              <input class="ui-control" [(ngModel)]="f.identificacion" type="number" placeholder="Identificación" />
            </div>
            <div class="ui-field ui-col-3">
              <label class="ui-label">Usuario</label>
              <input class="ui-control" [(ngModel)]="f.usuario" placeholder="Usuario" />
            </div>
            <div class="ui-field ui-col-3">
              <label class="ui-label">Sistema</label>
              <input class="ui-control" [(ngModel)]="f.sistema" placeholder="Sistema" />
            </div>
            <div class="ui-field ui-col-3">
              <label class="ui-label">Evento</label>
              <input class="ui-control" [(ngModel)]="f.evento" placeholder="p. ej. VERIFY_FAIL" />
            </div>
            <div class="ui-field ui-col-3">
              <label class="ui-label">Desde</label>
              <input class="ui-control" [(ngModel)]="f.desde" type="datetime-local" />
            </div>
            <div class="ui-field ui-col-3">
              <label class="ui-label">Hasta</label>
              <input class="ui-control" [(ngModel)]="f.hasta" type="datetime-local" />
            </div>
            <div class="ui-field ui-col-6" style="flex-direction:row;align-items:flex-end;gap:10px">
              <button class="ui-btn primary" (click)="pagina=1; buscar()"><i class="fa-solid fa-magnifying-glass"></i> Buscar</button>
              <button class="ui-btn" (click)="limpiar()">Limpiar</button>
            </div>
          </div>

          <div class="si2fa-muted" style="margin-bottom:8px">{{ total }} registro(s)</div>

          <div *ngIf="loading" class="si2fa-muted"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

          <div *ngIf="!loading" class="ui-table-wrap">
            <table class="ui-table">
              <thead>
                <tr><th>Fecha</th><th>Identificación</th><th>Usuario</th><th>Sistema</th><th>Evento</th><th>Detalle</th><th>IP</th></tr>
              </thead>
              <tbody>
                <tr *ngFor="let a of items">
                  <td style="white-space:nowrap">{{ a.creadoAt ? (a.creadoAt | date:'yyyy-MM-dd HH:mm:ss') : '' }}</td>
                  <td>{{ a.identificacion }}</td>
                  <td>{{ a.usuarioInstitucional }}</td>
                  <td>{{ a.sistema }}</td>
                  <td><span class="si2fa-badge" [ngClass]="eventoClase(a.evento)">{{ a.evento }}</span></td>
                  <td style="max-width:360px">{{ a.detalle }}</td>
                  <td>{{ a.ipOrigen }}</td>
                </tr>
                <tr *ngIf="!items.length"><td colspan="7" class="empty-row">Sin registros.</td></tr>
              </tbody>
            </table>
          </div>

          <div *ngIf="!loading" style="margin-top:14px;display:flex;gap:10px;align-items:center">
            <button class="ui-btn si2fa-sm" [disabled]="pagina<=1" (click)="pagina=pagina-1; buscar()">Anterior</button>
            <span class="si2fa-muted">Página {{ pagina }} de {{ totalPaginas }}</span>
            <button class="ui-btn si2fa-sm" [disabled]="pagina>=totalPaginas" (click)="pagina=pagina+1; buscar()">Siguiente</button>
          </div>
        </div>
      </div>
    </section>
  `
})
export class Si2faAuditoriaComponent implements OnInit {
  items: Auditoria2Fa[] = [];
  f: Auditoria2FaFiltro = {};
  pagina = 1;
  tamano = 50;
  total = 0;
  loading = false;
  exportando = false;

  constructor(private service: Si2faService, private toast: ToastService) {}

  get totalPaginas(): number { return Math.max(1, Math.ceil(this.total / this.tamano)); }

  ngOnInit(): void { this.buscar(); }

  buscar(): void {
    this.loading = true;
    const filtro: Auditoria2FaFiltro = { ...this.f, pagina: this.pagina, tamano: this.tamano };
    this.service.getAuditoria(filtro).subscribe({
      next: (r) => { this.items = r.items; this.total = r.total; this.loading = false; },
      error: () => { this.loading = false; this.toast.error('Error', 'No fue posible consultar la auditoría.'); }
    });
  }

  limpiar(): void { this.f = {}; this.pagina = 1; this.buscar(); }

  exportar(): void {
    this.exportando = true;
    this.service.exportarAuditoria(this.f).subscribe({
      next: (blob) => {
        this.exportando = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `auditoria_si2fa_${new Date().toISOString().slice(0,19).replace(/[:T]/g,'')}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => { this.exportando = false; this.toast.error('Error', 'No fue posible exportar.'); }
    });
  }

  eventoClase(ev?: string): string {
    if (!ev) return 'gray';
    if (ev.includes('FAIL') || ev.includes('RATELIMIT')) return 'red';
    if (ev.includes('OK') || ev === 'ENROLL_OK') return 'green';
    if (ev.startsWith('ADMIN') || ev.startsWith('RESET')) return 'amber';
    return 'gray';
  }
}
