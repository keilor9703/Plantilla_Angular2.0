import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map, startWith } from 'rxjs';

import { BreadcrumbItem } from '../../core/interfaces/breadcrumb-item.interface';
import { BrandIdentityService } from '../../core/services/brand-identity.service';
import { SidebarService } from '../../services/sidebar';

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './breadcrumb.html',
  styleUrl: './breadcrumb.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BreadcrumbComponent {
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly brandIdentity = inject(BrandIdentityService);

  readonly sidebarOpen = inject(SidebarService).isOpen;
  readonly identity = this.brandIdentity.identity;

  private readonly routeLabels: Record<string, string> = {
    '/home': 'Inicio',
    '/noticias': 'Noticias',
    '/gestion-digital': 'Gestión Digital',
    '/gestion-digital/inicio': 'Inicio Gestión Digital',
    '/gestion-digital/redes-sociales': 'Redes Sociales',
    '/grupo-prensa': 'Grupo Prensa',
    '/grupo-prensa/crear-boletines-prensa': 'Crear Boletines de Prensa',
    '/grupo-prensa/boletines-prensa': 'Crear Boletines de Prensa',
    '/grupo-prensa/publicacion-boletines': 'Aprobación de Publicación',
    '/eventos-institucionales': 'Eventos Institucionales',
    '/creativios': 'Creativos',
    '/creativios/emprendimientos': 'Emprendimientos',
    '/gestion-documental': 'Gestión Documental',
    '/gestion-documental/gestion-correos-electronicos': 'Correos Electrónicos',
    '/centro-analisis': 'Centro de Análisis',
    '/centro-analisis/inicio': 'Inicio Centro Análisis',
    '/centro-analisis/recoleccion': 'Recolección',
    '/centro-analisis/reportes': 'Reportes',
    '/administracion': 'Administración',
    '/administracion/inicio': 'Inicio Administración',
    '/administracion/usuarios': 'Usuarios',
    '/administracion/roles': 'Roles',
    '/administracion/formularios': 'Formularios',
    '/administracion/sliders': 'Sliders',
    '/administracion/personaje-mes': 'Personaje del Mes',
    '/administracion/radio': 'Emisora Radio',
    '/administracion/dominio': 'Dominios',
    '/administracion/menu': 'Menú',
    '/administracion/configuracion-sistema': 'Configuración Sistema',
    '/administracion/admin-multimedia': 'Multimedia',
    '/administracion/video-unidad': 'Video Unidad',
    '/administracion/configuracion-imagen-sitio': 'Imágenes del Sitio',
    '/administracion/linea-mando': 'Línea de Mando',
    '/administracion/video-institucional': 'Video Institucional',
    '/administracion/modal': 'Modales de Inicio',
    '/administracion/auditoria': 'Auditoría',
    '/administracion/cuentas-email': 'Cuentas de Correo',
  };

  private readonly navigationUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly items = computed<readonly BreadcrumbItem[]>(() => {
    this.navigationUrl();
    return this.buildBreadcrumb();
  });

  constructor() {
    this.brandIdentity.load();
  }

  onLogoError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img && !img.src.includes('oftic-logo-app.png')) {
      img.src = '/imagenes/oftic-logo-app.png';
    }
  }

  private buildBreadcrumb(): readonly BreadcrumbItem[] {
    const breadcrumbs: BreadcrumbItem[] = [{ label: 'Inicio', route: '/home' }];
    this.collectBreadcrumbs(this.activatedRoute.root, '', breadcrumbs);

    const normalized = this.removeDuplicateHome(breadcrumbs);
    const lastIndex = normalized.length - 1;

    return normalized.map((item, index) =>
      index === lastIndex ? { ...item, route: undefined } : item,
    );
  }

  private collectBreadcrumbs(
    route: ActivatedRoute | null | undefined,
    parentUrl: string,
    breadcrumbs: BreadcrumbItem[],
  ): void {
    if (!route) {
      return;
    }

    for (const child of route.children ?? []) {
      const snapshot = child.snapshot;
      const routeUrl = (snapshot.url ?? []).map((segment) => segment.path).join('/');
      const nextUrl = routeUrl ? `${parentUrl}/${routeUrl}` : parentUrl;

      let label = snapshot.routeConfig?.data?.['breadcrumb'] as string | undefined;
      if (!label && routeUrl) {
        label = this.routeLabels[nextUrl] || this.formatLabel(routeUrl);
      }

      if (label) {
        breadcrumbs.push({ label, route: nextUrl || undefined });
      }

      this.collectBreadcrumbs(child, nextUrl, breadcrumbs);
    }
  }

  private removeDuplicateHome(items: BreadcrumbItem[]): BreadcrumbItem[] {
    return items.filter((item, index) => item.label !== 'Inicio' || index === 0);
  }

  private formatLabel(segment: string): string {
    return segment
      .replace(/-/g, ' ')
      .replace(/\b\w/g, (c) => c.toUpperCase());
  }
}
