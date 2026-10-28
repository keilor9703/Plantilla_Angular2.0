import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, Parametro2Fa } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

interface GrupoParam { nombre: string; items: Parametro2Fa[]; }

@Component({
  selector: 'app-si2fa-parametros',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <section class="home-view">
      <div class="ui-panel">
        <div class="ui-panel__head">
          <div class="ui-panel__title"><i class="fa-solid fa-sliders"></i> Parámetros del servicio</div>
          <button class="ui-btn secondary" (click)="cargar()"><i class="fa-solid fa-rotate"></i> Recargar</button>
        </div>
        <div class="ui-panel__subtitle">Configuración operativa del servicio de doble autenticación</div>

        <div class="ui-panel__body">
          <div *ngIf="loading" class="si2fa-muted"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

          <ng-container *ngIf="!loading">
            <div *ngFor="let g of grupos" style="margin-bottom:18px">
              <div class="ui-label" style="font-size:calc(13px * var(--font-size-scale,1));margin-bottom:8px">{{ g.nombre }}</div>
              <div class="ui-table-wrap">
                <table class="ui-table">
                  <thead>
                    <tr><th style="width:26%">Parámetro</th><th style="width:22%">Valor</th><th>Descripción</th><th style="width:140px">Rango</th><th style="width:120px"></th></tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let p of g.items">
                      <td><strong>{{ p.clave }}</strong></td>
                      <td>
                        <div style="display:flex;align-items:center;gap:8px">
                          <input class="ui-control" style="max-width:120px"
                                 [type]="p.tipo === 'INT' ? 'number' : 'text'"
                                 [(ngModel)]="p.valor"
                                 [min]="p.minValor" [max]="p.maxValor" />
                          <span class="si2fa-muted" *ngIf="p.unidad">{{ p.unidad }}</span>
                        </div>
                      </td>
                      <td>{{ p.descripcion }}</td>
                      <td class="si2fa-muted">
                        <ng-container *ngIf="p.minValor != null || p.maxValor != null">{{ p.minValor }} – {{ p.maxValor }}</ng-container>
                      </td>
                      <td>
                        <button class="ui-btn si2fa-sm primary" [disabled]="savingKey === p.clave" (click)="guardar(p)">
                          <i class="fa-solid fa-spinner fa-spin" *ngIf="savingKey === p.clave"></i> Guardar
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div class="si2fa-muted" style="margin-top:6px">
              <i class="fa-solid fa-circle-info"></i>
              Los cambios se guardan de inmediato. El servicio 2FA los aplica dentro de su intervalo de recarga (por defecto ~60 s).
            </div>
          </ng-container>
        </div>
      </div>
    </section>
  `
})
export class Si2faParametrosComponent implements OnInit {
  grupos: GrupoParam[] = [];
  loading = false;
  savingKey: string | null = null;

  constructor(private service: Si2faService, private toast: ToastService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading = true;
    this.service.getParametros().subscribe({
      next: (items) => { this.grupos = this.agrupar(items); this.loading = false; },
      error: () => { this.loading = false; this.toast.error('Error', 'No fue posible cargar los parámetros.'); }
    });
  }

  guardar(p: Parametro2Fa): void {
    this.savingKey = p.clave;
    this.service.actualizarParametro(p.clave, String(p.valor)).subscribe({
      next: (r) => {
        this.savingKey = null;
        if (!r.success) { this.toast.error('Error', r.message || 'No se pudo actualizar.'); return; }
        this.toast.success('Listo', r.message || 'Parámetro actualizado.');
      },
      error: (e) => { this.savingKey = null; this.toast.error('Error', e?.error?.message || 'No se pudo actualizar.'); }
    });
  }

  private agrupar(items: Parametro2Fa[]): GrupoParam[] {
    const map = new Map<string, Parametro2Fa[]>();
    for (const it of items) {
      const g = it.grupo || 'General';
      if (!map.has(g)) map.set(g, []);
      map.get(g)!.push(it);
    }
    return Array.from(map.entries()).map(([nombre, items]) => ({ nombre, items }));
  }
}
