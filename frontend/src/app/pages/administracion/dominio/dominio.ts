import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ToastService } from '../../../core/services/toast.service';
import { DominioService, DtoDominio, DtoDominioRequest } from '../../../core/services/administracion/dominio.service';
import { AlertService } from '../../../core/services/alert.service';

@Component({
  selector: 'app-dominio',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './dominio.html',
  styleUrls: ['./dominio.scss']
})
export class DominioAdminComponent implements OnInit {
  visible = true;
  minimized = false;

  loading = false;
  saving = false;

  listaDominios: DtoDominio[] = [];
  dominiosTree: { item: DtoDominio; children: { item: DtoDominio; children: DtoDominio[]; expanded: boolean }[]; expanded: boolean }[] = [];
  editingId: number | null = null;

  dropdownPadreOpen = false;
  searchPadreFilter = '';

  form: any = {
    Descripcion: '',
    IdPadre: 1,
    Vigente: 1,
    Abreviatura: '',
    Observacion: ''
  };

  dominioOptions: { id: number; descripcion: string }[] = [];

  constructor(
    private toast: ToastService,
    private dominioService: DominioService
    ,private alert: AlertService
  ) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.dropdownPadreOpen) {
      this.dropdownPadreOpen = false;
      this.searchPadreFilter = '';
    }
  }

  get filteredDominioOptions(): { id: number; descripcion: string }[] {
    const query = (this.searchPadreFilter || '').trim().toLowerCase();
    const all: { id: number; descripcion: string }[] = [
      { id: 1, descripcion: 'Dominio Raíz' },
      ...this.dominioOptions
    ];
    if (!query) return all;
    return all.filter(opt => opt.descripcion.toLowerCase().includes(query));
  }

  get selectedPadreNombre(): string {
    const idPadre = Number(this.form?.IdPadre ?? 1);
    if (!idPadre || idPadre === 1) {
      return 'Dominio Raíz';
    }
    const found = this.listaDominios.find(d => d.idDominio === idPadre);
    return found?.descripcion || 'Dominio Raíz';
  }

  selectPadreOption(opt: { id: number; descripcion: string }): void {
    this.form.IdPadre = opt.id;
    this.dropdownPadreOpen = false;
    this.searchPadreFilter = '';
  }

  toggleDropdownPadre(event?: Event): void {
    if (event) event.stopPropagation();
    this.dropdownPadreOpen = !this.dropdownPadreOpen;
    if (!this.dropdownPadreOpen) {
      this.searchPadreFilter = '';
    }
  }

  ngOnInit(): void {
    this.cargarDominios();
  }

  cargarDominios(): void {
    this.loading = true;
    this.dominioService.getAll().subscribe({
      next: (data) => {
        this.listaDominios = (data ?? []).map((item) => this.normalizarDominio(item as any));
        this.buildTree();
        this.actualizarOpcionesPadre();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Error', 'No se pudieron cargar los dominios');
      }
    });
  }

  private actualizarOpcionesPadre(): void {
    this.dominioOptions = this.listaDominios
      .filter(
        d =>
          d.idDominio !== 0 &&
          d.idDominio !== 1 &&
          d.descripcion?.trim().toUpperCase() !== 'RAIZ' &&
          d.idDominio !== (this.editingId ?? 0)
      )
      .map(d => ({
        id: d.idDominio,
        descripcion: d.descripcion
      }))
      .sort((a, b) => a.descripcion.localeCompare(b.descripcion));
  }

  private normalizarDominio(item: any): DtoDominio {
    return {
      idDominio: Number(item?.idDominio ?? item?.IdDominio ?? item?.id_dominio ?? item?.ID_DOMINIO ?? 0),
      descripcion: String(item?.descripcion ?? item?.Descripcion ?? ''),
      idPadre: Number(item?.idPadre ?? item?.IdPadre ?? item?.id_padre ?? item?.ID_PADRE ?? 0),
      vigente: Number(item?.vigente ?? item?.Vigente ?? 0),
      abreviatura: String(item?.abreviatura ?? item?.Abreviatura ?? ''),
      observacion: String(item?.observacion ?? item?.Observacion ?? '')
    };
  }

  private buildTree(): void {
    // Excluimos la Raíz (ID 0, ID 1 o con descripción 'RAIZ') de mostrarse como un nodo contenedor principal
    const dominiosFiltrados = this.listaDominios.filter(d => 
      d.idDominio !== 0 && 
      d.idDominio !== 1 && 
      d.descripcion?.trim().toUpperCase() !== 'RAIZ'
    );

    // Los padres de nivel superior son aquellos cuyos padres son 0, 1 o vacíos (hijos directos de la Raíz)
    const padresSuperiores = dominiosFiltrados.filter(d => 
      !d.idPadre || d.idPadre === 0 || d.idPadre === 1
    );

    this.dominiosTree = this.getRecursiveNodes(padresSuperiores, dominiosFiltrados);
  }

  private getRecursiveNodes(parents: DtoDominio[], all: DtoDominio[]): any[] {
    return parents.map(p => {
      const children = all.filter(h => h.idPadre === p.idDominio);
      return {
        item: p,
        expanded: false,
        children: children.length > 0 ? this.getRecursiveNodes(children, all) : []
      };
    });
  }

  toggleNode(node: any): void {
    node.expanded = !node.expanded;
  }

  toggleExpand(index: number): void {
    this.dominiosTree[index].expanded = !this.dominiosTree[index].expanded;
  }

  toggleChildExpand(padreIndex: number, childIndex: number): void {
    this.dominiosTree[padreIndex].children[childIndex].expanded = 
      !this.dominiosTree[padreIndex].children[childIndex].expanded;
  }

  nuevo(): void {
    this.editingId = null;
    this.form = {
      Descripcion: '',
      IdPadre: 1,
      Vigente: 1,
      Abreviatura: '',
      Observacion: ''
    };
  }

  editar(item: DtoDominio): void {
    this.editingId = item.idDominio;
    this.form = {
      Descripcion: item.descripcion,
      IdPadre: item.idPadre,
      Vigente: item.vigente,
      Abreviatura: item.abreviatura || '',
      Observacion: item.observacion || ''
    };
    this.actualizarOpcionesPadre();
  }

  guardar(): void {
    if (!this.form.Descripcion?.trim()) {
      this.toast.warning('Guardar', 'La descripción es requerida');
      return;
    }

    this.saving = true;

    const request: DtoDominioRequest = {
      Descripcion: this.form.Descripcion.trim(),
      IdPadre: Number(this.form.IdPadre),
      Vigente: this.form.Vigente,
      Abreviatura: this.form.Abreviatura?.trim() || '',
      Observacion: this.form.Observacion?.trim() || ''
    };

    if (this.editingId) {
      this.dominioService.update(this.editingId, request).subscribe({
        next: (resp) => {
          this.saving = false;
          if (resp.success) {
            this.toast.success('Guardar', resp.message);
            this.nuevo();
            this.cargarDominios();
          } else {
            this.toast.warning('Guardar', resp.message);
          }
        },
        error: () => {
          this.saving = false;
          this.toast.error('Guardar', 'Error al actualizar dominio');
        }
      });
    } else {
      this.dominioService.create(request).subscribe({
        next: (resp) => {
          this.saving = false;
          if (resp.success) {
            this.toast.success('Guardar', resp.message);
            this.nuevo();
            this.cargarDominios();
          } else {
            this.toast.warning('Guardar', resp.message);
          }
        },
        error: () => {
          this.saving = false;
          this.toast.error('Guardar', 'Error al crear dominio');
        }
      });
    }
  }

  async eliminar(item: DtoDominio): Promise<void> {
    const motivo = await this.alert.prompt(
      'Eliminar dominio',
      `¿Está seguro de eliminar el dominio "${item.descripcion}"?`,
      'Motivo de la eliminación:',
      'Ingrese la justificación u observación...',
      'Eliminar',
      'Cancelar'
    );
    if (motivo === null) return;

    this.dominioService.delete(item.idDominio, motivo).subscribe({
      next: (resp) => {
        if (resp.success) {
          this.toast.success('Eliminar', resp.message);
          this.cargarDominios();
        } else {
          this.toast.warning('Eliminar', resp.message);
        }
      },
      error: () => {
        this.toast.error('Eliminar', 'Error al eliminar dominio');
      }
    });
  }

  cambiarEstado(item: DtoDominio): void {
    const nuevoEstado = item.vigente === 1 ? 0 : 1;
    const request: DtoDominioRequest = {
      Descripcion: item.descripcion,
      IdPadre: item.idPadre,
      Vigente: nuevoEstado,
      Abreviatura: item.abreviatura || '',
      Observacion: item.observacion || ''
    };

    this.dominioService.update(item.idDominio, request).subscribe({
      next: (resp) => {
        if (resp.success) {
          this.toast.success('Estado', resp.message);
          this.cargarDominios();
        } else {
          this.toast.warning('Estado', resp.message);
        }
      },
      error: () => {
        this.toast.error('Estado', 'Error al cambiar estado');
      }
    });
  }

  getNombrePadre(idPadre: number | string | null): string {
    if (!idPadre || idPadre === 0 || idPadre === '0' || idPadre === 'null') {
      return 'Raíz (sin padre)';
    }
    const padre = this.listaDominios.find(d => d.idDominio === Number(idPadre));
    return padre?.descripcion || 'Sin padre';
  }

  toggleMinimize(): void {
    this.minimized = !this.minimized;
  }

  closePanel(): void {
    this.visible = false;
  }
}
