import { Component, HostBinding, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SidebarService } from '../../services/sidebar';
import { MenuService, DbMenuItem } from '../../core/services/administracion/menu.service';
import { AuthService } from '../../core/auth/auth.service';
import { BrandingService } from '../../core/services/administracion/branding.service';

interface SubMenuItem {
  id: number;
  label: string;
  route: string;
  icon?: string;
  isExternal?: boolean;
}

interface MenuItem {
  id: number;
  icon: string;
  label: string;
  route?: string;
  isExternal?: boolean;
  submenu?: SubMenuItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.scss'],
})
export class SidebarComponent implements OnInit {
  expandedKey: string | null = null;


  submenuPos: { top: number; left: number } | null = null;

  private flyoutWidth = 240;
  private flyoutGap = 12;
  private flyoutMinViewportPadding = 10;

  constructor(
    public sidebarService: SidebarService,
    private menuService: MenuService,
    private authService: AuthService,
    private brandingService: BrandingService
  ) {}

  ngOnInit(): void {
    this.loadBranding();
    this.loadMenuFromDb();
  }


  @HostBinding('class.expanded')
  get expanded() {
    return this.sidebarService.isOpen();
  }

  isOpen() {
    return this.sidebarService.isOpen();
  }

  isExpandedItem(item: MenuItem) {
    return this.expandedKey === item.label;
  }

  toggleItem(item: MenuItem, ev: MouseEvent) {
    ev.preventDefault();
    ev.stopPropagation();

    const opening = !this.isExpandedItem(item);
    this.expandedKey = opening ? item.label : null;

    if (!opening) {
     this.submenuPos = null;
    return;
    }

    const target = ev.currentTarget as HTMLElement;
    const menuItemEl = target.closest('.menu-item') as HTMLElement | null;
    const menuLinkEl = menuItemEl?.querySelector('.menu-link') as HTMLElement | null;
    const sidebarEl = menuItemEl?.closest('aside.sidebar') as HTMLElement | null;

    if (!menuLinkEl || !sidebarEl) return;
    
    const linkRect = menuLinkEl.getBoundingClientRect();
    const sidebarRect = sidebarEl.getBoundingClientRect();
    
     const left = sidebarRect.right - this.flyoutGap;

  
    const top = linkRect.top;

    this.submenuPos = { top, left };

  }



  getSubmenuStyle() {
  if (!this.submenuPos) return {};
  return {
    top: `${this.submenuPos.top}px`,
    left: `${this.submenuPos.left}px`,
    width: `${this.flyoutWidth}px`,
  };
}
onItemTap(item: MenuItem, ev: MouseEvent) {
  if (item.submenu?.length) {
    ev.preventDefault();
    ev.stopPropagation();

    const opening = !this.isExpandedItem(item);
    this.expandedKey = opening ? item.label : null;

    if (!opening) {
      this.submenuPos = null;
      return;
    }

    const menuLinkEl = ev.currentTarget as HTMLElement;
    const menuItemEl = menuLinkEl.closest('.menu-item') as HTMLElement | null;
    const sidebarEl = menuItemEl?.closest('aside.sidebar') as HTMLElement | null;
    if (!sidebarEl) return;

    const linkRect = menuLinkEl.getBoundingClientRect();
    const sidebarRect = sidebarEl.getBoundingClientRect();

    const left = sidebarRect.right - this.flyoutGap;
    const top = linkRect.top;

    this.submenuPos = { top, left };
    return;
  }

  this.onNavigate();
}
  
  onNavigate() {
      this.sidebarService.closeSidebar();
      this.expandedKey = null;
      this.submenuPos = null;
  }

  closeSidebar() {
    this.sidebarService.closeSidebar();
    this.expandedKey = null;
    this.submenuPos = null;
    
  }

  @HostListener('document:keydown.escape')
  onEsc() {
    if (this.sidebarService.isOpen()) this.sidebarService.closeSidebar();
  }

  menuItems: MenuItem[] = [];
  systemName = 'OFTIC';
  logoUrl = '/imagenes/oftic-logo-app.png';

  private loadBranding(): void {
    this.brandingService.getPublicConfig().subscribe({
      next: (cfg) => {
        const name = (cfg?.sistema ?? cfg?.sigla ?? '').trim();
        this.systemName = name || 'OFTIC';
        this.logoUrl = (cfg?.logoUrl ?? '').trim() || '/imagenes/oftic-logo-app.png';
      },
      error: () => {
        this.systemName = 'OFTIC';
        this.logoUrl = '/imagenes/oftic-logo-app.png';
      }
    });
  }

  private loadMenuFromDb(): void {
    this.menuService.getMyMenu().subscribe({
      next: (items) => {
        if (!items || items.length === 0) {
          this.loadMenuByUserFallback();
          return;
        }
        const mapped = this.mapDbMenu(items);
        this.menuItems = this.orderAndSortMenu(mapped);
      },
      error: () => {
        this.loadMenuByUserFallback();
      }
    });
  }

  private loadMenuByUserFallback(): void {
    const userId = this.authService.getUserId();
    if (!userId) {
      this.menuItems = [];
      return;
    }

    this.menuService.getByUser(userId).subscribe({
      next: (items) => {
        this.menuItems = this.orderAndSortMenu(this.mapDbMenu(items));
      },
      error: () => {
        this.menuItems = this.orderAndSortMenu([]);
      }
    });
  }

  private mapDbMenu(items: DbMenuItem[]): MenuItem[] {
    const normalized = (items ?? []).map((x: any) => ({
      idMenu: Number(x.idMenu ?? x.IdMenu ?? 0),
      // Acepta titulo (schema nuevo) o descripcion (schema Oracle legacy)
      descripcion: String(x.titulo ?? x.descripcion ?? x.Titulo ?? x.Descripcion ?? ''),
      idPadre: Number(x.idPadre ?? x.IdPadre ?? x.id_padre ?? 0),
      posicion: Number(x.posicion ?? x.Posicion ?? x.orden ?? x.Orden ?? 1),
      icono: x.icono ?? x.Icono,
      // Acepta ruta (schema nuevo) o detalle (schema Oracle legacy)
      detalle: x.ruta ?? x.detalle ?? x.Ruta ?? x.Detalle,
      // Acepta estado (schema nuevo) o vigente (schema Oracle legacy); si es null/undefined → activo
      vigente: Number(x.estado ?? x.vigente ?? x.Estado ?? x.Vigente ?? 1)
    }));

    const active = normalized.filter(x => !!x.descripcion && x.vigente === 1);
    const byId = new Map(active.map(x => [x.idMenu, x]));

    // Raíces: idPadre === 0, o idPadre apunta a un id que no existe en la lista
    const parents = active
      .filter(x => {
        return x.idPadre === 0 || x.idPadre === 1 || !byId.has(x.idPadre);
      })
      .filter(x => x.idMenu !== 1)
      .sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es', { sensitivity: 'base' }));

    if (parents.length === 0) {
      return [];
    }

    const topLevel = parents.map(parent => {
      const directChildren = active
        .filter(child => child.idPadre === parent.idMenu)
        .sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es', { sensitivity: 'base' }));

      const subs = directChildren
        .map(child => {
          const route = this.normalizeRoute(child.detalle);
          return {
            id: child.idMenu,
            route: route,
            label: this.normalizeLabel(child.descripcion, route),
            icon: this.normalizeIcon(child.icono),
            isExternal: this.isExternalUrl(route)
          };
        })
        .filter(sub => !!sub.route)
        .sort((a, b) => a.label.localeCompare(b.label, 'es', { sensitivity: 'base' }));

      const route = this.normalizeRoute(parent.detalle);
      const label = this.normalizeLabel(parent.descripcion, route);
      const icon = this.normalizeIcon(parent.icono);
      const isExternal = this.isExternalUrl(route);

      if (subs.length > 0) {
        return {
          id: parent.idMenu,
          icon,
          label,
          submenu: subs
        } as MenuItem;
      }

      // Si no hay hijos y no hay ruta, no se renderiza (nodo contenedor huérfano).
      if (!route) {
        return null;
      }

      return {
        id: parent.idMenu,
        icon,
        label,
        route,
        isExternal
      } as MenuItem;
    });

    return topLevel
      .filter((x): x is MenuItem => x !== null)
      .sort((a, b) => a.label.localeCompare(b.label, 'es', { sensitivity: 'base' }));
  }

  private normalizeRoute(raw: unknown): string {
    const value = String(raw ?? '').trim();
    if (!value) {
      return '';
    }

    // URLs externas
    if (value.startsWith('http://') || value.startsWith('https://') || value.startsWith('www.')) {
      if (value.startsWith('www.') && !value.startsWith('http')) {
        return 'https://' + value;
      }
      return value;
    }

    // Rutas que ya están completas
    if (value.startsWith('/administracion/') || value === '/administracion') {
      return value;
    }

    // Ruta home
    if (value === '/home' || value === 'home') {
      return '/home';
    }

    // Rutas que van a configuracion-sistema
    const configRoutes = [
      '/video-unidad',
      '/configuracion-imagen-sitio',
      '/admin-multimedia',
      '/configuracion-sistema'
    ];
    if (configRoutes.includes(value)) {
      return '/administracion/configuracion-sistema';
    }

    // Rutas simples que necesitan prefijo /administracion/
    const routeMap: Record<string, string> = {
      '/formularios': '/administracion/formularios',
      '/usuarios': '/administracion/usuarios',
      '/roles': '/administracion/roles',
      '/linea-mando': '/administracion/linea-mando',
      '/radio': '/administracion/radio',
      '/menu': '/administracion/menu',
      '/auditoria': '/administracion/auditoria',
      '/sliders': '/administracion/sliders',
      '/branding': '/administracion/branding',
      '/personaje-mes': '/administracion/personaje-mes',
      '/personajes': '/administracion/personaje-mes',
      '/dominio': '/administracion/dominio'
    };

    return routeMap[value] || (value.startsWith('/') ? value : `/${value}`);
  }

  private isExternalUrl(url: string): boolean {
    return url.startsWith('http://') || url.startsWith('https://') || url.startsWith('www.');
  }

  private normalizeLabel(label: string, route: string): string {
    if (
      route === '/administracion/configuracion-sistema' ||
      route === '/administracion/admin-multimedia' ||
      route === '/administracion/video-unidad' ||
      route === '/administracion/configuracion-imagen-sitio' ||
      route === '/configuracion-sistema' ||
      route === '/video-unidad' ||
      route === '/configuracion-imagen-sitio' ||
      route === '/admin-multimedia'
    ) {
      return 'Configuracion sistema';
    }
    return label;
  }

  private normalizeIcon(raw: unknown): string {
    const value = String(raw ?? '').trim();
    if (!value) {
      return 'fa-solid fa-folder';
    }
    return value.includes('fa-') ? value : `fa-solid ${value}`;
  }

  private groupAdministrationItems(items: MenuItem[]): MenuItem[] {
    const kept: MenuItem[] = [];
    const adminSubmenu: SubMenuItem[] = [];

    for (const item of items) {
      // Caso menú simple: item directo con ruta de administración.
      if (item.route && this.isAdministrationRoute(item.route)) {
        adminSubmenu.push({ id: item.id, label: item.label, route: item.route, icon: item.icon });
        continue;
      }

      // Caso menú con submenús: separa los de administración.
      if (item.submenu?.length) {
        const adminSubs = item.submenu.filter((sub) => this.isAdministrationRoute(sub.route));
        const normalSubs = item.submenu.filter((sub) => !this.isAdministrationRoute(sub.route));

        adminSubmenu.push(...adminSubs);

        if (normalSubs.length > 0) {
          kept.push({ ...item, submenu: normalSubs });
        } else if (item.route && !this.isAdministrationRoute(item.route)) {
          kept.push({ ...item, submenu: undefined });
        }
        continue;
      }

      kept.push(item);
    }

    const dedupedAdmin = adminSubmenu.filter(
      (sub, idx, arr) => arr.findIndex((x) => x.route === sub.route) === idx
    );

    if (dedupedAdmin.length > 0) {
      kept.unshift({
        id: 999001,
        icon: 'fa-solid fa-user-shield',
        label: 'Administración',
        submenu: dedupedAdmin.sort((a, b) => a.label.localeCompare(b.label))
      });
    }

    return kept;
  }

  private isAdministrationRoute(route: string): boolean {
    return (
      route.startsWith('/administracion/') ||
      route === '/formularios' ||
      route === '/usuarios' ||
      route === '/roles' ||
      route === '/linea-mando' ||
      route === '/radio' ||
      route === '/configuracion-sistema' ||
      route === '/video-unidad' ||
      route === '/configuracion-imagen-sitio' ||
      route === '/admin-multimedia'
    );
  }

  private orderAndSortMenu(items: MenuItem[]): MenuItem[] {
    if (!this.authService.isAuthenticated()) {
      return items;
    }

    // 1. Clonar y ordenar submenús de cada elemento alfabéticamente
    const processed = (items ?? []).map((item) => {
      if (item.submenu && item.submenu.length > 0) {
        const sortedSub = [...item.submenu].sort((a, b) =>
          a.label.localeCompare(b.label, 'es', { sensitivity: 'base' })
        );
        return { ...item, submenu: sortedSub };
      }
      return { ...item };
    });

    let inicioItem: MenuItem | undefined;
    let adminItem: MenuItem | undefined;
    const remainingItems: MenuItem[] = [];

    // 2. Extraer Inicio y Administración
    for (const item of processed) {
      const normLabel = item.label.toLowerCase().trim();
      const normRoute = (item.route ?? '').toLowerCase().trim();

      if (normLabel === 'inicio' || normRoute === '/home') {
        inicioItem = {
          ...item,
          icon: item.icon || 'fa-solid fa-house',
          label: 'Inicio',
          route: '/home'
        };
      } else if (normLabel === 'administración' || normLabel === 'administracion') {
        adminItem = {
          ...item,
          label: 'Administración'
        };
      } else {
        remainingItems.push(item);
      }
    }

    // 3. Ordenar alfabéticamente todos los demás módulos de primer nivel
    remainingItems.sort((a, b) => a.label.localeCompare(b.label, 'es', { sensitivity: 'base' }));

    // 4. Construir menú garantizando el orden: Inicio -> Administración -> Alfabético
    const finalMenu: MenuItem[] = [];

    if (inicioItem) {
      finalMenu.push(inicioItem);
    } else {
      finalMenu.push({
        id: 999000,
        icon: 'fa-solid fa-house',
        label: 'Inicio',
        route: '/home'
      });
    }

    if (adminItem) {
      finalMenu.push(adminItem);
    }

    finalMenu.push(...remainingItems);
    return finalMenu;
  }
  
}

