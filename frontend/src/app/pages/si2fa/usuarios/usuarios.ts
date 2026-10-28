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
    <section class="home-view">
      <div class="ui-panel">
        <div class="ui-panel__head">
          <div class="ui-panel__title"><i class="fa-solid fa-user-lock"></i> Usuarios 2FA</div>
        </div>
        <div class="ui-panel__subtitle">Gestión centralizada del segundo factor de los funcionarios</div>

        <div class="ui-panel__body">
          <div class="si2fa-toolbar-row" style="margin-bottom:14px">
            <div class="ui-field" style="flex:1;min-width:260px">
              <label class="ui-label">Buscar</label>
              <input class="ui-control" [(ngModel)]="filtro" (keyup.enter)="buscar()" placeholder="Identificación o usuario institucional…" />
            </div>
            <button class="ui-btn primary" (click)="buscar()"><i class="fa-solid fa-magnifying-glass"></i> Buscar</button>
            <button class="ui-btn" (click)="filtro=''; buscar()">Limpiar</button>
          </div>

          <div *ngIf="loading" class="si2fa-muted"><i class="fa-solid fa-spinner fa-spin"></i> Cargando…</div>

          <div *ngIf="!loading" class="ui-table-wrap">
            <table class="ui-table">
              <thead>
                <tr><th>Identificación</th><th>Usuario</th><th>MFA</th><th>Enrolado</th><th>Intentos</th><th>Estado</th><th>Disp.</th><th>Último OK</th><th>Acciones</th></tr>
              </thead>
              <tbody>
                <tr *ngFor="let u of usuarios">
                  <td>{{ u.identificacion }}</td>
                  <td>{{ u.usuarioInstitucional }}</td>
                  <td><span class="si2fa-badge" [ngClass]="u.mfaHabilitado ? 'green' : 'gray'">{{ u.mfaHabilitado ? 'Sí' : 'No' }}</span></td>
                  <td>
                    <span class="si2fa-badge" [ngClass]="u.enrolado ? 'green' : 'amber'">{{ u.enrolado ? 'Sí' : 'Pendiente' }}</span>
                    <span *ngIf="u.requireReenroll" class="si2fa-badge amber" title="Requiere re-enrolamiento">RE</span>
                  </td>
                  <td>{{ u.intentosFallidos }}</td>
                  <td><span class="si2fa-badge" [ngClass]="u.bloqueado ? 'red' : 'green'">{{ u.bloqueado ? 'Bloqueado' : 'OK' }}</span></td>
                  <td>{{ u.dispositivosConfiables }}</td>
                  <td>{{ u.ultimoOk ? (u.ultimoOk | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
                  <td>
                    <div class="si2fa-actions">
                      <button class="ui-btn si2fa-sm" (click)="verDetalle(u)"><i class="fa-solid fa-eye"></i></button>
                      <button class="ui-btn si2fa-sm primary" *ngIf="u.bloqueado" (click)="accion(u,'desbloquear')">Desbloquear</button>
                      <button class="ui-btn si2fa-sm" [ngClass]="u.mfaHabilitado ? 'outline' : 'primary'" (click)="toggleMfa(u)">
                        {{ u.mfaHabilitado ? 'Deshabilitar' : 'Habilitar' }}
                      </button>
                      <button class="ui-btn si2fa-sm info" (click)="confirmarReset(u)"><i class="fa-solid fa-arrows-rotate"></i> Reset</button>
                    </div>
                  </td>
                </tr>
                <tr *ngIf="!usuarios.length"><td colspan="9" class="empty-row">Sin resultados.</td></tr>
              </tbody>
            </table>
          </div>

          <div *ngIf="!loading" style="margin-top:14px;display:flex;gap:10px;align-items:center">
            <button class="ui-btn si2fa-sm" [disabled]="pagina<=1" (click)="pagina=pagina-1; buscar()">Anterior</button>
            <span class="si2fa-muted">Página {{ pagina }}</span>
            <button class="ui-btn si2fa-sm" [disabled]="usuarios.length<tamano" (click)="pagina=pagina+1; buscar()">Siguiente</button>
          </div>
        </div>
      </div>
    </section>

    <!-- Detalle -->
    <div class="si2fa-backdrop" *ngIf="detalle" (click)="detalle=null">
      <div class="ui-panel si2fa-dialog" (click)="$event.stopPropagation()">
        <div class="ui-panel__head"><div class="ui-panel__title" style="font-size:1rem">Detalle del usuario</div></div>
        <div class="ui-panel__body">
          <p style="margin:.2rem 0"><strong>{{ detalle.usuarioInstitucional }}</strong> &middot; ID {{ detalle.identificacion }}</p>
          <div class="si2fa-kpis" style="margin:.5rem 0">
            <div class="si2fa-kpi"><div class="v">{{ detalle.intentosFallidos }}</div><div class="l">Intentos fallidos</div></div>
            <div class="si2fa-kpi"><div class="v">{{ detalle.dispositivos.length }}</div><div class="l">Dispositivos</div></div>
          </div>
          <div class="ui-label" style="margin-bottom:6px">Dispositivos confiables</div>
          <div class="ui-table-wrap" style="max-height:220px">
            <table class="ui-table">
              <thead><tr><th>Hash</th><th>Expira</th><th>Vigente</th><th></th></tr></thead>
              <tbody>
                <tr *ngFor="let d of detalle.dispositivos">
                  <td class="si2fa-mono">{{ (d.deviceHash || '').slice(0,16) }}…</td>
                  <td>{{ d.expiraAt ? (d.expiraAt | date:'yyyy-MM-dd HH:mm') : '—' }}</td>
                  <td><span class="si2fa-badge" [ngClass]="d.vigente ? 'green' : 'gray'">{{ d.vigente ? 'Sí' : 'No' }}</span></td>
                  <td><button class="ui-btn si2fa-sm info" (click)="revocar(d.idDevice)">Revocar</button></td>
                </tr>
                <tr *ngIf="!detalle.dispositivos.length"><td colspan="4" class="empty-row">Sin dispositivos.</td></tr>
              </tbody>
            </table>
          </div>
          <div style="display:flex;justify-content:flex-end;gap:10px;flex-wrap:wrap;margin-top:12px">
            <button class="ui-btn outline" (click)="accion(detalle,'forzar')">Forzar re-enrolamiento</button>
            <button class="ui-btn" (click)="accion(detalle,'limpiar')">Limpiar dispositivos</button>
            <button class="ui-btn secondary" (click)="detalle=null">Cerrar</button>
          </div>
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
