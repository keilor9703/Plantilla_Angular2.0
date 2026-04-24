import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../../../core/services/toast.service';
import {
  RedesSocialesService,
  DtoRedesSociales,
  DtoRedesSocialesRequest,
  DtoDominio,
  DtoUnidad,
} from '../../../core/services/gestion-digital/redes-sociales.service';

interface GrupoRedSocial {
  redSocial: string;
  items: DtoRedesSociales[];
  expanded: boolean;
}

@Component({
  selector: 'app-redes-sociales-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './redes-sociales-admin.html',
  styleUrls: ['./redes-sociales-admin.scss'],
})
export class RedesSocialesAdminComponent implements OnInit {
  minimized = false;
  loading = false;
  saving = false;

  lista: DtoRedesSociales[] = [];
  grupos: GrupoRedSocial[] = [];
  editingId: number | null = null;

  form: DtoRedesSocialesRequest = this.formVacio();

  dominios: DtoDominio[] = [];
  unidades: DtoUnidad[] = [];

  constructor(
    private toast: ToastService,
    private redesSocialesService: RedesSocialesService
  ) {}

  ngOnInit(): void {
    this.cargar();
    this.cargarDominios();
    this.cargarUnidades();
  }

  cargar(): void {
    this.loading = true;
    this.redesSocialesService.getAll().subscribe({
      next: (data) => {
        this.lista = data ?? [];
        this.agruparLista();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Error', 'No se pudieron cargar las redes sociales');
      },
    });
  }

  cargarDominios(): void {
    this.redesSocialesService.getDominios().subscribe({
      next: (data) => {
        const items = data ?? [];
        this.dominios = items.some((d) => d.idPadre !== undefined)
          ? items.filter((d) => d.idPadre === 164)
          : items;
        this.agruparLista();
      },
      error: () => {
        this.toast.error('Error', 'No se pudieron cargar los dominios');
      },
    });
  }

  cargarUnidades(): void {
    this.redesSocialesService.getUnidades().subscribe({
      next: (data) => {
        this.unidades = data ?? [];
      },
      error: () => {
        this.toast.error('Error', 'No se pudieron cargar las unidades');
      },
    });
  }

  nuevo(): void {
    this.editingId = null;
    this.form = this.formVacio();
  }

  editar(item: DtoRedesSociales): void {
    this.editingId = item.id;
    this.form = {
      redSocial: item.redSocial,
      unidadResponsable: item.unidadResponsable,
      nombreCuenta: item.nombreCuenta,
      urlCuenta: item.urlCuenta,
      actaAsignacion: item.actaAsignacion,
      nombreResponsable: item.nombreResponsable,
      telResponsable: item.telResponsable,
      emailResponsable: item.emailResponsable,
      emailCuenta: item.emailCuenta,
      telAsociado: item.telAsociado,
      vigente: item.vigente,
    };
  }

  guardar(): void {
    if (this.form.redSocial === null || this.form.redSocial === undefined || this.form.redSocial === '') {
      this.toast.warning('Guardar', 'Debe seleccionar una red social');
      return;
    }
    if (this.form.unidadResponsable === null || this.form.unidadResponsable === undefined || this.form.unidadResponsable === '') {
      this.toast.warning('Guardar', 'Debe seleccionar una unidad responsable');
      return;
    }

    const idRedSocial = Number(this.form.redSocial);
    if (Number.isNaN(idRedSocial) || idRedSocial <= 0) {
      this.toast.warning('Guardar', 'Seleccione una red social válida');
      return;
    }

    const idUnidad = Number(this.form.unidadResponsable);
    if (Number.isNaN(idUnidad) || idUnidad <= 0) {
      this.toast.warning('Guardar', 'Seleccione una unidad válida');
      return;
    }
    if (!this.form.nombreCuenta?.trim()) {
      this.toast.warning('Guardar', 'El nombre de la cuenta es requerido');
      return;
    }
    if (!this.form.urlCuenta?.trim()) {
      this.toast.warning('Guardar', 'La URL es requerida');
      return;
    }

    const payload: DtoRedesSocialesRequest = {
      ...this.form,
      redSocial: String(idRedSocial),
      unidadResponsable: String(idUnidad),
    };

    this.saving = true;
    if (this.editingId) {
      this.redesSocialesService.update(this.editingId, payload).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success('Guardar', 'Red social actualizada correctamente');
          this.nuevo();
          this.cargar();
        },
        error: (err: any) => {
          this.saving = false;
          const msg = err?.error?.message ?? err?.message ?? 'Error al actualizar la red social';
          this.toast.error('Guardar', msg);
        },
      });
    } else {
      this.redesSocialesService.create(payload).subscribe({
        next: () => {
          this.saving = false;
          this.toast.success('Guardar', 'Red social creada correctamente');
          this.nuevo();
          this.cargar();
        },
        error: (err: any) => {
          this.saving = false;
          const msg = err?.error?.message ?? err?.message ?? 'Error al crear la red social';
          this.toast.error('Guardar', msg);
        },
      });
    }
  }

  eliminar(item: DtoRedesSociales): void {
    if (!confirm(`¿Está seguro de eliminar la cuenta "${item.nombreCuenta}" de ${item.redSocial}?`)) return;

    this.redesSocialesService.delete(item.id).subscribe({
      next: () => {
        this.toast.success('Eliminar', 'Red social eliminada correctamente');
        this.cargar();
      },
      error: () => this.toast.error('Eliminar', 'Error al eliminar la red social'),
    });
  }

  toggleGrupo(grupo: GrupoRedSocial): void {
    grupo.expanded = !grupo.expanded;
  }

  private agruparLista(): void {
    const map = new Map<string, DtoRedesSociales[]>();

    for (const item of this.lista) {
      const key = this.resolveRedSocialLabel(item.redSocial);
      const current = map.get(key) ?? [];
      current.push(item);
      map.set(key, current);
    }

    this.grupos = Array.from(map.entries())
      .sort((a, b) => a[0].localeCompare(b[0], 'es', { sensitivity: 'base' }))
      .map(([redSocial, items], index) => ({
        redSocial,
        items: items.sort((x, y) => (x.nombreCuenta ?? '').localeCompare(y.nombreCuenta ?? '', 'es', { sensitivity: 'base' })),
        expanded: index === 0,
      }));
  }

  private resolveRedSocialLabel(value: string): string {
    const raw = (value ?? '').trim();
    if (!raw) {
      return 'Sin red social';
    }

    const id = Number(raw);
    if (Number.isNaN(id)) {
      return raw;
    }

    const dominio = this.dominios.find((d) => (d.id ?? d.idDominio) === id);
    return dominio?.descripcion?.trim() || raw;
  }

  private formVacio(): DtoRedesSocialesRequest {
    return {
      redSocial: '',
      unidadResponsable: '',
      nombreCuenta: '',
      urlCuenta: '',
      actaAsignacion: '',
      nombreResponsable: '',
      telResponsable: '',
      emailResponsable: '',
      emailCuenta: '',
      telAsociado: '',
      vigente: 1,
    };
  }
}
