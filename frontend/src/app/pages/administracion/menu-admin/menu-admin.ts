import { Component, OnInit } from '@angular/core';
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

@Component({
  selector: 'app-menu-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './menu-admin.html',
  styleUrls: ['./menu-admin.scss']
})
export class MenuAdminComponent implements OnInit {
  minimized = false;
  visible = true;
  loading = false;
  saving = false;

  menuItems: DbMenuItem[] = [];
  menuTree: { item: DbMenuItem; children: any[]; expanded: boolean }[] = [];
  parentOptions: DbMenuItem[] = [];
  expandedParents = new Set<number>();
  selectedMenuForRoles: DbMenuItem | null = null;
  rolesCatalog: MenuRolCatalogItem[] = [];
  rolesAsignados: MenuRolItem[] = [];
  selectedRolId: number | null = null;
  loadingRoles = false;
  savingRole = false;
  editingId: number | null = null;
  showRolesModal = false;

  // Selector de Iconos
  showIconPickerModal = false;
  iconSearchTerm = '';
  selectedCategory = 'Todos';

  iconCategories = ['Todos', 'Navegación', 'Administración', 'Usuarios', 'Seguridad', 'Archivos', 'Reportes', 'Comunicación', 'Ubicación'];

  iconList: { class: string; name: string; category: string }[] = [
    // Navegación
    { class: 'fa-solid fa-house', name: 'Inicio / Home', category: 'Navegación' },
    { class: 'fa-solid fa-folder', name: 'Carpeta', category: 'Navegación' },
    { class: 'fa-solid fa-folder-open', name: 'Carpeta abierta', category: 'Navegación' },
    { class: 'fa-solid fa-bars', name: 'Menú / Barras', category: 'Navegación' },
    { class: 'fa-solid fa-list', name: 'Lista', category: 'Navegación' },
    { class: 'fa-solid fa-sitemap', name: 'Mapa de sitio', category: 'Navegación' },
    { class: 'fa-solid fa-layer-group', name: 'Capas / Grupos', category: 'Navegación' },
    { class: 'fa-solid fa-border-all', name: 'Módulos Grid', category: 'Navegación' },

    // Administración
    { class: 'fa-solid fa-gear', name: 'Engranaje / Config', category: 'Administración' },
    { class: 'fa-solid fa-gears', name: 'Engranajes', category: 'Administración' },
    { class: 'fa-solid fa-sliders', name: 'Ajustes Sliders', category: 'Administración' },
    { class: 'fa-solid fa-wrench', name: 'Llave de tuercas', category: 'Administración' },
    { class: 'fa-solid fa-screwdriver-wrench', name: 'Herramientas', category: 'Administración' },
    { class: 'fa-solid fa-toolbox', name: 'Caja herramientas', category: 'Administración' },
    { class: 'fa-solid fa-database', name: 'Base de datos', category: 'Administración' },
    { class: 'fa-solid fa-server', name: 'Servidor', category: 'Administración' },

    // Usuarios
    { class: 'fa-solid fa-users', name: 'Usuarios', category: 'Usuarios' },
    { class: 'fa-solid fa-user', name: 'Usuario individual', category: 'Usuarios' },
    { class: 'fa-solid fa-user-gear', name: 'Gestión usuarios', category: 'Usuarios' },
    { class: 'fa-solid fa-user-shield', name: 'Usuario seguro', category: 'Usuarios' },
    { class: 'fa-solid fa-user-tie', name: 'Ejecutivo', category: 'Usuarios' },
    { class: 'fa-solid fa-id-card', name: 'Carnet / Cédula', category: 'Usuarios' },
    { class: 'fa-solid fa-address-book', name: 'Directorio', category: 'Usuarios' },
    { class: 'fa-solid fa-user-group', name: 'Grupo usuarios', category: 'Usuarios' },
    { class: 'fa-solid fa-users-gear', name: 'Admin grupos', category: 'Usuarios' },

    // Seguridad
    { class: 'fa-solid fa-shield-halved', name: 'Escudo Seguridad', category: 'Seguridad' },
    { class: 'fa-solid fa-shield-cat', name: 'Protección', category: 'Seguridad' },
    { class: 'fa-solid fa-lock', name: 'Candado Bloqueo', category: 'Seguridad' },
    { class: 'fa-solid fa-key', name: 'Llave Clave', category: 'Seguridad' },
    { class: 'fa-solid fa-user-secret', name: 'Incógnito / Secreto', category: 'Seguridad' },
    { class: 'fa-solid fa-clipboard-check', name: 'Auditoría', category: 'Seguridad' },
    { class: 'fa-solid fa-eye', name: 'Inspección', category: 'Seguridad' },
    { class: 'fa-solid fa-fingerprint', name: 'Huella digital', category: 'Seguridad' },

    // Archivos
    { class: 'fa-solid fa-file', name: 'Archivo', category: 'Archivos' },
    { class: 'fa-solid fa-file-lines', name: 'Documento texto', category: 'Archivos' },
    { class: 'fa-solid fa-file-pdf', name: 'PDF', category: 'Archivos' },
    { class: 'fa-solid fa-file-excel', name: 'Excel', category: 'Archivos' },
    { class: 'fa-solid fa-file-word', name: 'Word', category: 'Archivos' },
    { class: 'fa-solid fa-box-archive', name: 'Archivo histórico', category: 'Archivos' },
    { class: 'fa-solid fa-newspaper', name: 'Prensa / Noticias', category: 'Archivos' },
    { class: 'fa-solid fa-folder-tree', name: 'Carpeta árbol', category: 'Archivos' },

    // Reportes
    { class: 'fa-solid fa-chart-line', name: 'Gráfico línea', category: 'Reportes' },
    { class: 'fa-solid fa-chart-pie', name: 'Gráfico torta', category: 'Reportes' },
    { class: 'fa-solid fa-chart-column', name: 'Gráfico barras', category: 'Reportes' },
    { class: 'fa-solid fa-square-poll-vertical', name: 'Estadísticas', category: 'Reportes' },
    { class: 'fa-solid fa-diagram-project', name: 'Proyectos', category: 'Reportes' },
    { class: 'fa-solid fa-gauge-high', name: 'Dashboard', category: 'Reportes' },

    // Comunicación
    { class: 'fa-solid fa-envelope', name: 'Correo Email', category: 'Comunicación' },
    { class: 'fa-solid fa-paper-plane', name: 'Envío correo', category: 'Comunicación' },
    { class: 'fa-solid fa-bell', name: 'Notificación', category: 'Comunicación' },
    { class: 'fa-solid fa-bullhorn', name: 'Comunicado', category: 'Comunicación' },
    { class: 'fa-solid fa-comment', name: 'Comentario', category: 'Comunicación' },
    { class: 'fa-solid fa-headset', name: 'Soporte', category: 'Comunicación' },
    { class: 'fa-solid fa-radio', name: 'Radio / Emisora', category: 'Comunicación' },

    // Ubicación
    { class: 'fa-solid fa-building', name: 'Edificio Sede', category: 'Ubicación' },
    { class: 'fa-solid fa-building-columns', name: 'Institución', category: 'Ubicación' },
    { class: 'fa-solid fa-map-location-dot', name: 'Mapa Ubicación', category: 'Ubicación' },
    { class: 'fa-solid fa-globe', name: 'Portal Web', category: 'Ubicación' },
    { class: 'fa-solid fa-landmark', name: 'Oficina Central', category: 'Ubicación' }
  ];

  form: MenuSaveRequest = this.getDefaultForm();

  constructor(
    private menuService: MenuService,
    private toast: ToastService,
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

  loadMenu(): void {
    this.loading = true;
    this.menuService.getAdminMenu().subscribe({
      next: (items) => {
        this.menuItems = (items ?? []).slice().sort((a, b) => a.idPadre - b.idPadre || a.posicion - b.posicion);
        this.buildMenuTree();
        this.parentOptions = this.menuItems
          .filter((item) => item.idMenu === 1 || !this.isRaiz(item))
          .slice()
          .sort((a, b) => a.descripcion.localeCompare(b.descripcion));
        this.expandedParents.clear();
        this.selectedMenuForRoles = null;
        this.rolesAsignados = [];
        this.selectedRolId = null;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toast.error('Menú', err?.error?.message ?? 'No fue posible cargar el menú.');
      }
    });
  }

  nuevo(): void {
    this.editingId = null;
    this.form = this.getDefaultForm();
  }

  editar(item: DbMenuItem): void {
    this.editingId = item.idMenu;
    this.form = {
      idMenu: item.idMenu,
      descripcion: item.descripcion ?? '',
      idPadre: item.idPadre ?? 0,
      posicion: item.posicion ?? 0,
      tipo: item.tipo ?? 'S',
      icono: item.icono ?? '',
      vigente: item.vigente ?? 1,
      detalle: item.detalle ?? ''
    };
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

    if (this.editingId && this.form.idPadre === this.editingId) {
      this.toast.warning('Menú', 'Un menú no puede ser su propio menú padre.');
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
        this.nuevo();
        this.loadMenu();
      },
      error: (err) => {
        this.saving = false;
        this.toast.error('Menú', err?.error?.detail ?? err?.error?.message ?? 'Error guardando menú.');
      }
    });
  }

  async cambiarEstado(item: DbMenuItem): Promise<void> {
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
        this.toast.success('Menú', resp.message || 'Estado actualizado.');
        this.loadMenu();
      },
      error: (err) => {
        this.toast.error('Menú', err?.error?.detail ?? err?.error?.message ?? 'Error actualizando estado.');
      }
    });
  }

  gestionarRoles(item: DbMenuItem): void {
    this.selectedMenuForRoles = item;
    this.selectedRolId = null;
    this.showRolesModal = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) { /* ignore */ }
    this.loadRolesPanel(item.idMenu);
  }

  closeRolesModal(): void {
    this.showRolesModal = false;
    this.selectedMenuForRoles = null;
    this.rolesAsignados = [];
    this.selectedRolId = null;
    try { document.body.classList.remove('ui-modal-open'); } catch (e) { /* ignore */ }
  }

  get filteredIcons() {
    return this.iconList.filter(item => {
      const matchesCategory = this.selectedCategory === 'Todos' || item.category === this.selectedCategory;
      const term = this.iconSearchTerm.trim().toLowerCase();
      const matchesSearch = !term || item.name.toLowerCase().includes(term) || item.class.toLowerCase().includes(term);
      return matchesCategory && matchesSearch;
    });
  }

  openIconPicker(): void {
    this.showIconPickerModal = true;
    try { document.body.classList.add('ui-modal-open'); } catch (e) { /* ignore */ }
  }

  closeIconPicker(): void {
    this.showIconPickerModal = false;
    try { document.body.classList.remove('ui-modal-open'); } catch (e) { /* ignore */ }
  }

  selectIcon(iconClass: string): void {
    this.form.icono = iconClass;
    this.closeIconPicker();
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

  private buildMenuTree(): void {
    const all = this.menuItems.filter(item => !this.isRaiz(item));
    const roots = all.filter(item => item.idPadre === 1 || item.idPadre === 0 || item.idPadre === item.idMenu);
    this.menuTree = this.getMenuNodes(roots, all);
  }

  private getMenuNodes(parents: DbMenuItem[], all: DbMenuItem[]): any[] {
    return parents
      .map(p => {
        const children = all.filter(h => h.idPadre === p.idMenu && h.idMenu !== p.idMenu);
        return {
          item: p,
          expanded: false,
          children: children.length > 0 ? this.getMenuNodes(children, all) : []
        };
      })
      .sort((a, b) => a.item.posicion - b.item.posicion || a.item.idMenu - b.item.idMenu);
  }

  toggleNode(node: any): void {
    node.expanded = !node.expanded;
  }

  get availableParentOptions(): DbMenuItem[] {
    return this.parentOptions.filter(item => item.idMenu !== this.editingId);
  }

  get parentMenuItems(): DbMenuItem[] {
    return this.menuItems
      .filter((item) => (item.idPadre === 1 || item.idPadre === 0 || item.idPadre === item.idMenu) && !this.isRaiz(item))
      .sort((a, b) => a.posicion - b.posicion || a.idMenu - b.idMenu);
  }

  getChildrenOf(parentId: number): DbMenuItem[] {
    return this.menuItems
      .filter((item) => item.idPadre === parentId)
      .sort((a, b) => a.posicion - b.posicion || a.idMenu - b.idMenu);
  }

  hasChildren(parentId: number): boolean {
    return this.menuItems.some((item) => item.idPadre === parentId);
  }

  isExpanded(parentId: number): boolean {
    return this.expandedParents.has(parentId);
  }

  toggleChildren(parentId: number): void {
    if (this.expandedParents.has(parentId)) {
      this.expandedParents.delete(parentId);
      return;
    }
    this.expandedParents.add(parentId);
  }

  private getDefaultForm(): MenuSaveRequest {
    return {
      idMenu: null,
      descripcion: '',
      idPadre: 0,
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


