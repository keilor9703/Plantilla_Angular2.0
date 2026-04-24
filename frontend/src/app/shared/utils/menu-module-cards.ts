import { DbMenuItem } from '../../core/services/administracion/menu.service';

export interface ModuleCard {
  id: number;
  title: string;
  description: string;
  route: string;
  icon: string;
  position: number;
}

interface BuildModuleCardsOptions {
  prefix: '/administracion' | '/gestion-digital';
  excludeRoutes?: string[];
}

export function buildModuleCards(items: DbMenuItem[], options: BuildModuleCardsOptions): ModuleCard[] {
  const exclude = new Set(options.excludeRoutes ?? []);
  const seen = new Set<string>();

  return (items ?? [])
    .filter((item) => Number(item.vigente ?? 1) === 1)
    .map((item) => {
      const route = normalizeRoute(item.detalle);
      return {
        item,
        route,
      };
    })
    .filter((x) => !!x.route && x.route.startsWith(options.prefix))
    .filter((x) => !exclude.has(x.route))
    .filter((x) => {
      if (seen.has(x.route)) {
        return false;
      }
      seen.add(x.route);
      return true;
    })
    .sort((a, b) => a.item.posicion - b.item.posicion || a.item.descripcion.localeCompare(b.item.descripcion))
    .map((x) => ({
      id: x.item.idMenu,
      title: x.item.descripcion,
      description: buildDescription(x.item.descripcion),
      route: x.route,
      icon: normalizeIcon(x.item.icono),
      position: x.item.posicion,
    }));
}

function normalizeRoute(raw: string | null | undefined): string {
  const value = String(raw ?? '').trim();
  if (!value) {
    return '';
  }

  if (value.startsWith('http://') || value.startsWith('https://') || value.startsWith('www.')) {
    if (value.startsWith('www.') && !value.startsWith('http')) {
      return `https://${value}`;
    }
    return value;
  }

  if (value.startsWith('/administracion/') || value.startsWith('/gestion-digital/')) {
    if (value === '/administracion/redes-sociales') {
      return '/gestion-digital/redes-sociales';
    }
    if (value === '/administracion/gestion-digital') {
      return '/gestion-digital/inicio';
    }
    return value;
  }

  if (value === '/gestion-digital') {
    return '/gestion-digital/inicio';
  }

  const configRoutes = [
    '/video-unidad',
    '/configuracion-imagen-sitio',
    '/admin-multimedia',
    '/configuracion-sistema',
  ];
  if (configRoutes.includes(value)) {
    return '/administracion/configuracion-sistema';
  }

  const routeMap: Record<string, string> = {
    '/formularios': '/administracion/formularios',
    '/usuarios': '/administracion/usuarios',
    '/roles': '/administracion/roles',
    '/linea-mando': '/administracion/linea-mando',
    '/radio': '/administracion/radio',
    '/menu': '/administracion/menu',
    '/auditoria': '/administracion/auditoria',
    '/sliders': '/administracion/sliders',
    '/redes-sociales': '/gestion-digital/redes-sociales',
  };

  return routeMap[value] || (value.startsWith('/') ? value : `/${value}`);
}

function normalizeIcon(raw: string | null | undefined): string {
  const value = String(raw ?? '').trim();
  if (!value) {
    return 'fa-solid fa-folder';
  }

  return value.includes('fa-') ? value : `fa-solid ${value}`;
}

function buildDescription(title: string): string {
  const clean = (title ?? '').trim();
  if (!clean) {
    return 'Módulo del sistema.';
  }
  return `Gestión del módulo ${clean}.`;
}
