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
    <div class="si2fa-header">
      <div>
        <h1><i class="fa-solid fa-plug-circle-check"></i> Sistemas Integrados</h1>
        <div class="sub">Sistemas de información autorizados a consumir el servicio 2FA</div>
      </div>
      <button class="btn primary" (click)="abrirCrear()"><i class="fa-solid fa-plus"></i> Nuevo sistema</button>
    </div>

    <div *ngIf="loading" class="loading"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

    <div *ngIf="!loading" class="table-wrap">
      <table class="tbl">
        <thead>
          <tr><th>Sigla</th><th>Nombre</th><th>Descripción</th><th>Estado</th><th>Secreto</th><th>Últ. rotación</th><th>Acciones</th></tr>
        </thead>
        <tbody>
          <tr *ngFor="let s of sistemas">
            <td><strong>{{ s.sigla }}</strong></td>
            <td>{{ s.nombre }}</td>
            <td>{{ s.descripcion }}</td>
            <td>
              <span class="badge" [ngClass]="s.habilitado ? 'green' : 'gray'">{{ s.habilitado ? 'Activo' : 'Inactivo' }}</span>
            </td>
            <td style="font-family:monospace;color:#6b7280">{{ s.secretoEnmascarado }}</td>
            <td>{{ s.rotadoAt ? (s.rotadoAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
            <td>
              <div class="row-actions">
                <button class="btn secondary sm" (click)="abrirEditar(s)"><i class="fa-solid fa-pen"></i></button>
                <button class="btn sm" [ngClass]="s.habilitado ? 'warn' : 'success'" (click)="toggle(s)">
                  {{ s.habilitado ? 'Deshabilitar' : 'Habilitar' }}
                </button>
                <button class="btn danger sm" (click)="rotar(s)"><i class="fa-solid fa-key"></i> Rotar</button>
              </div>
            </td>
          </tr>
          <tr *ngIf="!sistemas.length"><td colspan="7" class="empty">No hay sistemas registrados.</td></tr>
        </tbody>
      </table>
    </div>

    <!-- Modal crear/editar -->
    <div class="modal-backdrop" *ngIf="showForm" (click)="cerrar()">
      <div class="modal" (click)="$event.stopPropagation()">
        <h3>{{ editId ? 'Editar sistema' : 'Nuevo sistema' }}</h3>
        <div class="form-group">
          <label>Sigla (identificador usado para firmar) *</label>
          <input [(ngModel)]="form.sigla" [disabled]="!!editId" placeholder="Ej: SECADSISEC" />
          <small *ngIf="editId" style="color:#6b7280">La sigla no se puede modificar.</small>
        </div>
        <div class="form-group">
          <label>Nombre *</label>
          <input [(ngModel)]="form.nombre" placeholder="Nombre del sistema de información" />
        </div>
        <div class="form-group">
          <label>Descripción</label>
          <textarea [(ngModel)]="form.descripcion" rows="2"></textarea>
        </div>
        <div class="form-group">
          <label>Estado</label>
          <select [(ngModel)]="form.habilitado">
            <option [ngValue]="1">Activo</option>
            <option [ngValue]="0">Inactivo</option>
          </select>
        </div>
        <div class="modal-actions">
          <button class="btn secondary" (click)="cerrar()">Cancelar</button>
          <button class="btn primary" [disabled]="saving" (click)="guardar()">
            <i class="fa-solid fa-spinner fa-spin" *ngIf="saving"></i> Guardar
          </button>
        </div>
      </div>
    </div>

    <!-- Modal secreto (una sola vez) -->
    <div class="modal-backdrop" *ngIf="secretoVisible" (click)="secretoVisible=null">
      <div class="modal" (click)="$event.stopPropagation()">
        <h3><i class="fa-solid fa-triangle-exclamation" style="color:#d97706"></i> Secreto HMAC</h3>
        <p style="font-size:.88rem">Copie este secreto y entréguelo de forma segura al sistema integrado.
          <strong>No se volverá a mostrar.</strong></p>
        <div class="secret-box">{{ secretoVisible }}</div>
        <div class="modal-actions">
          <button class="btn secondary" (click)="copiar(secretoVisible!)"><i class="fa-solid fa-copy"></i> Copiar</button>
          <button class="btn primary" (click)="secretoVisible=null">Entendido</button>
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
