import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  RolAdminItem,
  RolesAdminService,
  SaveRolAdminRequest
} from '../../../core/services/administracion/roles-admin.service';
import { DbMenuItem, MenuService, RoleMenuItem } from '../../../core/services/administracion/menu.service';
import { ToastService } from '../../../core/services/toast.service';

import { AlertService } from '../../../core/services/alert.service';

export interface RoleMenuTreeNode {
  item: RoleMenuItem;
  depth: number;
  hasChildren: boolean;
}

@Component({
  selector: 'app-roles-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './roles-admin.html',
  styleUrls: ['./roles-admin.scss']
})
export class RolesAdminComponent implements OnInit {
  minimized = false;
  visible = true;
  loading = false;
  saving = false;
  loadingRoleMenus = false;
  savingRoleMenu = false;
  removingMenuId: number | null = null;
  searchTerm = '';

  // Modal de Crear / Editar Rol
  showRoleModal = false;
  editingId: number | null = null;
  editingRoleName = '';
  form: SaveRolAdminRequest = this.getDefaultForm();

  // Catálogos
  roles: RolAdminItem[] = [];
  allMenus: DbMenuItem[] = [];
  roleMenus: RoleMenuItem[] = [];

  // Selección de rol y permisos
  selectedRoleId: number | null = null;
  selectedMenuId: number | null = null;

  constructor(
    private readonly rolesService: RolesAdminService,
    private readonly menuService: MenuService,
    private readonly toast: ToastService,
    private readonly alert: AlertService
  ) {}

  ngOnInit(): void {
    this.loadMenus();
    this.loadRoles();
  }

  toggleMinimize(): void {
    this.minimized = !this.minimized;
  }

  get activeRolesCount(): number {
    return this.roles.filter((r) => r.vigente === 1).length;
  }

  get filteredRoles(): RolAdminItem[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.roles;
    return this.roles.filter(
      (r) =>
        (r.nombre ?? '').toLowerCase().includes(term) ||
        r.id.toString().includes(term)
    );
  }

  get selectedRole(): RolAdminItem | null {
    return this.roles.find((r) => r.id === this.selectedRoleId) ?? null;
  }

  get selectedRoleName(): string {
    if (!this.selectedRoleId) {
      return 'Sin rol seleccionado';
    }
    const role = this.selectedRole;
    return role?.nombre?.trim() || `Rol #${this.selectedRoleId}`;
  }

  get availableMenus(): DbMenuItem[] {
    const assigned = new Set(this.roleMenus.map((x) => x.idMenu));
    return this.allMenus
      .filter((m) => !assigned.has(m.idMenu))
      .sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es', { sensitivity: 'base' }));
  }

  get roleMenuNodes(): RoleMenuTreeNode[] {
    const ordered = [...this.roleMenus].sort(
      (a, b) =>
        a.idPadre - b.idPadre ||
        a.descripcionMenu.localeCompare(b.descripcionMenu, 'es', { sensitivity: 'base' }) ||
        a.posicion - b.posicion
    );
    const itemIds = new Set(ordered.map((item) => item.idMenu));
    const childrenByParent = new Map<number, RoleMenuItem[]>();

    for (const item of ordered) {
      if (item.idPadre !== item.idMenu) {
        const siblings = childrenByParent.get(item.idPadre) ?? [];
        siblings.push(item);
        childrenByParent.set(item.idPadre, siblings);
      }
    }

    const roots = ordered.filter(
      (item) => !itemIds.has(item.idPadre) || item.idPadre === 1 || item.idPadre === 0
    );
    const visited = new Set<number>();
    const result: RoleMenuTreeNode[] = [];

    const visit = (item: RoleMenuItem, depth: number): void => {
      if (visited.has(item.idMenu)) return;
      visited.add(item.idMenu);
      const children = childrenByParent.get(item.idMenu) ?? [];
      result.push({ item, depth: Math.min(depth, 5), hasChildren: children.length > 0 });
      for (const child of children) {
        visit(child, depth + 1);
      }
    };

    for (const root of roots) {
      visit(root, 0);
    }
    for (const item of ordered) {
      if (!visited.has(item.idMenu)) {
        visit(item, 0);
      }
    }
    return result;
  }

  iconForNode(node: RoleMenuTreeNode): string {
    if (node.hasChildren) {
      return 'fa-solid fa-folder-tree';
    }
    const type = node.item.tipo?.trim().toLowerCase() ?? '';
    return type.includes('extern') || type.includes('url') || type.includes('link')
      ? 'fa-solid fa-arrow-up-right-from-square'
      : type === 'pdf'
        ? 'fa-solid fa-file-pdf'
        : 'fa-solid fa-file-shield';
  }

  trackRoleById(_: number, item: RolAdminItem): number {
    return item.id;
  }

  trackNodeById(_: number, node: RoleMenuTreeNode): number {
    return node.item.idMenu;
  }

  clearSearch(): void {
    this.searchTerm = '';
  }

  loadRoles(preferredId: number | null = this.selectedRoleId): void {
    this.loading = true;
    this.rolesService.getAll().subscribe({
      next: (data) => {
        this.roles = (data ?? []).slice().sort((a, b) => {
          const aa = (a.nombre ?? '').toString();
          const bb = (b.nombre ?? '').toString();
          return aa.localeCompare(bb, 'es', { sensitivity: 'base' });
        });

        const nextSelection =
          this.roles.find((r) => r.id === preferredId) ?? this.roles[0] ?? null;

        if (nextSelection) {
          this.seleccionarRolPermisos(nextSelection);
        } else {
          this.selectedRoleId = null;
          this.roleMenus = [];
        }

        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.roles = [];
        this.toast.error('Roles', err?.error?.message ?? 'No fue posible consultar los roles.');
      }
    });
  }

  loadMenus(): void {
    this.menuService.getAdminMenu().subscribe({
      next: (items) => {
        this.allMenus = (items ?? [])
          .filter((x) => !this.isRaiz(x))
          .slice()
          .sort((a, b) => a.idPadre - b.idPadre || a.posicion - b.posicion || a.descripcion.localeCompare(b.descripcion, 'es'));
      },
      error: () => {
        this.allMenus = [];
      }
    });
  }

  // --- MODAL DE CREACIÓN / EDICIÓN ---
  openCreateRoleModal(): void {
    this.editingId = null;
    this.editingRoleName = '';
    this.form = this.getDefaultForm();
    this.showRoleModal = true;
    try { document.body.classList.add('ui-modal-open'); } catch { /* ignore */ }
  }

  openEditRoleModal(item: RolAdminItem, event?: Event): void {
    if (event) event.stopPropagation();
    this.editingId = item.id;
    this.editingRoleName = item.nombre ?? `Rol #${item.id}`;
    this.form = {
      id: item.id,
      nombre: (item.nombre ?? '').trim(),
      vigente: item.vigente === 0 ? 0 : 1
    };
    this.showRoleModal = true;
    try { document.body.classList.add('ui-modal-open'); } catch { /* ignore */ }
  }

  closeRoleModal(): void {
    this.showRoleModal = false;
    this.editingId = null;
    this.editingRoleName = '';
    try { document.body.classList.remove('ui-modal-open'); } catch { /* ignore */ }
  }

  nuevo(): void {
    this.openCreateRoleModal();
  }

  editar(item: RolAdminItem, event?: Event): void {
    this.openEditRoleModal(item, event);
  }

  guardar(): void {
    const nombre = (this.form.nombre ?? '').trim();
    if (!nombre) {
      this.toast.warning('Roles', 'La descripción del rol es obligatoria.');
      return;
    }

    const formActive = Number(this.form.vigente) === 1;

    if (formActive) {
      this.saving = true;
      const nombreNorm = this.normalizeForCompare(nombre);

      this.rolesService.getAll().subscribe({
        next: (data) => {
          const latest = (data ?? []);
          const duplicado = latest.some(r => {
            const rActive = Number(r.vigente) === 1;
            if (!rActive) return false;
            if (this.editingId != null && r.id === this.editingId) return false;
            const rNombreNorm = this.normalizeForCompare(r.nombre ?? '');
            return rNombreNorm === nombreNorm;
          });

          if (duplicado) {
            this.saving = false;
            this.toast.warning('Roles', 'Ya existe un rol activo con ese nombre.');
            return;
          }

          this.performSave(nombre);
        },
        error: () => {
          this.saving = false;
          this.toast.error('Roles', 'No fue posible validar duplicados. Intente nuevamente.');
        }
      });
    } else {
      this.performSave(nombre);
    }
  }

  private performSave(nombre: string): void {
    this.saving = true;
    this.rolesService
      .save({
        id: this.form.id ?? null,
        nombre,
        vigente: this.form.vigente === 0 ? 0 : 1
      })
      .subscribe({
        next: (resp) => {
          this.saving = false;
          if (!resp?.success) {
            this.toast.warning('Roles', resp?.message || 'No fue posible guardar el rol.');
            return;
          }

          this.toast.success('Roles', resp.message || 'Rol guardado correctamente.');
          const targetId = resp.idRol ?? this.form.id ?? this.selectedRoleId;
          this.closeRoleModal();
          this.loadRoles(targetId);
        },
        error: (err) => {
          this.saving = false;
          this.toast.error('Roles', err?.error?.detail ?? err?.error?.message ?? 'Error guardando rol.');
        }
      });
  }

  async cambiarEstado(item: RolAdminItem, event?: Event): Promise<void> {
    if (event) event.stopPropagation();
    const nuevoEstado = item.vigente === 1 ? 0 : 1;
    const accion = nuevoEstado === 1 ? 'activar' : 'inactivar';
    const motivo = await this.alert.prompt(
      'Cambiar estado de rol',
      `¿Está seguro de ${accion} el rol "${item.nombre}"?`,
      'Motivo de la acción:',
      'Ingrese la justificación u observación...',
      'Confirmar',
      'Cancelar'
    );
    if (motivo === null) return;

    this.rolesService.setEstado(item.id, { vigente: nuevoEstado, observaciones: motivo }).subscribe({
      next: (resp) => {
        if (!resp?.success) {
          this.toast.warning('Roles', resp?.message || 'No fue posible actualizar estado.');
          return;
        }

        this.toast.success('Roles', resp.message || 'Estado actualizado.');
        item.vigente = nuevoEstado;
        this.loadRoles(item.id);
      },
      error: (err) => {
        this.toast.error('Roles', err?.error?.detail ?? err?.error?.message ?? 'Error actualizando estado.');
      }
    });
  }

  seleccionarRol(item: RolAdminItem, event?: Event): void {
    if (event) event.stopPropagation();
    this.seleccionarRolPermisos(item);
  }

  seleccionarRolPermisos(item: RolAdminItem): void {
    this.selectedRoleId = item.id;
    this.selectedMenuId = null;
    this.cargarMenusPorRol(item.id);
  }

  cargarMenusPorRol(idRol: number): void {
    this.loadingRoleMenus = true;
    this.menuService.getMenusByRol(idRol).subscribe({
      next: (data) => {
        this.roleMenus = (data ?? []).slice().sort((a, b) => a.idPadre - b.idPadre || a.posicion - b.posicion || a.descripcionMenu.localeCompare(b.descripcionMenu, 'es'));
        this.loadingRoleMenus = false;
      },
      error: (err) => {
        this.roleMenus = [];
        this.loadingRoleMenus = false;
        this.toast.error('Permisos de menú', err?.error?.message ?? 'No fue posible consultar menús del rol.');
      }
    });
  }

  asignarMenu(): void {
    if (!this.selectedRoleId || this.selectedRoleId <= 0) {
      this.toast.warning('Permisos de menú', 'Seleccione un rol.');
      return;
    }

    if (!this.selectedMenuId || this.selectedMenuId <= 0) {
      this.toast.warning('Permisos de menú', 'Seleccione un menú.');
      return;
    }

    this.savingRoleMenu = true;
    this.menuService.assignMenuToRol(this.selectedRoleId, { idMenu: this.selectedMenuId }).subscribe({
      next: (resp) => {
        this.savingRoleMenu = false;
        if (!resp?.success) {
          this.toast.warning('Permisos de menú', resp?.message || 'No fue posible asignar menú.');
          return;
        }
        this.toast.success('Permisos de menú', resp.message || 'Menú asignado.');
        this.selectedMenuId = null;
        this.cargarMenusPorRol(this.selectedRoleId!);
      },
      error: (err) => {
        this.savingRoleMenu = false;
        this.toast.error('Permisos de menú', err?.error?.detail ?? err?.error?.message ?? 'Error asignando menú.');
      }
    });
  }

  quitarMenu(item: RoleMenuItem, event?: Event): void {
    if (event) event.stopPropagation();
    if (!this.selectedRoleId || this.selectedRoleId <= 0 || this.removingMenuId !== null) {
      return;
    }

    this.removingMenuId = item.idMenu;
    this.menuService.removeMenuFromRol(this.selectedRoleId, item.idMenu).subscribe({
      next: (resp) => {
        this.removingMenuId = null;
        if (!resp?.success) {
          this.toast.warning('Permisos de menú', resp?.message || 'No fue posible quitar menú.');
          return;
        }
        this.toast.success('Permisos de menú', resp.message || 'Menú retirado del rol.');
        this.cargarMenusPorRol(this.selectedRoleId!);
      },
      error: (err) => {
        this.removingMenuId = null;
        this.toast.error('Permisos de menú', err?.error?.detail ?? err?.error?.message ?? 'Error retirando menú.');
      }
    });
  }

  formatMenuLabel(menu: DbMenuItem): string {
    const prefix = menu.idPadre === 1 ? '' : '↳ ';
    return `${prefix}${menu.descripcion} (ID ${menu.idMenu})`;
  }

  private isRaiz(item: DbMenuItem): boolean {
    return item.idMenu === 1 || (item.descripcion ?? '').trim().toUpperCase() === 'RAIZ';
  }

  private getDefaultForm(): SaveRolAdminRequest {
    return {
      id: null,
      nombre: '',
      vigente: 1
    };
  }

  private normalizeForCompare(value: string): string {
    return (value ?? '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/\u00A0/g, ' ')
      .replace(/[\u0000-\u001F\u007F-\u009F]/g, '')
      .replace(/\s+/g, ' ')
      .trim()
      .toUpperCase();
  }
}
