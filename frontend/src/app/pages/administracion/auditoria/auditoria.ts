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

  readonly acciones = ['CREAR', 'ACTUALIZAR', 'ELIMINAR', 'INACTIVAR', 'ACTIVAR', 'LOGIN', 'CONSULTAR'];
  readonly modulos = ['Noticias', 'Modales', 'Radio', 'Usuarios', 'Menú'];

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
