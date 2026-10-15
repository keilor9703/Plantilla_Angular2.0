import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { SidebarService } from '../../services/sidebar.js';
import { AuthService } from '../../core/auth/auth.service';
import { environment } from '../../../environments/environment';
import { BrandingService } from '../../core/services/administracion/branding.service';
import { MenuService, DbMenuItem } from '../../core/services/administracion/menu.service';
import { finalize } from 'rxjs/operators';

interface SearchResult {
  label: string;
  route: string;
  icon: string;
  isExternal?: boolean;
}

interface Notification {
  id: number;
  icon: string;
  color: string;
  count: number;
  tooltip: string;
}

interface MiPerfilDto {
  identificacion?: string;
  gradAlfabetico?: string;
  GradAlfabetico?: string;
  grado?: string;
  nombreCompleto?: string;
  cargo?: string;
  situacionLaboral?: string;
  tiempoServicio?: string;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './header.html',
  styleUrls: ['./header.scss']
})
export class HeaderComponent implements OnInit {
  isUserDropdownOpen = false;
  userPhotoUrl: string | null = null;
  profileModalOpen = false;
  profileLoading = false;
  profileSummaryLoading = true;
  perfil: MiPerfilDto | null = null;

  userName = 'Usuario';
  userRole = 'OFTIC';
  private brandingRole = 'OFTIC';

  constructor(
    private SidebarService: SidebarService,
    private authService: AuthService,
    private http: HttpClient,
    private router: Router,
    private brandingService: BrandingService,
    private menuService: MenuService
  ) {}

  allSearchItems: SearchResult[] = [];
  searchResults: SearchResult[] = [];
  showSearchDropdown = false;
  searchActiveIndex = -1;

  notifications: Notification[] = [];
  searchQuery: string = '';

  ngOnInit(): void {
    this.userName = this.authService.getUsuario();
    this.loadBranding();
    this.loadMyPhoto();
    this.loadMyProfile(false);
    this.loadSearchIndex();
  }

  toggleMenu() {
    this.SidebarService.toggleSidebar();
  }

  toggleUserDropdown(event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.isUserDropdownOpen = !this.isUserDropdownOpen;
  }

  closeUserDropdown(): void {
    this.isUserDropdownOpen = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;

    if (this.isUserDropdownOpen && !target.closest('.header-user')) {
      this.closeUserDropdown();
    }

    if (this.showSearchDropdown && !target.closest('.header-search')) {
      this.showSearchDropdown = false;
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeUserDropdown();
    this.closeProfileModal();
    this.showSearchDropdown = false;
  }

  get perfilGrado(): string {
    return (
      this.perfil?.gradAlfabetico ??
      this.perfil?.GradAlfabetico ??
      this.perfil?.grado ??
      ''
    ).trim();
  }

  get shouldScrollUserRole(): boolean {
    return this.userRole.length > 28;
  }

  get hasProfileSummary(): boolean {
    return !this.profileSummaryLoading && !!(this.userName || this.userRole);
  }

  private loadBranding(): void {
    this.brandingService.getPublicConfig().subscribe({
      next: (cfg) => {
        const sigla = (cfg?.sistema ?? cfg?.sigla ?? cfg?.systemName ?? '').trim();
        this.brandingRole = sigla || 'OFTIC';
        this.applyProfileSummary();
      },
      error: () => {
        this.brandingRole = 'OFTIC';
        this.applyProfileSummary();
      }
    });
  }

  private loadSearchIndex(): void {
    this.menuService.getMyMenu().subscribe({
      next: (items) => {
        this.allSearchItems = this.flattenMenuItems(items ?? []);
      },
      error: () => {
        const userId = this.authService.getUserId();
        if (!userId) return;
        this.menuService.getByUser(userId).subscribe({
          next: (items) => {
            this.allSearchItems = this.flattenMenuItems(items ?? []);
          }
        });
      }
    });
  }

  private flattenMenuItems(items: DbMenuItem[]): SearchResult[] {
    const normalized = items.map((x: any) => ({
      idMenu: Number(x.idMenu ?? x.IdMenu ?? 0),
      descripcion: String(x.descripcion ?? x.Descripcion ?? ''),
      idPadre: Number(x.idPadre ?? x.IdPadre ?? 0),
      posicion: Number(x.posicion ?? x.Posicion ?? 0),
      icono: x.icono ?? x.Icono,
      detalle: x.detalle ?? x.Detalle,
      vigente: Number(x.vigente ?? x.Vigente ?? 1)
    }));

    const active = normalized.filter(x => !!x.descripcion && x.vigente === 1);
    const byId = new Map(active.map(x => [x.idMenu, x]));

    const results: SearchResult[] = [];

    for (const item of active) {
      const route = this.resolveRoute(item.detalle);
      if (!route) continue;
      const isExternal = route.startsWith('http://') || route.startsWith('https://');
      const parent = byId.get(item.idPadre);
      const label = parent && parent.idMenu !== 1
        ? `${parent.descripcion} › ${item.descripcion}`
        : item.descripcion;
      const icon = this.resolveIcon(item.icono ?? parent?.icono);
      results.push({ label, route, icon, isExternal });
    }

    return results;
  }

  private resolveRoute(raw: unknown): string {
    const value = String(raw ?? '').trim();
    if (!value) return '';
    if (value.startsWith('http://') || value.startsWith('https://') || value.startsWith('www.')) {
      return value.startsWith('www.') ? 'https://' + value : value;
    }
    if (value.startsWith('/administracion/')) return value;
    const configRoutes = ['/video-unidad', '/configuracion-imagen-sitio', '/admin-multimedia', '/configuracion-sistema'];
    if (configRoutes.includes(value)) return '/administracion/configuracion-sistema';
    const routeMap: Record<string, string> = {
      '/formularios': '/administracion/formularios',
      '/usuarios': '/administracion/usuarios',
      '/roles': '/administracion/roles',
      '/linea-mando': '/administracion/linea-mando',
      '/radio': '/administracion/radio',
      '/menu': '/administracion/menu',
      '/auditoria': '/administracion/auditoria',
      '/sliders': '/administracion/sliders'
    };
    return routeMap[value] || (value.startsWith('/') ? value : `/${value}`);
  }

  private resolveIcon(raw: unknown): string {
    const value = String(raw ?? '').trim();
    if (!value) return 'fa-solid fa-circle-dot';
    return value.includes('fa-') ? value : `fa-solid ${value}`;
  }

  private normalizeText(text: string): string {
    return text.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  }

  onSearchInput(): void {
    const q = this.normalizeText(this.searchQuery ?? '');
    this.searchActiveIndex = -1;
    if (!q.trim()) {
      this.searchResults = [];
      this.showSearchDropdown = false;
      return;
    }
    this.searchResults = this.allSearchItems
      .filter(r => this.normalizeText(r.label).includes(q))
      .slice(0, 8);
    this.showSearchDropdown = true;
  }

  onSearchKeydown(event: KeyboardEvent): void {
    if (!this.showSearchDropdown || this.searchResults.length === 0) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.searchActiveIndex = Math.min(this.searchActiveIndex + 1, this.searchResults.length - 1);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.searchActiveIndex = Math.max(this.searchActiveIndex - 1, -1);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (this.searchActiveIndex >= 0) {
        this.navigateSearchResult(this.searchResults[this.searchActiveIndex]);
      } else if (this.searchResults.length === 1) {
        this.navigateSearchResult(this.searchResults[0]);
      }
    }
  }

  onSearch(): void {
    if (this.searchActiveIndex >= 0 && this.searchResults[this.searchActiveIndex]) {
      this.navigateSearchResult(this.searchResults[this.searchActiveIndex]);
    } else if (this.searchResults.length === 1) {
      this.navigateSearchResult(this.searchResults[0]);
    }
  }

  navigateSearchResult(result: SearchResult): void {
    this.showSearchDropdown = false;
    this.searchQuery = '';
    this.searchResults = [];
    this.searchActiveIndex = -1;
    if (result.isExternal) {
      window.open(result.route, '_blank', 'noopener,noreferrer');
    } else {
      this.router.navigate([result.route]);
    }
  }

  closeSearchDropdown(): void {
    this.showSearchDropdown = false;
    this.searchActiveIndex = -1;
  }

  onNotificationClick(notification: Notification): void {
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  isImpersonating(): boolean {
    return this.authService.isImpersonating();
  }

  getImpersonationOriginalUser(): string {
    return this.authService.getOriginalUser();
  }

  stopImpersonation(): void {
    const restored = this.authService.stopImpersonation();
    this.closeUserDropdown();
    if (!restored) {
      this.router.navigate(['/login']);
      return;
    }

    this.userName = this.authService.getUsuario();
    this.router.navigate(['/home']).then(() => window.location.reload());
  }

  private loadMyPhoto(): void {
    this.http
      .get(`${environment.apiBaseUrl}/Usuario/MiFoto`, { responseType: 'text' })
      .subscribe({
        next: (raw) => {
          this.userPhotoUrl = this.normalizePhoto(raw);
        },
        error: () => {
          this.userPhotoUrl = null;
        }
      });
  }

  private normalizePhoto(raw: string | null): string | null {
    if (!raw) {
      return null;
    }

    const trimmed = raw.trim();
    if (!trimmed) {
      return null;
    }

    let decoded = trimmed;
    try {
      if (trimmed.startsWith('"') && trimmed.endsWith('"')) {
        decoded = JSON.parse(trimmed);
      }
    } catch {
      decoded = trimmed;
    }

    if (!decoded || decoded.length < 20) {
      return null;
    }

    if (decoded.startsWith('data:image/')) {
      return decoded;
    }

    return `data:image/jpeg;base64,${decoded}`;
  }

  private loadMyProfile(showLoading: boolean): void {
    this.profileLoading = showLoading;
    this.profileSummaryLoading = !this.perfil;

    this.http.get<MiPerfilDto>(`${environment.apiBaseUrl}/Usuario/MiPerfil`).subscribe({
      next: (data) => {
        this.perfil = data ?? {};
        this.profileLoading = false;
        this.profileSummaryLoading = false;
        this.applyProfileSummary();
      },
      error: () => {
        this.profileLoading = false;
        this.profileSummaryLoading = false;
        this.applyProfileSummary();
      }
    });
  }

  private applyProfileSummary(): void {
    const name = (this.perfil?.nombreCompleto ?? '').trim();
    const grade = this.perfilGrado;
    const cargo = (this.perfil?.cargo ?? '').trim();

    this.userName = name ? [grade, name].filter(Boolean).join(' ') : this.authService.getUsuario();
    this.userRole = cargo || this.brandingRole;
  }

  openProfileModal(): void {
    this.profileModalOpen = true;
    this.closeUserDropdown();

    if (!this.perfil && !this.profileLoading) {
      this.loadMyProfile(true);
    }
  }

  closeProfileModal(): void {
    this.profileModalOpen = false;
  }
}
