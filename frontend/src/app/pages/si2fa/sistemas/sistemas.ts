import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, Sistema2Fa, Sistema2FaRequest } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-si2fa-sistemas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <section class="home-view">
      <div class="ui-panel">
        <div class="ui-panel__head">
          <div class="ui-panel__title"><i class="fa-solid fa-plug-circle-check"></i> Sistemas Integrados</div>
          <button class="ui-btn primary" (click)="abrirCrear()"><i class="fa-solid fa-plus"></i> Nuevo sistema</button>
        </div>
        <div class="ui-panel__subtitle">Sistemas de información autorizados a consumir el servicio 2FA</div>

        <div class="ui-panel__body">
          <div *ngIf="loading" class="si2fa-muted"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

          <div *ngIf="!loading" class="ui-table-wrap">
            <table class="ui-table">
              <thead>
                <tr><th>Sigla</th><th>Nombre</th><th>Descripción</th><th>Estado</th><th>Secreto</th><th>Últ. rotación</th><th>Acciones</th></tr>
              </thead>
              <tbody>
                <tr *ngFor="let s of sistemas">
                  <td><strong>{{ s.sigla }}</strong></td>
                  <td>{{ s.nombre }}</td>
                  <td>{{ s.descripcion }}</td>
                  <td><span class="si2fa-badge" [ngClass]="s.habilitado ? 'green' : 'gray'">{{ s.habilitado ? 'Activo' : 'Inactivo' }}</span></td>
                  <td class="si2fa-mono si2fa-muted">{{ s.secretoEnmascarado }}</td>
                  <td>{{ s.rotadoAt ? (s.rotadoAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
                  <td>
                    <div class="si2fa-actions">
                      <button class="ui-btn si2fa-sm" (click)="abrirEditar(s)"><i class="fa-solid fa-pen"></i></button>
                      <button class="ui-btn si2fa-sm" [ngClass]="s.habilitado ? 'outline' : 'primary'" (click)="toggle(s)">
                        {{ s.habilitado ? 'Deshabilitar' : 'Habilitar' }}
                      </button>
                      <button class="ui-btn si2fa-sm info" (click)="rotar(s)"><i class="fa-solid fa-key"></i> Rotar</button>
                    </div>
                  </td>
                </tr>
                <tr *ngIf="!sistemas.length"><td colspan="7" class="empty-row">No hay sistemas registrados.</td></tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </section>

    <!-- Modal crear/editar -->
    <div class="si2fa-backdrop" *ngIf="showForm" (click)="cerrar()">
      <div class="ui-panel si2fa-dialog" (click)="$event.stopPropagation()">
        <div class="ui-panel__head"><div class="ui-panel__title" style="font-size:1rem">{{ editId ? 'Editar sistema' : 'Nuevo sistema' }}</div></div>
        <div class="ui-panel__body">
          <div class="ui-field" style="margin-bottom:12px">
            <label class="ui-label">Sigla (identificador usado para firmar) *</label>
            <input class="ui-control" [(ngModel)]="form.sigla" [disabled]="!!editId" placeholder="Ej: SECADSISEC" />
            <small class="si2fa-muted" *ngIf="editId">La sigla no se puede modificar.</small>
          </div>
          <div class="ui-field" style="margin-bottom:12px">
            <label class="ui-label">Nombre *</label>
            <input class="ui-control" [(ngModel)]="form.nombre" placeholder="Nombre del sistema de información" />
          </div>
          <div class="ui-field" style="margin-bottom:12px">
            <label class="ui-label">Descripción</label>
            <textarea class="ui-control" [(ngModel)]="form.descripcion" rows="2"></textarea>
          </div>
          <div class="ui-field" style="margin-bottom:12px">
            <label class="ui-label">Estado</label>
            <select class="ui-control" [(ngModel)]="form.habilitado">
              <option [ngValue]="1">Activo</option>
              <option [ngValue]="0">Inactivo</option>
            </select>
          </div>
          <div style="display:flex;justify-content:flex-end;gap:10px;margin-top:8px">
            <button class="ui-btn" (click)="cerrar()">Cancelar</button>
            <button class="ui-btn primary" [disabled]="saving" (click)="guardar()">
              <i class="fa-solid fa-spinner fa-spin" *ngIf="saving"></i> Guardar
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal secreto (una sola vez) -->
    <div class="si2fa-backdrop" *ngIf="secretoVisible" (click)="secretoVisible=null">
      <div class="ui-panel si2fa-dialog" (click)="$event.stopPropagation()">
        <div class="ui-panel__head"><div class="ui-panel__title" style="font-size:1rem"><i class="fa-solid fa-triangle-exclamation" style="color:#d97706"></i> Secreto HMAC</div></div>
        <div class="ui-panel__body">
          <p>Copie este secreto y entréguelo de forma segura al sistema integrado. <strong>No se volverá a mostrar.</strong></p>
          <div class="si2fa-secret">{{ secretoVisible }}</div>
          <div style="display:flex;justify-content:flex-end;gap:10px;margin-top:12px">
            <button class="ui-btn" (click)="copiar(secretoVisible!)"><i class="fa-solid fa-copy"></i> Copiar</button>
            <button class="ui-btn primary" (click)="secretoVisible=null">Entendido</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class Si2faSistemasComponent implements OnInit {
  sistemas: Sistema2Fa[] = [];
  loading = false;
  saving = false;

  showForm = false;
  editId: number | null = null;
  form: Sistema2FaRequest = { sigla: '', nombre: '', descripcion: '', habilitado: 1 };

  secretoVisible: string | null = null;

  constructor(private service: Si2faService, private toast: ToastService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading = true;
    this.service.getSistemas().subscribe({
      next: (d) => { this.sistemas = d; this.loading = false; },
      error: () => { this.loading = false; this.toast.error('Error', 'No fue posible cargar los sistemas.'); }
    });
  }

  abrirCrear(): void {
    this.editId = null;
    this.form = { sigla: '', nombre: '', descripcion: '', habilitado: 1 };
    this.showForm = true;
  }
  abrirEditar(s: Sistema2Fa): void {
    this.editId = s.idSistema;
    this.form = { sigla: s.sigla, nombre: s.nombre, descripcion: s.descripcion || '', habilitado: s.habilitado };
    this.showForm = true;
  }
  cerrar(): void { this.showForm = false; }

  guardar(): void {
    if (!this.form.nombre?.trim() || (!this.editId && !this.form.sigla?.trim())) {
      this.toast.warning('Validación', 'La sigla y el nombre son obligatorios.');
      return;
    }
    this.saving = true;
    const obs = this.editId
      ? this.service.actualizarSistema(this.editId, this.form)
      : this.service.crearSistema(this.form);
    obs.subscribe({
      next: (r) => {
        this.saving = false;
        if (!r.success) { this.toast.error('Error', r.message || 'No se pudo guardar.'); return; }
        this.toast.success('Listo', r.message || 'Sistema guardado.');
        this.showForm = false;
        if (r.secreto) this.secretoVisible = r.secreto;
        this.cargar();
      },
      error: (e) => { this.saving = false; this.toast.error('Error', e?.error?.message || 'No se pudo guardar.'); }
    });
  }

  toggle(s: Sistema2Fa): void {
    this.service.cambiarEstadoSistema(s.idSistema, s.habilitado ? 0 : 1).subscribe({
      next: (r) => { this.toast.success('Listo', r.message || 'Estado actualizado.'); this.cargar(); },
      error: () => this.toast.error('Error', 'No se pudo cambiar el estado.')
    });
  }

  rotar(s: Sistema2Fa): void {
    if (!confirm(`¿Rotar el secreto del sistema "${s.sigla}"? El secreto anterior dejará de funcionar.`)) return;
    this.service.rotarSecreto(s.idSistema).subscribe({
      next: (r) => {
        if (r.secreto) this.secretoVisible = r.secreto;
        this.toast.success('Listo', 'Secreto rotado.');
        this.cargar();
      },
      error: () => this.toast.error('Error', 'No se pudo rotar el secreto.')
    });
  }

  copiar(txt: string): void {
    navigator.clipboard?.writeText(txt).then(
      () => this.toast.info('Copiado', 'Secreto copiado al portapapeles.'),
      () => {}
    );
  }
}
