export interface AppRouteCatalogItem {
  readonly route: string;
  readonly label: string;
  readonly area: string;
}

export interface AppInternalCapabilityCatalogItem {
  readonly code: string;
  readonly route: string;
  readonly label: string;
  readonly area: string;
  readonly allowedRoleIds: readonly number[];
}

export const APP_ROUTE_CATALOG: readonly AppRouteCatalogItem[] = [
  { route: '/home', label: 'Inicio', area: 'General' },
  { route: '/administracion', label: 'Inicio de administración', area: 'Administración' },
  { route: '/administracion/formularios', label: 'Guía de formularios', area: 'Administración' },
  { route: '/administracion/usuarios', label: 'Usuarios', area: 'Administración' },
  { route: '/administracion/roles', label: 'Roles', area: 'Administración' },
  { route: '/administracion/auditoria', label: 'Auditoría del sistema', area: 'Administración' },
  { route: '/administracion/menu', label: 'Administración de menú', area: 'Administración' },
  { route: '/administracion/configuracion-sistema', label: 'Configuración del sistema', area: 'Administración' },
  { route: '/administracion/linea-mando', label: 'Línea de mando', area: 'Administración' },
  { route: '/administracion/dominio', label: 'Dominios', area: 'Administración' },
] as const;

export const APP_INTERNAL_CAPABILITY_CATALOG: readonly AppInternalCapabilityCatalogItem[] = [] as const;

const ROUTE_ALIASES: Readonly<Record<string, string>> = {
  '/inicio': '/home',
  '/formularios': '/administracion/formularios',
  '/usuarios': '/administracion/usuarios',
  '/roles': '/administracion/roles',
  '/auditoria': '/administracion/auditoria',
  '/menu': '/administracion/menu',
  '/configuracion-sistema': '/administracion/configuracion-sistema',
  '/admin-multimedia': '/administracion/configuracion-sistema',
  '/video-unidad': '/administracion/configuracion-sistema',
  '/configuracion-imagen-sitio': '/administracion/configuracion-sistema',
  '/linea-mando': '/administracion/linea-mando',
  '/dominio': '/administracion/dominio',
};

const KNOWN_INTERNAL_ROUTES = new Set(APP_ROUTE_CATALOG.map((item) => item.route));

export function normalizeAppRoute(rawRoute: unknown): string {
  const value = String(rawRoute ?? '').trim();
  if (!value) {
    return '';
  }

  const withLeadingSlash = value.startsWith('/') ? value : `/${value}`;
  const withoutTrailingSlash = withLeadingSlash.replace(/\/+$/, '') || '/';
  return ROUTE_ALIASES[withoutTrailingSlash] ?? withoutTrailingSlash;
}

export function isKnownAppRoute(rawRoute: unknown): boolean {
  const route = normalizeAppRoute(rawRoute);
  if (KNOWN_INTERNAL_ROUTES.has(route)) return true;
  if (route.startsWith('/administracion/') || route.startsWith('/home')) return true;
  return route.length > 1 && route.startsWith('/');
}
