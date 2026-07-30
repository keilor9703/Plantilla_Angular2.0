import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditoriaItem, AuditoriaService } from '../../../core/services/administracion/auditoria.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-auditoria-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auditoria.html',
  styleUrls: ['./auditoria.scss'],
})
export class AuditoriaAdminComponent {
  loading = false;
  filtros = {
    fechaDesde: '',
    fechaHasta: '',
    usuario: '',
    modulo: '',
    accion: '',
  };

  paginaActual = 1;
  pageSize = 10;
  total = 0;

  readonly acciones = ['CREAR', 'ACTUALIZAR', 'GUARDAR', 'ELIMINAR', 'INACTIVAR', 'ACTIVAR', 'ASIGNAR ROL', 'ELIMINAR ROL', 'EDITAR ROL', 'DESACTIVAR', 'LOGIN', 'CONSULTAR'];
  readonly modulos = ['USUARIOS', 'ROLES', 'MENU', 'DOMINIO', 'PERSONAJE DEL MES', 'LINEA DE MANDO', 'CUENTAS CORREO'];

  items: AuditoriaItem[] = [];

  constructor(
    private auditoriaService: AuditoriaService,
    private toast: ToastService
  ) {
    this.consultar();
  }

  get totalPaginas(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }

  /**
   * Page numbers generator for auditoria pagination.
   * Shows 1..10, '...', last when pages > 11.
   */
  get pageNumbers(): Array<number | string> {
    const total = this.totalPaginas;
    const current = this.paginaActual;
    const window = 1; // pages before/after

    if (total <= window * 2 + 2) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    const pages: Array<number | string> = [];
    pages.push(1);

    let start = Math.max(2, current - window);
    let end = Math.min(total - 1, current + window);

    if (start > 2) pages.push('...');

    for (let i = start; i <= end; i++) pages.push(i);

    if (end < total - 1) pages.push('...');

    pages.push(total);
    return pages;
  }

  goToPage(page: number): void {
    if (!Number.isFinite(page) || page < 1 || page > this.totalPaginas) return;
    if (this.paginaActual === page) return;
    this.paginaActual = page;
    this.consultar();
  }

  onPageClick(p: number | string): void {
    if (p === '...') return;
    const n = Number(p);
    if (Number.isFinite(n)) this.goToPage(n);
  }

  filtrar(): void {
    this.paginaActual = 1;
    this.consultar();
  }

  limpiarFiltros(): void {
    this.filtros = { fechaDesde: '', fechaHasta: '', usuario: '', modulo: '', accion: '' };
    this.paginaActual = 1;
    this.consultar();
  }

  anterior(): void {
    if (this.paginaActual > 1) {
      this.paginaActual--;
      this.consultar();
    }
  }

  siguiente(): void {
    if (this.paginaActual < this.totalPaginas) {
      this.paginaActual++;
      this.consultar();
    }
  }

  private consultar(): void {
    this.loading = true;
    this.auditoriaService.getBitacora({
      fechaDesde: this.filtros.fechaDesde || undefined,
      fechaHasta: this.filtros.fechaHasta || undefined,
      usuario: this.filtros.usuario?.trim() || undefined,
      modulo: this.filtros.modulo || undefined,
      accion: this.filtros.accion || undefined,
      page: this.paginaActual,
      pageSize: this.pageSize
    }).subscribe({
      next: (resp) => {
        this.items = resp?.items ?? [];
        this.total = Number(resp?.total ?? 0);
        this.loading = false;
      },
      error: () => {
        this.items = [];
        this.total = 0;
        this.loading = false;
        this.toast.error('Auditoría', 'No fue posible cargar la bitácora.');
      }
    });
  }
}
