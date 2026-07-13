import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  RecoleccionService,
  DtoHechoNoticioso,
  DtoAnioItem,
  DtoDominioCoest
} from '../../../core/services/centro-analisis/recoleccion.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './reportes.html',
  styleUrls: ['./reportes.scss']
})
export class ReportesComponent implements OnInit {
  loading = false;
  hechos: DtoHechoNoticioso[] = [];
  anios: DtoAnioItem[] = [];
  selectedAnio: number = new Date().getFullYear();
  searchQuery = '';

  // Filtros simples
  filterFechaDesde = '';
  filterFechaHasta = '';
  filterUnidad: number | null = null;
  unidades: DtoDominioCoest[] = [];

  constructor(
    private service: RecoleccionService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.cargarAnios();
    this.cargarHechos();
    this.cargarUnidades();
  }

  cargarHechos(): void {
    this.loading = true;
    this.service.getHechos(this.selectedAnio).subscribe({
      next: (data) => {
        this.hechos = data || [];
        this.loading = false;
      },
      error: () => {
        this.hechos = [];
        this.loading = false;
        this.toast.error('Reportes', 'Error al cargar hechos para el reporte.');
      }
    });
  }

  cargarAnios(): void {
    this.service.getAnios().subscribe({
      next: (data) => {
        this.anios = data || [];
        if (!this.anios.some(a => a.anio === this.selectedAnio)) {
          this.anios.unshift({ anio: this.selectedAnio });
        }
      }
    });
  }

  cargarUnidades(): void {
    this.service.getUnidades().subscribe({
      next: (data) => this.unidades = data || []
    });
  }

  limpiarFiltros(): void {
    this.searchQuery = '';
    this.filterFechaDesde = '';
    this.filterFechaHasta = '';
    this.filterUnidad = null;
  }

  get hechosFiltrados(): DtoHechoNoticioso[] {
    let list = this.hechos;

    const q = (this.searchQuery || '').trim().toLowerCase();
    if (q) {
      list = list.filter(h =>
        h.titular.toLowerCase().includes(q) ||
        (h.unidadStr || '').toLowerCase().includes(q) ||
        (h.temaStr || '').toLowerCase().includes(q)
      );
    }

    if (this.filterFechaDesde) {
      const desde = new Date(this.filterFechaDesde + 'T00:00:00');
      list = list.filter(h => h.fechaCreacion && new Date(h.fechaCreacion) >= desde);
    }
    if (this.filterFechaHasta) {
      const hasta = new Date(this.filterFechaHasta + 'T23:59:59');
      list = list.filter(h => h.fechaCreacion && new Date(h.fechaCreacion) <= hasta);
    }

    if (this.filterUnidad !== null && this.filterUnidad !== undefined && this.filterUnidad.toString() !== 'null') {
      list = list.filter(h => h.idUnidad === Number(this.filterUnidad));
    }

    return list;
  }

  get totalMedios(): number {
    return this.hechosFiltrados.reduce((acc, h) => acc + (h.contadorMedios || 0), 0);
  }

  exportarExcel(): void {
    if (this.hechosFiltrados.length === 0) {
      this.toast.warning('Exportar', 'No hay datos en la lista actual para exportar.');
      return;
    }

    const headers = [
      'TITULAR',
      'FECHA',
      'UNIDAD',
      'CARACTER',
      'TEMA',
      'DESCRIPCION TEMA',
      'COBERTURA',
      'TENDENCIA',
      'DEPENDENCIA',
      'ESTACION',
      'CANTIDAD MEDIOS'
    ];

    const rows = this.hechosFiltrados.map(h => [
      `"${(h.titular || '').replace(/"/g, '""')}"`,
      h.fecha ? `"${new Date(h.fecha).toLocaleDateString()}"` : '""',
      `"${(h.unidadStr || '').replace(/"/g, '""')}"`,
      `"${(h.caracterStr || '').replace(/"/g, '""')}"`,
      `"${(h.temaStr || '').replace(/"/g, '""')}"`,
      `"${(h.descTemaStr || '').replace(/"/g, '""')}"`,
      `"${(h.coberturaStr || '').replace(/"/g, '""')}"`,
      `"${(h.tendenciaStr || '').replace(/"/g, '""')}"`,
      `"${(h.dependenciaStr || '').replace(/"/g, '""')}"`,
      `"${(h.estacionStr || '').replace(/"/g, '""')}"`,
      h.contadorMedios
    ]);

    const csvContent = '\uFEFF' + [
      headers.join(';'),
      ...rows.map(e => e.join(';'))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `reporte_hechos_${this.selectedAnio}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    this.toast.success('Exportar', 'Archivo exportado correctamente.');
  }
}
