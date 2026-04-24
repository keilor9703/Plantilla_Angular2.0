import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { DtoSliders, SliderService } from '../../core/services/administracion/slider.service';
import { HomeService, HomeStats } from '../../core/services/home.service';
import { VideoUnidadService } from '../../core/services/administracion/video-unidad.service';
import { VideoInstitucionalService } from '../../core/services/administracion/video-institucional.service';
import { DtoLineaMando, LineaMandoService } from '../../core/services/administracion/linea-mando.service';
import { NoticiaService, DtoNoticia } from '../../core/services/administracion/noticia.service';
import { SafeUrlPipe } from '../../shared/pipes/safe-url.pipe';
import { environment } from '../../../environments/environment';
import { RedesSocialesService, DtoDominio, DtoRedesSociales } from '../../core/services/gestion-digital/redes-sociales.service';

type NewsTag = 'Comunicado' | 'Servicio' | 'Importante';

interface NewsItem {
  id: number;
  date: string;
  tag: NewsTag;
  title: string;
  lead: string;     
  content: string;  
  image: string;
  megusta: number;
}

interface SocialAccount {
  id: number;
  networkName: string;
  accountName: string;
  icon: string;
  url: string;
  color: string;
}

interface SocialNetworkGroup {
  networkName: string;
  icon: string;
  color: string;
  accounts: SocialAccount[];
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, SafeUrlPipe, RouterLink],
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
})
export class HomeComponent implements OnInit, OnDestroy {
  banners: DtoSliders[] = [];
  stats: HomeStats = {
    usuariosActivos: 0,
    reportesGenerados: 0,
    alertasSistema: 0
  };
  currentBannerIndex = 0;
  private bannerTimer: ReturnType<typeof setInterval> | null = null;
  videoUnidadUrl = '';
  videoInstitucionalUrl = '';
  lineaMando: DtoLineaMando[] = [];
  lineaMandoLightboxOpen = false;
  lineaMandoLightboxItem: DtoLineaMando | null = null;

  socialGroups: SocialNetworkGroup[] = [];
  socialModalOpen = false;
  selectedSocialGroup: SocialNetworkGroup | null = null;

  constructor(
    private sliderService: SliderService,
    private homeService: HomeService,
    private videoUnidadService: VideoUnidadService,
    private videoInstitucionalService: VideoInstitucionalService,
    private lineaMandoService: LineaMandoService,
    private noticiaService: NoticiaService,
    private redesSocialesService: RedesSocialesService
  ) {}

  ngOnInit(): void {
    this.loadBanners();
    this.loadStats();
    this.loadVideoUnidad();
    this.loadVideoInstitucional();
    this.loadLineaMando();
    this.loadNoticias();
    this.loadSocialNetworks();
  }

  loadNoticias(): void {
    this.noticiaService.getActivas().subscribe({
      next: (noticias) => {
        console.log('Noticias cargadas:', noticias);
        console.log('Cantidad:', noticias.length);
        // Ordenar por fecha descendente y limitar a 5 noticias más recientes
        const sorted = noticias.sort((a, b) => 
          new Date(b.fechaCreacion).getTime() - new Date(a.fechaCreacion).getTime()
        ).slice(0, 5);
        
        this.news = sorted.map(n => ({
          id: n.idNoticia,
          date: new Date(n.fechaCreacion).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' }),
          tag: this.mapSeccionToTag(n.seccion),
          title: n.titulo,
          lead: n.subtitulo || '',
          content: n.contenido || '',
          image: this.getNoticiaImageUrl(n.imagenNoticia),
          megusta: n.megusta || 0
        }));
        console.log('News mapeadas:', this.news);
      },
      error: (err) => {
        console.error('Error cargando noticias:', err);
        this.news = [];
      }
    });
  }

  private getNoticiaImageUrl(imagenNoticia: string | null | undefined): string {
    const raw = (imagenNoticia ?? '').trim();
    if (!raw) return '';
    if (raw.startsWith('http://') || raw.startsWith('https://') || raw.startsWith('/api/') || raw.startsWith('data:')) return raw;
    const fileName = raw.split('/').filter(Boolean).pop() ?? '';
    return fileName ? `/api/NoticiaUpload/Imagen/${encodeURIComponent(fileName)}` : '';
  }

  private mapSeccionToTag(seccion: string): NewsTag {
    switch (seccion.toLowerCase()) {
      case 'comunicado': return 'Comunicado';
      case 'servicio': return 'Servicio';
      case 'importante': return 'Importante';
      default: return 'Comunicado';
    }
  }

  ngOnDestroy(): void {
    this.stopBannerRotation();
    document.body.classList.remove('ui-modal-open');
  }

  private loadBanners(): void {
    this.sliderService.getPublicos().subscribe({
      next: (items) => {
        const now = new Date();
        const ordered = (items ?? [])
          .filter((x) => this.isBannerVigenteNow(x, now))
          .slice()
          .sort((a, b) => a.orden - b.orden);
        this.banners = ordered;
        this.currentBannerIndex = 0;
        this.startBannerRotation();
      },
      error: () => {
        this.banners = this.getFallbackBanners();
        this.currentBannerIndex = 0;
        this.startBannerRotation();
      }
    });
  }

  private isBannerVigenteNow(item: DtoSliders, now: Date): boolean {
    if (Number(item?.vigente ?? 0) !== 1) {
      return false;
    }

    const inicio = this.parseSliderDate(item?.fechaInicio);
    const fin = this.parseSliderDate(item?.fechaFin);

    if (inicio && inicio.getTime() > now.getTime()) {
      return false;
    }

    if (fin && fin.getTime() < now.getTime()) {
      return false;
    }

    return true;
  }

  private parseSliderDate(value?: string | null): Date | null {
    const raw = String(value ?? '').trim();
    if (!raw) {
      return null;
    }

    const parsed = new Date(raw);
    if (Number.isNaN(parsed.getTime())) {
      return null;
    }

    return parsed;
  }

  private loadStats(): void {
    this.homeService.getStats().subscribe({
      next: (data) => {
        this.stats = {
          usuariosActivos: Number(data?.usuariosActivos ?? 0),
          reportesGenerados: Number(data?.reportesGenerados ?? 0),
          alertasSistema: Number(data?.alertasSistema ?? 0)
        };
      },
      error: () => {
        this.stats = {
          usuariosActivos: 0,
          reportesGenerados: 0,
          alertasSistema: 0
        };
      }
    });
  }

  private loadVideoUnidad(): void {
    this.videoUnidadService.getCurrent().subscribe({
      next: (data) => {
        this.videoUnidadUrl = data?.hasVideo ? data.url : '';
      },
      error: () => {
        this.videoUnidadUrl = '';
      }
    });
  }

  private loadVideoInstitucional(): void {
    this.videoInstitucionalService.getCurrent().subscribe({
      next: (data) => {
        this.videoInstitucionalUrl = data?.hasVideo && data.data?.embedUrl ? data.data.embedUrl : '';
      },
      error: () => {
        this.videoInstitucionalUrl = '';
      }
    });
  }

  private loadLineaMando(): void {
    this.lineaMandoService.getAll().subscribe({
      next: (items) => {
        this.lineaMando = (items ?? [])
          .filter((x) => Number(x?.vigente ?? 1) === 1)
          .sort((a, b) => Number(a?.orden ?? 0) - Number(b?.orden ?? 0));
      },
      error: () => {
        this.lineaMando = [];
      }
    });
  }

  getLineaMandoFotoUrl(item: DtoLineaMando): string {
    const fotoBase64 = item?.fotoBase64?.trim();
    if (!fotoBase64) {
      return 'imagenes/policia.jpg';
    }

    if (fotoBase64.startsWith('data:')) {
      return fotoBase64;
    }

    return `data:image/jpeg;base64,${fotoBase64}`;
  }

  getLineaMandoNombre(item: DtoLineaMando): string {
    return `${item?.nombre ?? ''} ${item?.apellidos ?? ''}`.trim();
  }

  getLineaMandoCargoDisplay(item: DtoLineaMando): string {
    const rawCargo = (item?.cargo ?? '').trim();
    return rawCargo ? this.toTitleCase(rawCargo) : 'Sin cargo';
  }

  private toTitleCase(value: string): string {
    return value
      .toLowerCase()
      .split(/\s+/)
      .filter(Boolean)
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  openLineaMandoLightbox(item: DtoLineaMando): void {
    this.lineaMandoLightboxItem = item;
    this.lineaMandoLightboxOpen = true;
    this.syncBodyModalClass();
  }

  closeLineaMandoLightbox(): void {
    this.lineaMandoLightboxOpen = false;
    this.lineaMandoLightboxItem = null;
    this.syncBodyModalClass();
  }

  private syncBodyModalClass(): void {
    if (this.newsModalOpen || this.lineaMandoLightboxOpen || this.socialModalOpen) {
      document.body.classList.add('ui-modal-open');
      return;
    }

    document.body.classList.remove('ui-modal-open');
  }

  goToBanner(index: number): void {
    if (index < 0 || index >= this.banners.length) {
      return;
    }
    this.currentBannerIndex = index;
  }

  openBannerLink(item: DtoSliders): void {
    const url = (item.urlDestino ?? '').trim();
    if (!url) {
      return;
    }

    if (url.startsWith('http://') || url.startsWith('https://')) {
      window.open(url, '_blank', 'noopener,noreferrer');
      return;
    }

    window.location.href = url.startsWith('/') ? url : `/${url}`;
  }

  getBannerSubtitle(item: DtoSliders): string {
    const subtitle = (item.subtitulo ?? '').trim();
    if (!subtitle) {
      return 'Comunicado institucional';
    }

    // Si por mapeo del backend llega una URL en subtitulo, no la mostramos en pantalla.
    const destino = (item.urlDestino ?? '').trim();
    if (this.looksLikeUrl(subtitle) || (destino && subtitle.toLowerCase() === destino.toLowerCase())) {
      return 'Comunicado institucional';
    }

    return subtitle;
  }

  getBannerTitle(item: DtoSliders): string {
    const title = (item.titulo ?? '').trim();
    if (!title || this.looksLikeUrl(title)) {
      return 'SISGE';
    }
    return title;
  }

  getBannerImageUrl(item: DtoSliders): string {
    const raw = (item.urlImagen ?? '').trim();
    if (!raw) {
      return '';
    }

    if (raw.startsWith('data:')) {
      return raw;
    }

    // Si viene URL absoluta/local con /uploads/sliders, resolvemos por API para evitar problemas de host/puerto/proxy.
    const uploadPathIndex = raw.toLowerCase().indexOf('/uploads/sliders/');
    if (uploadPathIndex >= 0) {
      const fileName = raw.substring(uploadPathIndex).split('/').filter(Boolean).pop() ?? '';
      return fileName ? `${this.getApiBaseUrl()}/Slider/Image/${encodeURIComponent(fileName)}` : '';
    }

    const normalized = raw.replace(/\\/g, '/');
    if (normalized.toLowerCase().startsWith('uploads/sliders/')) {
      const fileName = normalized.split('/').filter(Boolean).pop() ?? '';
      return fileName ? `${this.getApiBaseUrl()}/Slider/Image/${encodeURIComponent(fileName)}` : '';
    }
    if (raw.startsWith('http://') || raw.startsWith('https://')) {
      return raw;
    }
    if (raw.startsWith('/')) {
      return `${this.getApiOrigin()}${raw}`;
    }

    // Si viene solo el nombre de archivo, apuntamos a la carpeta pública de sliders.
    if (/^[^/]+\.(jpg|jpeg|png|webp)$/i.test(normalized)) {
     return `${this.getApiBaseUrl()}/Slider/Image/${encodeURIComponent(normalized)}`;
    }

    return `/${raw}`;
  }

  private getApiBaseUrl(): string {
    const base = (environment.apiBaseUrl ?? '/api').trim().replace(/\/+$/, '');
    return base || '/api';
  }

  private getApiOrigin(): string {
    const base = this.getApiBaseUrl();
    if (!/^https?:\/\//i.test(base)) {
      return '';
    }
    try {
      const url = new URL(base);
      return `${url.protocol}//${url.host}`;
    } catch {
      return '';
    }
  }

  private looksLikeUrl(value: string): boolean {
    const v = value.trim().toLowerCase();
    return v.startsWith('http://')
      || v.startsWith('https://')
      || v.startsWith('www.')
      || v.startsWith('/')
      || v.startsWith('/api/')
      || v.startsWith('/uploads/')
      || v.startsWith('api/')
      || v.startsWith('uploads/')
      || /^[^/]+\.(jpg|jpeg|png|webp|gif)$/i.test(value.trim());
  }

  private startBannerRotation(): void {
    this.stopBannerRotation();
    if (this.banners.length <= 1) {
      return;
    }

    this.bannerTimer = setInterval(() => {
      this.currentBannerIndex = (this.currentBannerIndex + 1) % this.banners.length;
    }, 7000);
  }

  private stopBannerRotation(): void {
    if (this.bannerTimer) {
      clearInterval(this.bannerTimer);
      this.bannerTimer = null;
    }
  }

  private getFallbackBanners(): DtoSliders[] {
    return [
      { idSlider: 1, titulo: 'SISGE', subtitulo: 'Banner informativo', urlImagen: 'banner/banner1.jpg', urlDestino: '', orden: 1, vigente: 1 },
      { idSlider: 2, titulo: 'SISGE', subtitulo: 'Banner informativo', urlImagen: 'banner/banner2.jpg', urlDestino: '', orden: 2, vigente: 1 },
      { idSlider: 3, titulo: 'SISGE', subtitulo: 'Banner informativo', urlImagen: 'banner/banner3.jpg', urlDestino: '', orden: 3, vigente: 1 }
    ];
  }

  news: NewsItem[] = [];

  newsModalOpen = false;
  selectedNews: NewsItem | null = null;

  openNews(item: NewsItem): void {
    this.selectedNews = item;
    this.newsModalOpen = true;
    this.syncBodyModalClass();
  }

  closeNews(): void {
    this.newsModalOpen = false;
    this.selectedNews = null;
    this.syncBodyModalClass();
  }

  likeNoticia(item: NewsItem, event: Event): void {
    event.stopPropagation();
    
    const likedNews = this.getLikedNews();
    if (likedNews.includes(item.id)) {
      return;
    }
    
    this.noticiaService.darLike(item.id).subscribe({
      next: () => {
        item.megusta = (item.megusta || 0) + 1;
        this.saveLikedNews(item.id);
      },
      error: (err) => {
        console.error('Error dando like:', err);
      }
    });
  }

  hasLiked(noticiaId: number): boolean {
    const likedNews = this.getLikedNews();
    return likedNews.includes(noticiaId);
  }

  private getLikedNews(): number[] {
    const stored = sessionStorage.getItem('likedNews');
    return stored ? JSON.parse(stored) : [];
  }

  private saveLikedNews(noticiaId: number): void {
    const liked = this.getLikedNews();
    if (!liked.includes(noticiaId)) {
      liked.push(noticiaId);
      sessionStorage.setItem('likedNews', JSON.stringify(liked));
    }
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

  openSocialGroup(group: SocialNetworkGroup): void {
    this.selectedSocialGroup = group;
    this.socialModalOpen = true;
    this.syncBodyModalClass();
  }

  closeSocialModal(): void {
    this.socialModalOpen = false;
    this.selectedSocialGroup = null;
    this.syncBodyModalClass();
  }

  private groupSocialAccounts(redes: DtoRedesSociales[], dominios: DtoDominio[]): SocialNetworkGroup[] {
    const map = new Map<string, SocialAccount[]>();

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

  private getFallbackSocialGroups(): SocialNetworkGroup[] {
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
    if (lower.includes('linkedin')) return 'fa-linkedin-in';
    if (lower.includes('tiktok')) return 'fa-tiktok';
    return 'fa-share-nodes'; // default
  }

  private getSocialColor(nombreRed: string): string {
    const lower = nombreRed.toLowerCase();
    if (lower.includes('facebook')) return '#1877F2';
    if (lower.includes('twitter') || lower.includes('x')) return '#000000';
    if (lower.includes('instagram')) return '#E4405F';
    if (lower.includes('youtube')) return '#FF0000';
    if (lower.includes('linkedin')) return '#0077B5';
    if (lower.includes('tiktok')) return '#000000';
    return '#666'; // default
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.socialModalOpen) {
      this.closeSocialModal();
      return;
    }

    if (this.lineaMandoLightboxOpen) {
      this.closeLineaMandoLightbox();
      return;
    }

    if (this.newsModalOpen) {
      this.closeNews();
    }
  }
}


