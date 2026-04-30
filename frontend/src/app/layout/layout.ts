import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { SidebarComponent } from '../components/sidebar/sidebar';
import { HeaderComponent } from '../components/header/header';
import { FooterComponent } from '../components/footer/footer';
import { BreadcrumbComponent } from '../components/breadcrumb/breadcrumb';
import { AccessibilityMenuComponent } from '../components/accessibility-menu/accessibility-menu';
import { SocialDockGroup, SocialDockAccount } from '../components/social-dock/social-dock';
import { SidebarService } from '../services/sidebar';
import { DtoDominio, DtoRedesSociales, RedesSocialesService } from '../core/services/gestion-digital/redes-sociales.service';

@Component({
  selector: 'app-layout',
  templateUrl: './layout.html',
  styleUrls: ['./layout.scss'],
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, HeaderComponent, FooterComponent, BreadcrumbComponent, AccessibilityMenuComponent],
})
export class LayoutComponent {
  socialGroups: SocialDockGroup[] = [];

  constructor(
    private sidebarService: SidebarService,
    private redesSocialesService: RedesSocialesService
  ) {
    this.loadSocialNetworks();
  }

  isMenuOpen() {
    return this.sidebarService.isOpen();
  }

  closeMenu() {
    this.sidebarService.closeSidebar();
  }

  private loadSocialNetworks(): void {
    forkJoin({
      redes: this.redesSocialesService.getAll(),
      dominios: this.redesSocialesService.getDominios()
    }).subscribe({
      next: ({ redes, dominios }) => {
        this.socialGroups = this.groupSocialAccounts(redes ?? [], dominios ?? []);
      },
      error: () => {
        this.socialGroups = this.getFallbackSocialGroups();
      }
    });
  }

  private groupSocialAccounts(redes: DtoRedesSociales[], dominios: DtoDominio[]): SocialDockGroup[] {
    const map = new Map<string, SocialDockAccount[]>();

    for (const item of redes) {
      if (Number(item?.vigente ?? 1) !== 1) {
        continue;
      }

      const networkName = this.resolveSocialNetworkName(item.redSocial, dominios);
      const accountName = (item?.nombreCuenta ?? '').trim();
      const url = (item?.urlCuenta ?? '').trim();

      if (!networkName || !accountName || !url) {
        continue;
      }

      const normalizedKey = networkName.toLowerCase();
      const current = map.get(normalizedKey) ?? [];
      current.push({
        id: Number(item?.id ?? 0),
        networkName,
        accountName,
        url,
        icon: this.getSocialIcon(networkName),
        color: this.getSocialColor(networkName)
      });
      map.set(normalizedKey, current);
    }

    return Array.from(map.values())
      .map((accounts) => {
        const first = accounts[0];
        return {
          networkName: first.networkName,
          icon: first.icon,
          color: first.color,
          accounts: accounts.sort((a, b) => a.accountName.localeCompare(b.accountName, 'es', { sensitivity: 'base' }))
        };
      })
      .sort((a, b) => a.networkName.localeCompare(b.networkName, 'es', { sensitivity: 'base' }));
  }

  private resolveSocialNetworkName(value: string, dominios: DtoDominio[]): string {
    const raw = (value ?? '').trim();
    if (!raw) {
      return '';
    }

    const id = Number(raw);
    if (Number.isNaN(id)) {
      return raw;
    }

    const dominio = dominios.find((d) => (d.id ?? d.idDominio) === id);
    return (dominio?.descripcion ?? raw).trim();
  }

  private getFallbackSocialGroups(): SocialDockGroup[] {
    return [
      {
        networkName: 'Facebook',
        icon: 'fa-facebook-f',
        color: '#1877F2',
        accounts: [{
          id: 1,
          networkName: 'Facebook',
          accountName: 'Policia Colombia',
          icon: 'fa-facebook-f',
          url: 'https://www.facebook.com/PoliciaColombia',
          color: '#1877F2'
        }]
      },
      {
        networkName: 'X',
        icon: 'fa-x-twitter',
        color: '#000000',
        accounts: [{
          id: 2,
          networkName: 'X',
          accountName: 'Policia Colombia',
          icon: 'fa-x-twitter',
          url: 'https://twitter.com/PoliciaColombia',
          color: '#000000'
        }]
      },
      {
        networkName: 'Instagram',
        icon: 'fa-instagram',
        color: '#E4405F',
        accounts: [{
          id: 3,
          networkName: 'Instagram',
          accountName: 'Policia Colombia',
          icon: 'fa-instagram',
          url: 'https://www.instagram.com/policiacolombia',
          color: '#E4405F'
        }]
      },
      {
        networkName: 'YouTube',
        icon: 'fa-youtube',
        color: '#FF0000',
        accounts: [{
          id: 4,
          networkName: 'YouTube',
          accountName: 'Policia Nacional Colombia',
          icon: 'fa-youtube',
          url: 'https://www.youtube.com/@PoliciaNacionalCol',
          color: '#FF0000'
        }]
      }
    ];
  }

  private getSocialIcon(nombreRed: string): string {
    const lower = nombreRed.toLowerCase();
    if (lower.includes('facebook')) return 'fa-facebook-f';
    if (lower.includes('twitter') || lower.includes('x')) return 'fa-x-twitter';
    if (lower.includes('instagram')) return 'fa-instagram';
    if (lower.includes('youtube')) return 'fa-youtube';
    if (lower.includes('linkedin') || lower.includes('linkdn')) return 'fa-linkedin-in';
    if (lower.includes('tiktok')) return 'fa-tiktok';
    if (lower.includes('whatsapp')) return 'fa-whatsapp';
    if (lower.includes('telegram')) return 'fa-telegram';
    if (lower.includes('threads')) return 'fa-threads';
    if (lower.includes('snapchat')) return 'fa-snapchat';
    if (lower.includes('pinterest')) return 'fa-pinterest-p';
    if (lower.includes('flickr')) return 'fa-flickr';
    if (lower.includes('spotify')) return 'fa-spotify';
    if (lower.includes('discord')) return 'fa-discord';
    if (lower.includes('reddit')) return 'fa-reddit-alien';
    if (lower.includes('vimeo')) return 'fa-vimeo-v';
    return 'fa-share-nodes';
  }

  private getSocialColor(nombreRed: string): string {
    const lower = nombreRed.toLowerCase();
    if (lower.includes('facebook')) return '#1877F2';
    if (lower.includes('twitter') || lower.includes('x')) return '#000000';
    if (lower.includes('instagram')) return '#E4405F';
    if (lower.includes('youtube')) return '#FF0000';
    if (lower.includes('linkedin') || lower.includes('linkdn')) return '#0077B5';
    if (lower.includes('tiktok')) return '#000000';
    if (lower.includes('whatsapp')) return '#25D366';
    if (lower.includes('telegram')) return '#229ED9';
    if (lower.includes('threads')) return '#111111';
    if (lower.includes('snapchat')) return '#FFFC00';
    if (lower.includes('pinterest')) return '#BD081C';
    if (lower.includes('flickr')) return '#FF0084';
    if (lower.includes('spotify')) return '#1DB954';
    if (lower.includes('discord')) return '#5865F2';
    if (lower.includes('reddit')) return '#FF4500';
    if (lower.includes('vimeo')) return '#1AB7EA';
    return '#08a6cb';
  }
}
