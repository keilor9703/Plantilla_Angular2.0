import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Si2faService, Usuario2Fa, Usuario2FaDetalle } from '../../../core/services/si2fa/si2fa.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-si2fa-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <div class="si2fa-header">
      <div>
        <h1><i class="fa-solid fa-user-lock"></i> Usuarios 2FA</h1>
        <div class="sub">Gestión centralizada del segundo factor de los funcionarios</div>
      </div>
    </div>

    <div class="toolbar">
      <input [(ngModel)]="filtro" (keyup.enter)="buscar()" placeholder="Identificación o usuario institucional…" style="min-width:280px" />
      <button class="btn primary" (click)="buscar()"><i class="fa-solid fa-magnifying-glass"></i> Buscar</button>
      <button class="btn secondary" (click)="filtro=''; buscar()">Limpiar</button>
    </div>

    <div *ngIf="loading" class="loading"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

    <div *ngIf="!loading" class="table-wrap">
      <table class="tbl">
        <thead>
          <tr><th>Identificación</th><th>Usuario</th><th>MFA</th><th>Enrolado</th><th>Intentos</th><th>Estado</th><th>Disp.</th><th>Último OK</th><th>Acciones</th></tr>
        </thead>
        <tbody>
          <tr *ngFor="let u of usuarios">
            <td>{{ u.identificacion }}</td>
            <td>{{ u.usuarioInstitucional }}</td>
            <td><span class="badge" [ngClass]="u.mfaHabilitado ? 'green' : 'gray'">{{ u.mfaHabilitado ? 'Sí' : 'No' }}</span></td>
            <td>
              <span class="badge" [ngClass]="u.enrolado ? 'green' : 'amber'">{{ u.enrolado ? 'Sí' : 'Pendiente' }}</span>
              <span *ngIf="u.requireReenroll" class="badge amber" title="Requiere re-enrolamiento">RE</span>
            </td>
            <td>{{ u.intentosFallidos }}</td>
            <td><span class="badge" [ngClass]="u.bloqueado ? 'red' : 'green'">{{ u.bloqueado ? 'Bloqueado' : 'OK' }}</span></td>
            <td>{{ u.dispositivosConfiables }}</td>
            <td>{{ u.ultimoOk ? (u.ultimoOk | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
            <td>
              <div class="row-actions">
                <button class="btn secondary sm" (click)="verDetalle(u)"><i class="fa-solid fa-eye"></i></button>
                <button class="btn success sm" *ngIf="u.bloqueado" (click)="accion(u,'desbloquear')">Desbloquear</button>
                <button class="btn sm" [ngClass]="u.mfaHabilitado ? 'warn' : 'success'" (click)="toggleMfa(u)">
                  {{ u.mfaHabilitado ? 'Deshabilitar' : 'Habilitar' }}
                </button>
                <button class="btn danger sm" (click)="confirmarReset(u)"><i class="fa-solid fa-arrows-rotate"></i> Reset</button>
              </div>
            </td>
          </tr>
          <tr *ngIf="!usuarios.length"><td colspan="9" class="empty">Sin resultados.</td></tr>
        </tbody>
      </table>
    </div>

    <div style="margin-top:.8rem;display:flex;gap:.5rem;align-items:center" *ngIf="!loading">
      <button class="btn secondary sm" [disabled]="pagina<=1" (click)="pagina=pagina-1; buscar()">Anterior</button>
      <span style="font-size:.85rem">Página {{ pagina }}</span>
      <button class="btn secondary sm" [disabled]="usuarios.length<tamano" (click)="pagina=pagina+1; buscar()">Siguiente</button>
    </div>

    <!-- Detalle -->
    <div class="modal-backdrop" *ngIf="detalle" (click)="detalle=null">
      <div class="modal" (click)="$event.stopPropagation()">
        <h3>Detalle del usuario</h3>
        <p style="margin:.2rem 0"><strong>{{ detalle.usuarioInstitucional }}</strong> &middot; ID {{ detalle.identificacion }}</p>
        <div class="kpi-grid" style="margin:.5rem 0">
          <div class="card kpi"><div class="value">{{ detalle.intentosFallidos }}</div><div class="label">Intentos fallidos</div></div>
          <div class="card kpi"><div class="value">{{ detalle.dispositivos.length }}</div><div class="label">Dispositivos</div></div>
        </div>
        <h3 style="font-size:.9rem">Dispositivos confiables</h3>
        <div class="table-wrap" style="max-height:220px">
          <table class="tbl">
            <thead><tr><th>Hash</th><th>Expira</th><th>Vigente</th><th></th></tr></thead>
            <tbody>
              <tr *ngFor="let d of detalle.dispositivos">
                <td style="font-family:monospace;font-size:.75rem">{{ (d.deviceHash || '').slice(0,16) }}…</td>
                <td>{{ d.expiraAt ? (d.expiraAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
                <td><span class="badge" [ngClass]="d.vigente ? 'green' : 'gray'">{{ d.vigente ? 'Sí' : 'No' }}</span></td>
                <td><button class="btn danger sm" (click)="revocar(d.idDevice)">Revocar</button></td>
              </tr>
              <tr *ngIf="!detalle.dispositivos.length"><td colspan="4" class="empty">Sin dispositivos.</td></tr>
            </tbody>
          </table>
        </div>
        <div class="modal-actions">
          <button class="btn warn" (click)="accion(detalle,'forzar')">Forzar re-enrolamiento</button>
          <button class="btn secondary" (click)="accion(detalle,'limpiar')">Limpiar dispositivos</button>
          <button class="btn secondary" (click)="detalle=null">Cerrar</button>
        </div>
      </div>
    </div>
  `
})
export class Si2faUsuariosComponent implements OnInit {
  usuarios: Usuario2Fa[] = [];
  detalle: Usuario2FaDetalle | null = null;
  filtro = '';
  pagina = 1;
  tamano = 50;
  loading = false;

  constructor(private service: Si2faService, private toast: ToastService) {}

  ngOnInit(): void { this.buscar(); }

  buscar(): void {
    if (this.pagina < 1) this.pagina = 1;
    this.loading = true;
    this.service.getUsuarios(this.filtro, this.pagina, this.tamano).subscribe({
      next: (d) => { this.usuarios = d; this.loading = false; },
      error: () => { this.loading = false; this.toast.error('Error', 'No fue posible cargar los usuarios.'); }
    });
  }

  verDetalle(u: Usuario2Fa): void {
    this.service.getUsuario(u.idMfa).subscribe({
      next: (d) => this.detalle = d,
      error: () => this.toast.error('Error', 'No fue posible cargar el detalle.')
    });
  }

  toggleMfa(u: Usuario2Fa): void {
    this.service.cambiarEstadoMfa(u.idMfa, u.mfaHabilitado ? 0 : 1).subscribe({
      next: (r) => { this.toast.success('Listo', r.message || 'Estado actualizado.'); this.buscar(); },
      error: () => this.toast.error('Error', 'No se pudo cambiar el estado.')
    });
  }

  confirmarReset(u: Usuario2Fa): void {
    if (!confirm(`¿Restablecer el MFA de ${u.usuarioInstitucional}? El usuario deberá volver a enrolarse.`)) return;
    this.service.restablecer(u.idMfa).subscribe({
      next: (r) => { this.toast.success('Listo', r.message || 'MFA restablecido.'); this.buscar(); },
      error: () => this.toast.error('Error', 'No se pudo restablecer.')
    });
  }

  accion(u: Usuario2Fa, tipo: 'desbloquear' | 'forzar' | 'limpiar'): void {
    const call = tipo === 'desbloquear' ? this.service.desbloquear(u.idMfa)
      : tipo === 'forzar' ? this.service.forzarReenrolamiento(u.idMfa)
      : this.service.limpiarDispositivos(u.idMfa);
    call.subscribe({
      next: (r) => {
        this.toast.success('Listo', r.message || 'Acción aplicada.');
        if (this.detalle && this.detalle.idMfa === u.idMfa) this.verDetalle(u);
        this.buscar();
      },
      error: () => this.toast.error('Error', 'No se pudo completar la acción.')
    });
  }

  revocar(idDevice: number): void {
    this.service.revocarDispositivo(idDevice).subscribe({
      next: (r) => { this.toast.success('Listo', r.message || 'Dispositivo revocado.'); if (this.detalle) this.verDetalle(this.detalle); this.buscar(); },
      error: () => this.toast.error('Error', 'No se pudo revocar.')
    });
  }
}
