import { Injectable, signal } from '@angular/core';
import { BrandingService } from './administracion/branding.service';

export type MenuBrandLayout =
  | 'stacked-name-first'
  | 'stacked-acronym-first'
  | 'inline-name-first'
  | 'inline-acronym-first';

export type MenuLogoPosition = 'left' | 'right';
export type BrandTextLayout = 'stacked' | 'inline';
export type BrandTextAlign = 'left' | 'center' | 'right';
export type BrandTextOrder =
  | 'institution-system-acronym'
  | 'institution-acronym-system'
  | 'system-institution-acronym'
  | 'system-acronym-institution'
  | 'acronym-institution-system'
  | 'acronym-system-institution';

export interface BrandIdentity {
  readonly institutionName: string;
  readonly systemName: string;
  readonly shortName: string;
  readonly logoUrl: string;
  readonly menuShowLogo: boolean;
  readonly menuShowInstitutionName: boolean;
  readonly menuShowSystemName: boolean;
  readonly menuTextSizePx: number;
  readonly menuShowAcronym: boolean;
  readonly menuBrandLayout: MenuBrandLayout;
  readonly menuLogoPosition: MenuLogoPosition;
  readonly menuTextLayout: BrandTextLayout;
  readonly menuBrandTextOrder: BrandTextOrder;
  readonly menuTextAlign: BrandTextAlign;
  readonly menuLogoSizePx: number;
  readonly menuInstitutionTextSizePx: number;
  readonly menuSystemTextSizePx: number;
  readonly menuAcronymTextSizePx: number;
}

const DEFAULT_IDENTITY: BrandIdentity = {
  institutionName: 'Policía Nacional de Colombia',
  systemName: 'OFTIC',
  shortName: 'OFTIC',
  logoUrl: '/imagenes/oftic-logo-app.png',
  menuShowLogo: true,
  menuShowInstitutionName: true,
  menuShowSystemName: true,
  menuTextSizePx: 14,
  menuShowAcronym: true,
  menuBrandLayout: 'stacked-name-first',
  menuLogoPosition: 'left',
  menuTextLayout: 'stacked',
  menuBrandTextOrder: 'institution-system-acronym',
  menuTextAlign: 'left',
  menuLogoSizePx: 48,
  menuInstitutionTextSizePx: 9,
  menuSystemTextSizePx: 14,
  menuAcronymTextSizePx: 10,
};

@Injectable({ providedIn: 'root' })
export class BrandIdentityService {
  private readonly identityState = signal<BrandIdentity>(DEFAULT_IDENTITY);
  private loaded = false;

  readonly identity = this.identityState.asReadonly();

  constructor(private readonly brandingService: BrandingService) {}

  load(forceRefresh = false): void {
    if (this.loaded && !forceRefresh) {
      return;
    }

    this.loaded = true;
    this.brandingService.getPublicConfig().subscribe({
      next: (config: any) => {
        const systemName = this.firstText(
          config?.nombreSistema,
          config?.systemName,
          config?.sistema,
        );
        const shortName = this.firstText(config?.sigla, config?.sistema, config?.systemName);
        const logoUrl = (config?.logoUrl ?? '').trim();

        this.identityState.set({
          institutionName:
            this.firstText(config?.nombreInstitucion, config?.nombreUnidad) || DEFAULT_IDENTITY.institutionName,
          systemName: systemName || DEFAULT_IDENTITY.systemName,
          shortName: shortName || DEFAULT_IDENTITY.shortName,
          logoUrl: logoUrl || DEFAULT_IDENTITY.logoUrl,
          menuShowLogo: config?.menuShowLogo ?? DEFAULT_IDENTITY.menuShowLogo,
          menuShowInstitutionName:
            config?.menuShowInstitutionName ?? DEFAULT_IDENTITY.menuShowInstitutionName,
          menuShowSystemName:
            config?.menuShowSystemName ?? DEFAULT_IDENTITY.menuShowSystemName,
          menuTextSizePx: config?.menuTextSizePx ?? DEFAULT_IDENTITY.menuTextSizePx,
          menuShowAcronym:
            config?.menuShowAcronym ?? DEFAULT_IDENTITY.menuShowAcronym,
          menuBrandLayout:
            config?.menuBrandLayout ?? DEFAULT_IDENTITY.menuBrandLayout,
          menuLogoPosition:
            config?.menuLogoPosition ?? DEFAULT_IDENTITY.menuLogoPosition,
          menuTextLayout: config?.menuTextLayout ?? DEFAULT_IDENTITY.menuTextLayout,
          menuBrandTextOrder:
            config?.menuBrandTextOrder ?? DEFAULT_IDENTITY.menuBrandTextOrder,
          menuTextAlign: config?.menuTextAlign ?? DEFAULT_IDENTITY.menuTextAlign,
          menuLogoSizePx: config?.menuLogoSizePx ?? DEFAULT_IDENTITY.menuLogoSizePx,
          menuInstitutionTextSizePx:
            config?.menuInstitutionTextSizePx ?? DEFAULT_IDENTITY.menuInstitutionTextSizePx,
          menuSystemTextSizePx:
            config?.menuSystemTextSizePx ?? DEFAULT_IDENTITY.menuSystemTextSizePx,
          menuAcronymTextSizePx:
            config?.menuAcronymTextSizePx ?? DEFAULT_IDENTITY.menuAcronymTextSizePx,
        });
      },
      error: () => {
        if (!forceRefresh) {
          this.identityState.set(DEFAULT_IDENTITY);
        }
      },
    });
  }

  refresh(): void {
    this.load(true);
  }

  private firstText(...values: Array<string | null | undefined>): string {
    return values.map((value) => (value ?? '').trim()).find(Boolean) ?? '';
  }
}
