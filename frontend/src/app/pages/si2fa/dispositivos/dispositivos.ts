import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Si2faCelularesComponent } from '../celulares/celulares';
import { Si2faEquiposConfiablesComponent } from './equipos-confiables';

/**
 * Módulo único «Dispositivos y accesos». Separa con claridad dos cosas distintas:
 *  - Celulares de la app: el ingreso SIN contraseña (QR y notificación). Revocarlos bloquea ese ingreso.
 *  - Equipos confiables: navegadores que omiten el código TOTP. Quitarlos vuelve a pedir el código.
 * El restablecimiento del TOTP del funcionario vive en «Usuarios 2FA».
 */
@Component({
  selector: 'app-si2fa-dispositivos',
  standalone: true,
  imports: [CommonModule, RouterModule, Si2faCelularesComponent, Si2faEquiposConfiablesComponent],
  styleUrls: ['../si2fa-shared.scss'],
  template: `
    <section class="home-view">
      <div class="ui-panel">
        <div class="ui-panel__head">
          <div class="ui-panel__title"><i class="fa-solid fa-mobile-screen"></i> Dispositivos y accesos</div>
        </div>
        <div class="ui-panel__subtitle">Qué celulares y equipos pueden ingresar por funcionario, y cómo quitarles el acceso.</div>

        <div class="ui-panel__body">
          <div class="si2fa-tabs" style="display:flex;gap:8px;margin-bottom:14px;flex-wrap:wrap">
            <button class="ui-btn" [ngClass]="tab === 'celulares' ? 'primary' : 'outline'" (click)="tab = 'celulares'">
              <i class="fa-solid fa-mobile-screen-button"></i> Celulares de la app
            </button>
            <button class="ui-btn" [ngClass]="tab === 'equipos' ? 'primary' : 'outline'" (click)="tab = 'equipos'">
              <i class="fa-solid fa-laptop"></i> Equipos confiables
            </button>
          </div>

          <p class="si2fa-muted" *ngIf="tab === 'celulares'" style="margin-bottom:14px">
            <b>Ingreso sin contraseña (QR y notificación).</b> Revocar un celular bloquea de inmediato su ingreso con la app y el
            funcionario deberá registrarlo de nuevo. <b>No cambia</b> su contraseña ni su código TOTP; para eso use
            <a routerLink="/si2fa/usuarios">Usuarios 2FA</a>.
          </p>
          <p class="si2fa-muted" *ngIf="tab === 'equipos'" style="margin-bottom:14px">
            <b>Navegadores que omiten el código TOTP</b> («Confiar en este equipo»). Quitar la confianza hace que ese equipo
            vuelva a pedir el código. No afecta al celular de la app. Para restablecer el TOTP use
            <a routerLink="/si2fa/usuarios">Usuarios 2FA</a>.
          </p>

          <app-si2fa-celulares *ngIf="tab === 'celulares'"></app-si2fa-celulares>
          <app-si2fa-equipos-confiables *ngIf="tab === 'equipos'"></app-si2fa-equipos-confiables>
        </div>
      </div>
    </section>
  `
})
export class Si2faDispositivosComponent {
  tab: 'celulares' | 'equipos' = 'celulares';
}
