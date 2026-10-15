import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  DbMenuItem,
  MenuRolCatalogItem,
  MenuRolItem,
  MenuSaveRequest,
  MenuService
} from '../../../core/services/administracion/menu.service';
import { ToastService } from '../../../core/services/toast.service';
import { AlertService } from '../../../core/services/alert.service';
import { UiIconPickerComponent } from '../../../components/ui-icon-picker/ui-icon-picker';

export interface MenuTreeRow {
  item: DbMenuItem;
  depth: number;
  indent: number;
  hasChildren: boolean;
  isLastSibling: boolean;
}

@Component({
  selector: 'app-menu-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, UiIconPickerComponent],
  templateUrl: './menu-admin.html',
  styleUrls: ['./menu-admin.scss']
})
export class MenuAdminComponent implements OnInit {
  minimized = false;
  visible = true;
  loading = false;
  saving = false;

  menuItems: DbMenuItem[] = [];
  expandedIds = new Set<number>();
  searchTerm = '';

  // Modal de Crear / Editar Menú
  showMenuModal = false;
  editingId: number | null = null;
  editingMenuName = '';
  createParentName = '';
  form: MenuSaveRequest = this.getDefaultForm();

  // Modal de Roles
  showRolesModal = false;
  selectedMenuForRoles: DbMenuItem | null = null;
  rolesCatalog: MenuRolCatalogItem[] = [];
  rolesAsignados: MenuRolItem[] = [];
  selectedRolId: number | null = null;
  loadingRoles = false;
  savingRole = false;

  constructor(
    private menuService: MenuService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef,
    private alert: AlertService
  ) {}

  ngOnInit(): void {
    this.loadMenu();
  }

  toggleMinimize(): void {
    this.minimized = !this.minimized;
  }

  closePanel(): void {
    this.visible = false;
  }

  loadMenu(focusId: number | null = null): void {
    this.loading = true;
    this.menuService.getAdminMenu().subscribe({
      next: (items) => {
        // Normalizar tipos numéricos
        this.menuItems = (items ?? []).map((x: any) => ({
          idMenu: Number(x.idMenu ?? x.IdMenu ?? 0),
          descripcion: String(x.descripcion ?? x.Descripcion ?? ''),
          idPadre: Number(x.idPadre ?? x.IdPadre ?? 0),
          posicion: Number(x.posicion ?? x.Posicion ?? 0),
          tipo: String(x.tipo ?? x.Tipo ?? 'S'),
          icono: x.icono ?? x.Icono ?? '',
          vigente: Number(x.vigente ?? x.Vigente ?? 1),
          detalle: x.detalle ?? x.Detalle ?? ''
        })).sort((a, b) => a.idPadre - b.idPadre || a.posicion - b.posicion || a.idMenu - b.idMenu);

        // Expandir por defecto las carpetas de primer nivel si expandedIds está vacío
        if (this.expandedIds.size === 0) {
          for (const item of this.menuItems) {
            if (item.idPadre === 1 && item.tipo === 'S') {
              this.expandedIds.add(item.idMenu);
            }
          }
        }

        if (focusId !== null) {
          this.expandAncestors(focusId);
        }

        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.toast.error('Menú', err?.error?.message ?? 'No fue posible cargar el menú.');
      }
    });
  }

  get totalItemsCount(): number {
    return this.menuItems.filter(item => !this.isRaiz(item)).length;
  }

  get activeItemsCount(): number {
    return this.menuItems.filter(item => !this.isRaiz(item) && item.vigente === 1).length;
  }

  get parentOptions(): DbMenuItem[] {
    const editingId = this.editingId;
    const excluded = new Set<number>();
    if (editingId) {
      excluded.add(editingId);
      this.collectDescendantIds(editingId, this.menuItems, excluded);
    }
    return this.menuItems
      .filter((item) => !excluded.has(item.idMenu))
      .filter((item) => item.idMenu === 1 || !this.isRaiz(item))
      .filter((item) => item.idMenu === 1 || item.tipo === 'S')
      .slice()
      .sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es'));
  }

  private collectDescendantIds(parentId: number, items: DbMenuItem[], out: Set<number>): void {
    for (const item of items) {
      if (item.idPadre === parentId && !out.has(item.idMenu)) {
        out.add(item.idMenu);
        this.collectDescendantIds(item.idMenu, items, out);
      }
    }
  }

  get visibleRows(): MenuTreeRow[] {
    const source = this.menuItems.filter(item => !this.isRaiz(item));
    const itemIds = new Set(source.map(item => item.idMenu));
    const childrenByParent = new Map<number, DbMenuItem[]>();

    for (const item of source) {
      const pId = Number(item.idPadre);
      const siblings = childrenByParent.get(pId) ?? [];
      siblings.push(item);
      childrenByParent.set(pId, siblings);
    }

    for (const siblings of childrenByParent.values()) {
      siblings.sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es', { sensitivity: 'base' }) || a.posicion - b.posicion);
    }

    const roots = source
      .filter(item => item.idPadre === 1 || !itemIds.has(item.idPadre))
      .sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es', { sensitivity: 'base' }) || a.posicion - b.posicion);

    const term = this.searchTerm.trim().toLocaleLowerCase('es');
    const branchMatchCache = new Map<number, boolean>();

    const branchMatches = (item: DbMenuItem, trail = new Set<number>()): boolean => {
      const cached = branchMatchCache.get(item.idMenu);
      if (cached !== undefined) return cached;
      if (trail.has(item.idMenu)) return false;

      const ownMatch = this.searchableText(item).includes(term);
      const nextTrail = new Set(trail).add(item.idMenu);
      const childMatch = (childrenByParent.get(item.idMenu) ?? []).some(child => branchMatches(child, nextTrail));
      const matches = !term || ownMatch || childMatch;
      branchMatchCache.set(item.idMenu, matches);
      return matches;
    };

    const rows: MenuTreeRow[] = [];
    const visited = new Set<number>();

    const append = (item: DbMenuItem, depth: number, isLastSibling: boolean): void => {
      if (visited.has(item.idMenu) || !branchMatches(item)) return;
      visited.add(item.idMenu);

      const children = childrenByParent.get(item.idMenu) ?? [];
      const visibleChildren = children.filter(child => branchMatches(child));
      rows.push({
        item,
        depth,
        indent: Math.min(depth, 5) * 26,
        hasChildren: children.length > 0,
        isLastSibling
      });

      if (term || this.expandedIds.has(item.idMenu)) {
        for (const [index, child] of visibleChildren.entries()) {
          append(child, depth + 1, index === visibleChildren.length - 1);
        }
      }
    };

    const visibleRoots = roots.filter(root => branchMatches(root));
    for (const [index, root] of visibleRoots.entries()) {
      append(root, 0, index === visibleRoots.length - 1);
    }

    return rows;
  }

  private searchableText(item: DbMenuItem): string {
    return [item.idMenu, item.descripcion, item.tipo, item.detalle ?? '', item.icono ?? '']
      .join(' ')
      .toLocaleLowerCase('es');
  }

  getSuggestedPosition(parentId: number): number {
    const siblings = this.menuItems.filter(item => item.idPadre === parentId);
    if (siblings.length === 0) return 0;
    return Math.max(...siblings.map(s => s.posicion)) + 1;
  }

  toggleNode(itemId: number, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    const id = Number(itemId);
    const next = new Set(this.expandedIds);
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.expandedIds = next;
    this.cdr.markForCheck();
  }

  onRowClick(row: MenuTreeRow, event: Event): void {
    if (row.hasChildren) {
      this.toggleNode(row.item.idMenu, event);
    }
  }

  trackByMenuId(_index: number, row: MenuTreeRow): number {
    return row.item.idMenu;
  }

  isExpanded(itemId: number): boolean {
    return this.expandedIds.has(Number(itemId));
  }

  expandAll(): void {
    const next = new Set<number>();
    for (const item of this.menuItems) {
      if (item.tipo === 'S') {
        next.add(item.idMenu);
      }
    }
    this.expandedIds = next;
    this.cdr.markForCheck();
  }

  collapseAll(): void {
    this.expandedIds = new Set<number>();
    this.cdr.markForCheck();
  }

  expandAncestors(menuId: number): void {
    const byId = new Map(this.menuItems.map(i => [i.idMenu, i]));
    let current = byId.get(menuId);
    const next = new Set(this.expandedIds);
    while (current && current.idPadre > 1) {
      next.add(current.idPadre);
      current = byId.get(current.idPadre);
    }
    this.expandedIds = next;
    this.cdr.markForCheck();
  }

  clearSearch(): void {
    this.searchTerm = '';
  }

  itemIcon(item: DbMenuItem): string {
    return (
      item.icono?.trim() ||
      (item.tipo === 'S'
        ? (this.isExpanded(item.idMenu) ? 'fa-solid fa-folder-open' : 'fa-solid fa-folder')
        : item.tipo === 'pdf'
          ? 'fa-solid fa-file-pdf'
          : item.tipo === 'url'
            ? 'fa-solid fa-link'
            : 'fa-solid fa-file-lines')
    );
  }

  typeLabel(tipo: string): string {
    const labels: Record<string, string> = {
      S: 'Submenú',
      frm: 'Formulario',
      url: 'Hipervínculo',
      pdf: 'PDF'
    };
    return labels[tipo] || tipo || 'Sin tipo';
  }

  // --- MODAL DE CREACIÓN / EDICIÓN ---
  openCreateModal(parent?: DbMenuItem, event?: Event): void {
    if (event) event.stopPropagation();
    this.editingId = null;
    this.editingMenuName = '';
    this.createParentName = parent ? parent.descripcion : '';
    const parentId = parent ? parent.idMenu : 1;
    this.form = {
      idMenu: null,
      descripcion: '',
      idPadre: parentId,
      posicion: this.getSuggestedPosition(parentId),
      tipo: parent ? 'frm' : 'S',
      icono: '',
      vigente: 1,
      detalle: ''
    };
    if (parent) {
      this.expandedIds.add(parent.idMenu);
    }
    this.showMenuModal = true;
    try { document.body.classList.add('ui-modal-open'); } catch { /* ignore */ }
    this.cdr.markForCheck();
  }

  openEditModal(item: DbMenuItem, event?: Event): void {
    if (event) event.stopPropagation();
    this.editingId = item.idMenu;
    this.editingMenuName = item.descripcion;
    this.createParentName = '';
    this.form = {
      idMenu: item.idMenu,
      descripcion: item.descripcion ?? '',
      idPadre: item.idPadre ?? 1,
      posicion: item.posicion ?? 0,
      tipo: item.tipo ?? 'S',
      icono: item.icono ?? '',
      vigente: item.vigente ?? 1,
      detalle: item.detalle ?? ''
    };
    this.expandAncestors(item.idMenu);
    this.showMenuModal = true;
    try { document.body.classList.add('ui-modal-open'); } catch { /* ignore */ }
    this.cdr.markForCheck();
  }

  closeMenuModal(): void {
    this.showMenuModal = false;
    this.editingId = null;
    this.editingMenuName = '';
    this.createParentName = '';
    try { document.body.classList.remove('ui-modal-open'); } catch { /* ignore */ }
    this.cdr.markForCheck();
  }

  nuevo(): void {
    this.openCreateModal();
  }

  crearSubmenu(parent: DbMenuItem, event?: Event): void {
    this.openCreateModal(parent, event);
  }

  editar(item: DbMenuItem, event?: Event): void {
    this.openEditModal(item, event);
  }

  guardar(): void {
    if (!this.form.descripcion.trim()) {
      this.toast.warning('Menú', 'La descripción es obligatoria.');
      return;
    }

    if (!this.form.tipo.trim()) {
      this.toast.warning('Menú', 'El tipo es obligatorio.');
      return;
    }

    if (this.form.posicion < 0) {
      this.toast.warning('Menú', 'La posición no puede ser negativa.');
      return;
    }

    this.saving = true;
    this.menuService.saveAdminMenu(this.form).subscribe({
      next: (resp) => {
        this.saving = false;
        if (!resp?.success) {
          this.toast.warning('Menú', resp?.message || 'No fue posible guardar.');
          return;
        }
        this.toast.success('Menú', resp.message || 'Menú guardado correctamente.');
        const targetId = resp.id || this.editingId;
        this.closeMenuModal();
        this.loadMenu(targetId);
      },
      error: (err) => {
        this.saving = false;
        this.toast.error('Menú', err?.error?.detail ?? err?.error?.message ?? 'Error guardando menú.');
      }
    });
  }

  async cambiarEstado(item: DbMenuItem, event?: Event): Promise<void> {
    if (event) event.stopPropagation();
    const nuevoEstado = item.vigente === 1 ? 0 : 1;
    const accion = nuevoEstado === 1 ? 'activar' : 'inactivar';
    const motivo = await this.alert.prompt(
      'Cambiar estado de menú',
      `¿Está seguro de ${accion} el ítem de menú "${item.descripcion}"?`,
      'Motivo de la acción:',
      'Ingrese la justificación u observación...',
      'Confirmar',
      'Cancelar'
    );
    if (motivo === null) return;

    this.menuService.setEstadoAdminMenu(item.idMenu, { vigente: nuevoEstado, observaciones: motivo }).subscribe({
      next: (resp) => {
        if (!resp?.success) {
          this.toast.warning('Menú', resp?.message || 'No fue posible actualizar estado.');
          return;
        }
        item.vigente = nuevoEstado;
        this.toast.success('Menú', nuevoEstado === 1 ? 'Menú activado.' : 'Menú inactivado.');
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.toast.error('Menú', err?.error?.detail ?? err?.error?.message ?? 'Error actualizando estado.');
      }
    });
  }

  gestionarRoles(item: DbMenuItem, event?: Event): void {
    if (event) event.stopPropagation();
    this.selectedMenuForRoles = item;
    this.selectedRolId = null;
    this.showRolesModal = true;
    try { document.body.classList.add('ui-modal-open'); } catch { /* ignore */ }
    this.loadRolesPanel(item.idMenu);
  }

  closeRolesModal(): void {
    this.showRolesModal = false;
    this.selectedMenuForRoles = null;
    this.rolesAsignados = [];
    this.selectedRolId = null;
    try { document.body.classList.remove('ui-modal-open'); } catch { /* ignore */ }
  }

  cargarCatalogoRoles(): void {
    this.menuService.getRolesCatalog().subscribe({
      next: (roles) => {
        this.rolesCatalog = (roles ?? []).slice().sort((a, b) => a.descripcion.localeCompare(b.descripcion));
      },
      error: (err) => {
        this.rolesCatalog = [];
        this.toast.error('Roles menú', err?.error?.message ?? 'No fue posible cargar el catálogo de roles.');
      }
    });
  }

  loadRolesPanel(idMenu: number): void {
    this.loadingRoles = true;

    if (this.rolesCatalog.length === 0) {
      this.cargarCatalogoRoles();
    }

    this.menuService.getRolesByMenu(idMenu).subscribe({
      next: (data) => {
        this.rolesAsignados = (data ?? []).slice().sort((a, b) => a.descripcionRol.localeCompare(b.descripcionRol));
        this.loadingRoles = false;
      },
      error: (err) => {
        this.rolesAsignados = [];
        this.loadingRoles = false;
        this.toast.error('Roles menú', err?.error?.message ?? 'No fue posible consultar roles del menú.');
      }
    });
  }

  asignarRolMenu(): void {
    if (!this.selectedMenuForRoles) {
      this.toast.warning('Roles menú', 'Selecciona un menú para gestionar roles.');
      return;
    }

    if (!this.selectedRolId || this.selectedRolId <= 0) {
      this.toast.warning('Roles menú', 'Selecciona un rol.');
      return;
    }

    this.savingRole = true;
    this.menuService.assignRolToMenu(this.selectedMenuForRoles.idMenu, { idRol: this.selectedRolId }).subscribe({
      next: (resp) => {
        this.savingRole = false;
        if (!resp?.success) {
          this.toast.warning('Roles menú', resp?.message || 'No fue posible asignar el rol.');
          return;
        }
        this.toast.success('Roles menú', resp.message || 'Rol asignado correctamente.');
        this.selectedRolId = null;
        this.loadRolesPanel(this.selectedMenuForRoles!.idMenu);
      },
      error: (err) => {
        this.savingRole = false;
        this.toast.error('Roles menú', err?.error?.detail ?? err?.error?.message ?? 'Error asignando rol.');
      }
    });
  }

  quitarRolMenu(item: MenuRolItem): void {
    if (!this.selectedMenuForRoles) {
      return;
    }

    this.menuService.removeRolFromMenu(this.selectedMenuForRoles.idMenu, item.idRol).subscribe({
      next: (resp) => {
        if (!resp?.success) {
          this.toast.warning('Roles menú', resp?.message || 'No fue posible quitar el rol.');
          return;
        }
        this.toast.success('Roles menú', resp.message || 'Rol retirado del menú.');
        this.loadRolesPanel(this.selectedMenuForRoles!.idMenu);
      },
      error: (err) => {
        this.toast.error('Roles menú', err?.error?.detail ?? err?.error?.message ?? 'Error retirando rol.');
      }
    });
  }

  private getDefaultForm(): MenuSaveRequest {
    return {
      idMenu: null,
      descripcion: '',
      idPadre: 1,
      posicion: 0,
      tipo: 'S',
      icono: '',
      vigente: 1,
      detalle: ''
    };
  }

  private isRaiz(item: DbMenuItem): boolean {
    return item.idMenu === 1 || (item.descripcion ?? '').trim().toUpperCase() === 'RAIZ';
  }
}
