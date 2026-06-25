import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastService } from '../../../core/services/toast.service';
import { AlertService } from '../../../core/services/alert.service';
import { DtoRolCatalogo, EditarRolRequest, UsuarioAdminService, UsuarioListadoItem } from '../../../core/services/administracion/usuario-admin.service';
import { AuthService } from '../../../core/auth/auth.service';

type TabKey = 'datos' | 'roles';
type RolEstado = 'Vigente' | 'Vencido';

interface UserRole {
  id: number;
  nombre: string;
  fechaExpiracion: string;
  estado: RolEstado;
  justificacion: string;
}

interface NewRoleForm {
  rolId: number | null;
  justificacion: string;
  fechaFin: string;
}

interface UserProfile {
  identificacion: string;
  nombres: string;
  apellidos: string;
  grado: string;
  nombreCompleto: string;
  usuarioEmpresarial: string;
  email: string;
  telefono: string;
  situacionLaboral: string;
  unidad: string;
  unidadFisica: string;
  cargo: string;
  gradAlfabetico: string;
  funcionarioCodigo: string;
  undeLaborandoCodigo: string;
  codigoCargo: string;
  activo: boolean;
  ultimoIngreso: string;
  fotoUrl?: string;
  roles: UserRole[];
}

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './usuarios.html',
  styleUrls: ['./usuarios.scss']
})
export class UsuariosComponent implements OnInit, OnDestroy {
  public readonly superAdministradorRolId = 1;
  private canAssignSuperAdministrador = false;

  constructor(
    private toast: ToastService,
    private usuarioAdminService: UsuarioAdminService,
    private authService: AuthService,
    private router: Router,
    private alert: AlertService
  ) {}

  minimized = false;
  visible = true;
  loading = false;
  savingRole = false;
  deletingRoleId: number | null = null;
  deletingUser = false;
  loadingListado = false;
  showDeleteUserModal = false;
  deleteConfirmText = '';
  deletingTarget: UsuarioListadoItem | null = null;
  searchNombreListado = '';
  private searchNombreTimeout: ReturnType<typeof setTimeout> | null = null;
  readonly minSearchChars = 6;
  pageSize = 10;
  pageSizeOptions: number[] = [5, 10, 20, 30, 50, 100];
  currentPage = 1;
  isSearchMode = false;
  activeTab: TabKey = 'datos';
  searchIdentification = '';
  user: UserProfile | null = null;
  usuariosListado: UsuarioListadoItem[] = [];
  rolesCatalogo: DtoRolCatalogo[] = [];
  showAddRoleForm = false;
  newRole: NewRoleForm = {
    rolId: null,
    justificacion: '',
    fechaFin: ''
  };
  editingRole: UserRole | null = null;

  ngOnInit(): void {
    this.canAssignSuperAdministrador = this.authService.isCurrentUserSuperAdmin();
    this.cargarListadoUsuarios();
  }

  ngOnDestroy(): void {
    if (this.searchNombreTimeout) {
      clearTimeout(this.searchNombreTimeout);
    }
  }

  toggleMinimize(): void {
    this.minimized = !this.minimized;
  }

  closePanel(): void {
    this.visible = false;
  }

  prepararNuevoUsuario(): void {
    this.user = null;
    this.activeTab = 'datos';
    this.searchIdentification = '';
    this.showAddRoleForm = false;
    this.newRole = { rolId: null, justificacion: '', fechaFin: '' };
    this.rolesCatalogo = [];
  }

  consultarUsuario(): void {
    const documento = this.searchIdentification.trim();
    if (!documento) {
      this.toast.warning('Documento requerido', 'Digite una identificación para consultar.');
      return;
    }

    this.loading = true;
    this.toast.info('Consulta', 'Buscando información empresarial...');

    this.usuarioAdminService.consultarUsuarioPorIdentificacion(documento).subscribe({
      next: (resp) => {
        const funcionario = resp.funcionario ?? {};
        const nombres = (funcionario.nombres ?? '').trim();
        const apellidos = (funcionario.apellidos ?? '').trim();
        const nombreCompleto = `${nombres} ${apellidos}`.trim() || 'SIN NOMBRE';

        this.user = {
          identificacion: (funcionario.identificacion ?? documento).trim(),
          nombres,
          apellidos,
          grado: (funcionario.nombreGrado ?? funcionario.cargo ?? '').trim(),
          nombreCompleto,
          usuarioEmpresarial: (funcionario.usuario ?? '').trim(),
          email: (funcionario.correo ?? '').trim(),
          telefono: (funcionario.celular ?? '').trim(),
          situacionLaboral: (funcionario.situacionLaboral ?? '').trim(),
          unidad: (funcionario.dependencia ?? '').trim(),
          unidadFisica: (funcionario.siglaFisica ?? funcionario.siglaLaborando ?? '').trim(),
          cargo: (funcionario.cargo ?? '').trim(),
          gradAlfabetico: (funcionario.gradAlfabetico ?? funcionario.nombreGrado ?? '').trim(),
          funcionarioCodigo: (funcionario.funcionarioCodigo ?? '').trim(),
          undeLaborandoCodigo: (funcionario.undeLaborandoCodigo ?? '').trim(),
          codigoCargo: (funcionario.codigoCargo ?? '').trim(),
          activo: funcionario.activo ?? true,
          ultimoIngreso: 'Sin dato',
          fotoUrl: resp.fotoBase64 ?? undefined,
          roles: (resp.rolesAsignados ?? []).map((rol) => this.mapAssignedRole(rol))
        };
        this.rolesCatalogo = this.filtrarRolesCatalogo(resp.rolesCatalogo ?? []);
        this.showAddRoleForm = false;
        this.newRole = { rolId: null, justificacion: '', fechaFin: '' };

        this.loading = false;
        this.toast.success('Consulta exitosa', 'Se cargó la información del usuario.');
      },
      error: (err) => {
        this.loading = false;
        this.user = null;
        const errorResponse = err?.error;
        const errorMsg =
          errorResponse?.message ??
          (errorResponse?.success === false ? 'Operación fallida' : null) ??
          'No fue posible consultar el usuario.';
        this.toast.error('Consulta fallida', errorMsg);
      }
    });
  }

  setTab(tab: TabKey): void {
    this.activeTab = tab;
  }

  toggleEstadoUsuario(): void {
    if (!this.user) {
      return;
    }
    this.user.activo = !this.user.activo;
  }

 get activeRolesCount(): number {
    return this.user?.roles.filter(r => r.estado === 'Vigente').length ?? 0;
}

  get rolesVigentes(): UserRole[] {
    return this.user?.roles ?? [];
  }

  agregarRol(): void {
    if (!this.user) {
      this.toast.warning('Roles', 'Consulta un usuario antes de asignar roles.');
      return;
    }

    if (this.rolesCatalogo.length === 0) {
      this.toast.warning('Roles', 'No hay catálogo de roles disponible.');
      return;
    }

    // Calcular fecha por defecto (6 meses a partir de hoy)
    const fecha = new Date();
    fecha.setMonth(fecha.getMonth() + 6);
    this.newRole.fechaFin = this.normalizeDateString(fecha.toISOString());
    
    this.showAddRoleForm = true;
  }

  cancelarNuevoRol(): void {
    this.showAddRoleForm = false;
    this.editingRole = null;
    this.newRole = { rolId: null, justificacion: '', fechaFin: '' };
  }

  prepararEdicionRol(rol: UserRole): void {
    this.editingRole = rol;
    this.newRole = {
      rolId: rol.id,
      justificacion: rol.justificacion,
      fechaFin: rol.fechaExpiracion
    };
    this.showAddRoleForm = true;
    
    // Hacer scroll al formulario
    setTimeout(() => {
      document.querySelector('.role-add-form')?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  }

  guardarDatosUsuario(): void {
    if (!this.user) {
      this.toast.warning('Guardar datos', 'Primero consulta un usuario.');
      return;
    }

    if (!this.user.identificacion?.trim()) {
      this.toast.warning('Guardar datos', 'La identificación está vacía; vuelve a consultar el usuario.');
      return;
    }

    const username = (this.user.usuarioEmpresarial ?? '').trim();
    const funcionarioCodigo = Number((this.user.funcionarioCodigo ?? '').trim());
    const undeLaborandoCodigo = Number((this.user.undeLaborandoCodigo ?? '').trim());
    const codigoCargo = Number((this.user.codigoCargo ?? '').trim());

    if (!username) {
      this.toast.warning('Guardar datos', 'El usuario empresarial está vacío. Consulta nuevamente el funcionario.');
      return;
    }

    if (!Number.isFinite(funcionarioCodigo) || funcionarioCodigo <= 0) {
      this.toast.warning('Guardar datos', 'No se recibió código de funcionario válido. Vuelve a consultar la cédula.');
      return;
    }

    if (!Number.isFinite(undeLaborandoCodigo) || undeLaborandoCodigo <= 0) {
      this.toast.warning('Guardar datos', 'No se recibió código de unidad laborando válido. Vuelve a consultar la cédula.');
      return;
    }

    if (!Number.isFinite(codigoCargo) || codigoCargo <= 0) {
      this.toast.warning('Guardar datos', 'No se recibió código de cargo válido. Vuelve a consultar la cédula.');
      return;
    }

    const payload = {
      username,
      identificacion: this.user.identificacion,
      nombres: this.user.nombres,
      apellidos: this.user.apellidos,
      email: this.user.email,
      gradAlfabetico: this.user.gradAlfabetico || this.user.grado,
      funcionario: String(funcionarioCodigo),
      undeLaborando: String(undeLaborandoCodigo),
      codigoCargo: String(codigoCargo),
      activo: this.user.activo
    };

    this.loading = true;
    this.usuarioAdminService.guardarUsuario(payload).subscribe({
      next: (resp) => {
        this.loading = false;
        if (!resp?.success) {
          this.toast.warning('Guardar datos', resp?.message || 'No fue posible guardar.');
          return;
        }
        this.toast.success('Guardar datos', resp.message || 'Usuario guardado correctamente.');
        this.cargarListadoUsuarios();
      },
      error: (err) => {
        this.loading = false;
        const backendMessage =
          err?.error?.message ??
          err?.error?.Message ??
          err?.error?.detail ??
          err?.message;
        this.toast.error(
          'Guardar datos',
          backendMessage ?? 'Se presentó un error guardando el usuario.'
        );
      }
    });
  }

  guardarRoles(): void {
    if (!this.user) {
      this.toast.warning('Roles', 'Consulta un usuario primero.');
      return;
    }

    if (!this.showAddRoleForm) {
      this.toast.info('Roles', 'No hay cambios pendientes en roles.');
      return;
    }

    if (!this.newRole.rolId || this.newRole.rolId <= 0) {
      this.toast.warning('Roles', 'Selecciona un rol.');
      return;
    }

    if (!this.newRole.justificacion.trim()) {
      this.toast.warning('Roles', 'La justificación es obligatoria.');
      return;
    }

    if (!this.newRole.fechaFin) {
      this.toast.warning('Roles', 'La fecha fin es obligatoria.');
      return;
    }

    if (this.editingRole === null) {
      const rolCatalogo = this.rolesCatalogo.find(r => r.id === this.newRole.rolId);
      const rolNombre = (rolCatalogo?.nombre ?? '').trim().toUpperCase();
      const yaAsignado = this.user.roles.some(r =>
        r.nombre.trim().toUpperCase() === rolNombre
        && r.estado === 'Vigente'
      );
      if (yaAsignado) {
        this.toast.warning('Roles', 'El usuario ya tiene este rol asignado y vigente.');
        return;
      }
    }

    this.savingRole = true;

    if (this.editingRole !== null) {
      const editPayload: EditarRolRequest = {
        usuario: this.user.usuarioEmpresarial,
        identificacion: this.user.identificacion,
        justificacion: this.newRole.justificacion.trim(),
        fechaFin: this.newRole.fechaFin
      };
      this.usuarioAdminService.editarRol(this.editingRole.id, editPayload).subscribe({
        next: (resp) => {
          this.savingRole = false;
          if (!resp?.success) {
            this.toast.warning('Roles', resp?.message || 'No fue posible editar el rol.');
            return;
          }
          this.toast.success('Roles', resp.message || 'Rol actualizado correctamente.');
          this.cancelarNuevoRol();
          this.consultarUsuario();
          this.cargarListadoUsuarios();
        },
        error: (err) => {
          this.savingRole = false;
          this.toast.error('Roles', err?.error?.detail ?? err?.error?.message ?? 'Error editando rol.');
        }
      });
      return;
    }

    this.usuarioAdminService.asignarRol({
      usuarioId: 0,
      usuario: this.user.usuarioEmpresarial,
      identificacion: this.user.identificacion,
      rolId: this.newRole.rolId,
      justificacion: this.newRole.justificacion.trim(),
      fechaFin: this.newRole.fechaFin,
      vigente: 1
    }).subscribe({
      next: (resp) => {
        this.savingRole = false;
        if (!resp?.success) {
          this.toast.warning('Roles', resp?.message || 'No fue posible asignar el rol.');
          return;
        }

        this.toast.success('Roles', resp.message || 'Rol asignado correctamente.');
        this.cancelarNuevoRol();
        this.consultarUsuario();
        this.cargarListadoUsuarios();
      },
      error: (err) => {
        this.savingRole = false;
        this.toast.error('Roles', err?.error?.detail ?? err?.error?.message ?? 'Error asignando rol.');
      }
    });
  }

  async eliminarRol(rol: UserRole): Promise<void> {
    if (!this.user) {
      this.toast.warning('Roles', 'Consulta un usuario primero.');
      return;
    }

    const confirmed = await this.alert.confirm(
      'Retirar rol',
      `¿Deseas retirar el rol "${rol.nombre}" del usuario?`,
      'Retirar',
      'Cancelar'
    );
    if (!confirmed) {
      return;
    }

    this.deletingRoleId = rol.id;
    this.usuarioAdminService.eliminarRol(rol.id, this.user.usuarioEmpresarial, this.user.identificacion).subscribe({
      next: (resp) => {
        this.deletingRoleId = null;
        if (!resp?.success) {
          this.toast.warning('Roles', resp?.message || 'No fue posible retirar el rol.');
          return;
        }
        this.toast.success('Roles', resp.message || 'Rol retirado correctamente.');
        this.consultarUsuario();
        this.cargarListadoUsuarios();
      },
      error: (err) => {
        this.deletingRoleId = null;
        this.toast.error('Roles', err?.error?.detail ?? err?.error?.message ?? 'Error retirando rol.');
      }
    });
  }

  cargarListadoUsuarios(): void {
    this.loadingListado = true;
    const term = (this.searchNombreListado ?? '').trim();
    const hasSearchTerm = term.length > 0;

    if (hasSearchTerm && term.length < this.minSearchChars) {
      this.isSearchMode = true;
      this.usuariosListado = [];
      this.currentPage = 1;
      this.loadingListado = false;
      return;
    }

    this.isSearchMode = hasSearchTerm;
    const query = this.isSearchMode ? term : '';

    this.usuarioAdminService.getListadoUsuarios(query).subscribe({
      next: (items) => {
        this.usuariosListado = (items ?? []).map((x) => ({
          ...x,
          fechaFinRol: this.normalizeDateString(x?.fechaFinRol ?? '')
        }));
        this.currentPage = 1;
        this.loadingListado = false;
      },
      error: () => {
        this.usuariosListado = [];
        this.currentPage = 1;
        this.loadingListado = false;
      }
    });
  }

  onSearchNombreListadoChange(): void {
    if (this.searchNombreTimeout) {
      clearTimeout(this.searchNombreTimeout);
    }

    this.searchNombreTimeout = setTimeout(() => {
      this.cargarListadoUsuarios();
    }, 350);
  }

  get totalUsuariosListado(): number {
    return this.usuariosListado.length;
  }

  get totalPaginasListado(): number {
    return Math.max(1, Math.ceil(this.totalUsuariosListado / this.pageSize));
  }

  /**
   * Page numbers for compact pagination UI.
   * If total pages <= 11, show all. Otherwise show 1..10, '...', last.
   */
  get pageNumbers(): Array<number | string> {
    const total = this.totalPaginasListado;
    const current = this.currentPage;
    const window = 5; // pages before/after

    // If total small, show all
    if (total <= window * 2 + 5) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    const pages: Array<number | string> = [];
    // always show first
    pages.push(1);

    let start = Math.max(2, current - window);
    let end = Math.min(total - 1, current + window);

    // adjust when close to boundaries
    if (start <= 2) start = 2;
    if (end >= total - 1) end = total - 1;

    if (start > 2) pages.push('...');

    for (let i = start; i <= end; i++) pages.push(i);

    if (end < total - 1) pages.push('...');

    pages.push(total);
    return pages;
  }

  goToPage(page: number): void {
    if (!Number.isFinite(page) || page < 1 || page > this.totalPaginasListado) return;
    this.currentPage = page;
  }

  onPageClick(p: number | string): void {
    if (p === '...') return;
    const n = Number(p);
    if (Number.isFinite(n)) this.goToPage(n);
  }

  onPageSizeChange(size: number | string): void {
    const n = Number(size) || 10;
    console.log('[DEBUG] onPageSizeChange start', { requestedSize: n, currentPageBefore: this.currentPage, pageSizeBefore: this.pageSize, totalItems: this.usuariosListado.length });

    // compute last page based on the new page size before applying it
    const totalItems = this.usuariosListado.length;
    const lastPage = Math.max(1, Math.ceil(totalItems / n));

    this.pageSize = n;
    console.log('[DEBUG] onPageSizeChange - applied pageSize', { pageSize: this.pageSize, lastPage });

    // Ensure currentPage is within bounds for the new page size
    this.currentPage = Math.min(this.currentPage, lastPage);
    console.log('[DEBUG] onPageSizeChange - clamped currentPage', this.currentPage);

    // If the current page (after resize) has no items, jump to last page that has items
    const pageItemsNow = this.usuariosListadoPaginado;
    console.log('[DEBUG] onPageSizeChange - pageItemsNow.length', pageItemsNow.length);
    if (pageItemsNow.length === 0 && totalItems > 0) {
      this.currentPage = lastPage;
      console.log('[DEBUG] onPageSizeChange - jumped to lastPage', lastPage);
    }

    console.log('[DEBUG] onPageSizeChange end', { currentPageAfter: this.currentPage, pageSizeAfter: this.pageSize });
  }

  get usuariosListadoPaginado(): UsuarioListadoItem[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.usuariosListado.slice(start, start + this.pageSize);
  }

  canGoPrevListado(): boolean {
    return this.currentPage > 1;
  }

  canGoNextListado(): boolean {
    return this.currentPage < this.totalPaginasListado;
  }

  prevPaginaListado(): void {
    if (!this.canGoPrevListado()) {
      return;
    }
    this.currentPage -= 1;
  }

  nextPaginaListado(): void {
    if (!this.canGoNextListado()) {
      return;
    }
    this.currentPage += 1;
  }

  editarDesdeListado(item: UsuarioListadoItem): void {
    const identificacion = String(item?.identificacion ?? '').trim();
    if (!identificacion) {
      this.toast.warning('Usuarios', 'El registro no tiene identificación para cargar edición.');
      return;
    }

    this.searchIdentification = identificacion;
    this.activeTab = 'datos';
    this.consultarUsuario();
  }

  abrirModalEliminarUsuario(item: UsuarioListadoItem): void {
    this.deletingTarget = item;
    this.deleteConfirmText = '';
    this.showDeleteUserModal = true;
  }

  cerrarModalEliminarUsuario(): void {
    if (this.deletingUser) {
      return;
    }
    this.showDeleteUserModal = false;
    this.deleteConfirmText = '';
    this.deletingTarget = null;
  }

  confirmarEliminarUsuario(): void {
    if (!this.deletingTarget) {
      return;
    }

    if (this.deleteConfirmText.trim().toUpperCase() !== 'ELIMINAR') {
      this.toast.warning('Usuarios', 'Debes escribir ELIMINAR para confirmar.');
      return;
    }

    this.deletingUser = true;
    this.usuarioAdminService.eliminarUsuario(this.deletingTarget.idUsuario).subscribe({
      next: (resp) => {
        this.deletingUser = false;
        if (!resp?.success) {
          this.toast.warning('Usuarios', resp?.message || 'No fue posible eliminar el usuario.');
          return;
        }
        this.toast.success('Usuarios', resp.message || 'Usuario eliminado correctamente.');
        this.cerrarModalEliminarUsuario();
        this.cargarListadoUsuarios();

        if (this.user?.identificacion?.trim() === String(this.deletingTarget?.identificacion ?? '').trim()) {
          this.user = null;
          this.activeTab = 'datos';
        }
      },
      error: (err) => {
        this.deletingUser = false;
        this.toast.error('Usuarios', err?.error?.detail ?? err?.error?.message ?? 'Error eliminando usuario.');
      }
    });
  }

  impersonarUsuarioActual(): void {
    if (!this.user) {
      this.toast.warning('Impersonación', 'No hay usuario seleccionado.');
      return;
    }

    const usuarioListadoItem: UsuarioListadoItem = {
      idUsuario: 0,
      identificacion: this.user.identificacion,
      username: this.user.usuarioEmpresarial,
      nombreCompleto: this.user.nombreCompleto,
      rol: this.user.roles?.[0]?.nombre || '',
      fechaFinRol: this.user.roles?.[0]?.fechaExpiracion
    };

    this.impersonarDesdeListado(usuarioListadoItem);
  }

  impersonarDesdeListado(item: UsuarioListadoItem): void {
    const username = String(item?.username ?? '').trim();
    if (!username) {
      this.toast.warning('Impersonación', 'El usuario seleccionado no tiene username válido.');
      return;
    }

    const userActual = (this.authService.getUsuario() ?? '').trim().toLowerCase();
    if (userActual && userActual === username.toLowerCase()) {
      this.toast.info('Impersonación', 'Ya estás autenticado con ese usuario.');
      return;
    }

    this.authService.startImpersonation(username).subscribe({
      next: (resp) => {
        if (!this.authService.isLoginSuccessful(resp) || !resp?.token) {
          this.toast.error('Impersonación', resp?.message ?? 'No fue posible iniciar impersonación.');
          return;
        }

        this.toast.success('Impersonación', `Ahora navegas como ${username}.`);
        this.router.navigate(['/home']).then(() => window.location.reload());
      },
      error: (err) => {
        this.toast.error(
          'Impersonación',
          err?.error?.message ?? err?.error?.detail ?? 'No fue posible iniciar impersonación.'
        );
      }
    });
  }

  private mapAssignedRole(rol: any): UserRole {
    const fechaExpiracion = this.normalizeDateString(rol?.fechaFin ?? rol?.fecha_fin ?? '');
    const estadoBase = String(rol?.estado ?? '').trim().toLowerCase();
    const estado: RolEstado = estadoBase.includes('venc') ? 'Vencido' : 'Vigente';

    return {
      id: Number(rol?.id ?? 0),
      nombre: String(rol?.rol ?? '').trim() || `Rol ${rol?.id}`,
      fechaExpiracion,
      estado,
      justificacion: String(rol?.justificacion ?? '').trim()
    };
  }

  private normalizeDateString(raw: string): string {
    const value = String(raw ?? '').trim();
    if (!value) {
      return '';
    }

    const onlyDate = value.includes('T') ? value.split('T')[0] : value;
    if (/^\d{4}-\d{2}-\d{2}$/.test(onlyDate)) {
      return onlyDate;
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      return value;
    }

    const y = parsed.getFullYear();
    const m = String(parsed.getMonth() + 1).padStart(2, '0');
    const d = String(parsed.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  tiempoRestante(fechaExpiracion: string): string {
    if (!fechaExpiracion) return 'Sin fecha';
    const hoy = new Date();
    hoy.setHours(0, 0, 0, 0);
    const fin = new Date(fechaExpiracion + 'T00:00:00');
    if (Number.isNaN(fin.getTime())) return 'Sin fecha';

    const diff = fin.getTime() - hoy.getTime();
    if (diff < 0) return 'Vencido';
    if (diff === 0) return 'Vence hoy';

    let anios = fin.getFullYear() - hoy.getFullYear();
    let meses = fin.getMonth() - hoy.getMonth();
    let dias  = fin.getDate()  - hoy.getDate();

    if (dias < 0) {
      meses--;
      const ultimoDiaMesAnterior = new Date(fin.getFullYear(), fin.getMonth(), 0).getDate();
      dias += ultimoDiaMesAnterior;
    }
    if (meses < 0) { anios--; meses += 12; }

    const partes: string[] = [];
    if (anios > 0)  partes.push(`${anios} ${anios === 1 ? 'año' : 'años'}`);
    if (meses > 0)  partes.push(`${meses} ${meses === 1 ? 'mes' : 'meses'}`);
    if (dias > 0)   partes.push(`${dias} ${dias === 1 ? 'día' : 'días'}`);
    return partes.length ? partes.join(', ') : 'Vence hoy';
  }

  private filtrarRolesCatalogo(roles: DtoRolCatalogo[]): DtoRolCatalogo[] {
    if (!this.canAssignSuperAdministrador) {
      return [];
    }
    return roles ?? [];
  }
}
