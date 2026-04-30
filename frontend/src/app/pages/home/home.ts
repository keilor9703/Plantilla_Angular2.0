import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DtoSliders, SliderService } from '../../core/services/administracion/slider.service';
import { HomeService, HomeStats } from '../../core/services/home.service';
import { VideoUnidadService } from '../../core/services/administracion/video-unidad.service';
import { VideoInstitucionalService } from '../../core/services/administracion/video-institucional.service';
import { DtoLineaMando, LineaMandoService } from '../../core/services/administracion/linea-mando.service';
import { NoticiaService, DtoNoticia } from '../../core/services/administracion/noticia.service';
import { SafeUrlPipe } from '../../shared/pipes/safe-url.pipe';
import { environment } from '../../../environments/environment';
import { DtoDominio } from '../../core/services/gestion-digital/redes-sociales.service';
import { DtoPersonajeMes, PersonajeMesService, sortPersonajesMes } from '../../core/services/administracion/personaje-mes.service';
import { DominioService } from '../../core/services/administracion/dominio.service';
import { DtoEvento, EventoService } from '../../core/services/administracion/evento.service';

type NewsTag = 'Comunicado' | 'Servicio' | 'Importante';

interface NewsItem {
  id: number;
  date: string;
  tag: NewsTag;
  title: string;
  lead: string;     
  content: string;  
  image: string;
  images: string[];
  megusta: number;
}

interface CalendarDay {
  date: Date;
  inCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  eventos: DtoEvento[];
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, SafeUrlPipe, RouterLink],
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
})
export class HomeComponent implements OnInit, OnDestroy {
    // Slider de grupos de alto rendimiento
    grupoSliderIndex: number = 0;
  readonly INTEGRANTE_HEIGHT = 24; // px, debe coincidir con el CSS

  // --- Pausa de sliders por hover ---
  private sliderPaused: { [categoria: string]: boolean } = {};
  private grupoSliderPaused = false;

  pauseSliderCategoria(categoria: string): void {
    this.sliderPaused[categoria] = true;
    if (this.sliderTimers[categoria]) {
      clearInterval(this.sliderTimers[categoria]);
      this.sliderTimers[categoria] = null;
    }
  }

  resumeSliderCategoria(categoria: string): void {
    if (this.sliderPaused[categoria]) {
      this.sliderPaused[categoria] = false;
      // Reiniciar solo si hay más de 1 item
      const grupo = this.personajesMesPorCategoria.find(g => g.categoria === categoria);
      if (grupo && grupo.items.length > 1) {
        this.iniciarSliderCategoria(categoria, grupo.items.length);
      }
    }
  }

  pauseGrupoSlider(): void {
    this.grupoSliderPaused = true;
    if (this.grupoScrollTimer) {
      clearInterval(this.grupoScrollTimer);
      this.grupoScrollTimer = null;
    }
  }

  resumeGrupoSlider(): void {
    if (this.grupoSliderPaused) {
      this.grupoSliderPaused = false;
      this.startGrupoScroll();
    }
  }

  // Devuelve el estilo de transformación para el scroll visual tipo créditos
  getGrupoIntegrantesTransform(grupo: any): any {
    if (!grupo.integrantes || grupo.integrantes.length <= 3) {
      return { transform: 'translateY(0)' };
    }
    // El scrollIndex es global, pero el máximo depende del grupo
    const max = grupo.integrantes.length - 3;
    let idx = this.grupoScrollIndex;
    if (idx > max) idx = 0;
    return {
      transform: `translateY(-${idx * this.INTEGRANTE_HEIGHT}px)`,
      transition: 'transform 0.6s cubic-bezier(0.4,0,0.2,1)'
    };
  }

  // Scroll automático para integrantes de grupo
  grupoScrollIndex: number = 0;
  grupoScrollTimer: any = null;

  grupoLightboxOpen = false;
  grupoLightboxItem: any = null;

  personajeMesLightboxOpen = false;
  personajeMesLightboxItem: DtoPersonajeMes | null = null;
  categoriasPersonajeMes: DtoDominio[] = [];

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
  personajesMes: DtoPersonajeMes[] = [];
  equiposAltoRendimiento: any[] = [];

  // --- Agrupación y slider de personajes del mes por categoría ---
  personajesMesPorCategoria: { categoria: string, items: DtoPersonajeMes[] }[] = [];
  sliderIndices: { [categoria: string]: number } = {};
  sliderTimers: { [categoria: string]: any } = {};

  agruparPersonajesMesPorCategoria(): void {
    const grupos: { [categoria: string]: DtoPersonajeMes[] } = {};
    for (const item of this.personajesMes) {
      const cat = this.getPersonajeMesCategoriaDisplay(item);
      if (!grupos[cat]) grupos[cat] = [];
      grupos[cat].push(item);
    }
    this.personajesMesPorCategoria = Object.entries(grupos).map(([categoria, items]) => ({ categoria, items }));
    // Inicializar índices y timers
    for (const grupo of this.personajesMesPorCategoria) {
      if (!(grupo.categoria in this.sliderIndices)) {
        this.sliderIndices[grupo.categoria] = 0;
      }
      this.iniciarSliderCategoria(grupo.categoria, grupo.items.length);
    }
  }

  iniciarSliderCategoria(categoria: string, total: number): void {
    if (this.sliderTimers[categoria]) {
      clearInterval(this.sliderTimers[categoria]);
    }
    if (total <= 1) return;
    this.sliderTimers[categoria] = setInterval(() => {
      this.sliderIndices[categoria] = (this.sliderIndices[categoria] + 1) % total;
    }, 3000);
  }

  detenerSliders(): void {
    Object.values(this.sliderTimers).forEach(timer => clearInterval(timer));
    this.sliderTimers = {};
  }

  getPersonajeMesActual(categoria: string): DtoPersonajeMes | null {
    const grupo = this.personajesMesPorCategoria.find(g => g.categoria === categoria);
    if (!grupo || grupo.items.length === 0) return null;
    const idx = this.sliderIndices[categoria] || 0;
    return grupo.items[idx % grupo.items.length];
  }

  news: NewsItem[] = [];
  newsModalOpen = false;
  selectedNews: NewsItem | null = null;
  newsModalImageIndex = 0;

  eventosActivos: DtoEvento[] = [];
  calendarCurrent = new Date();
  calendarDays: CalendarDay[] = [];
  selectedCalendarDate: Date | null = null;
  selectedCalendarEventos: DtoEvento[] = [];
  selectedCalendarEvento: DtoEvento | null = null;
  eventoModalOpen = false;

  constructor(
    private sliderService: SliderService,
    private homeService: HomeService,
    private videoUnidadService: VideoUnidadService,
    private videoInstitucionalService: VideoInstitucionalService,
    private lineaMandoService: LineaMandoService,
    private noticiaService: NoticiaService,
    private personajeMesService: PersonajeMesService,
    private dominioService: DominioService,
    private eventoService: EventoService
  ) {}

  openGrupoLightbox(grupo: any): void {
    this.grupoLightboxItem = grupo;
    this.grupoLightboxOpen = true;
    this.syncBodyModalClass();
  }

  closeGrupoLightbox(): void {
    this.grupoLightboxOpen = false;
    this.grupoLightboxItem = null;
    this.syncBodyModalClass();
  }

  openPersonajeMesLightbox(item: DtoPersonajeMes): void {
    this.personajeMesLightboxItem = item;
    this.personajeMesLightboxOpen = true;
    this.syncBodyModalClass();
  }

  closePersonajeMesLightbox(): void {
    this.personajeMesLightboxOpen = false;
    this.personajeMesLightboxItem = null;
    this.syncBodyModalClass();
  }


  ngAfterViewInit(): void {
    this.startGrupoScroll();
  }

  startGrupoScroll(): void {
    if (this.grupoScrollTimer) {
      clearInterval(this.grupoScrollTimer);
    }
    this.grupoScrollTimer = setInterval(() => {
      if (!this.equiposAltoRendimiento || this.equiposAltoRendimiento.length === 0) return;
      // Si hay al menos un grupo con más de 3 integrantes, avanzar el scroll
      let maxIntegrantes = Math.max(...this.equiposAltoRendimiento.map(g => g.integrantes.length));
      if (maxIntegrantes > 3) {
        this.grupoScrollIndex = (this.grupoScrollIndex + 1) % (maxIntegrantes - 2);
      } else {
        this.grupoScrollIndex = 0;
      }
    }, 2000); // Cambia cada 2 segundos
  }

  getIntegrantesVisibles(grupo: any): any[] {
    if (!grupo.integrantes || grupo.integrantes.length <= 3) {
      return grupo.integrantes;
    }
    // Scroll automático: mostrar 3 a partir del índice
    const start = this.grupoScrollIndex;
    return grupo.integrantes.slice(start, start + 3);
  }

  ngOnInit(): void {
    this.loadBanners();
    this.loadStats();
    this.loadVideoUnidad();
    this.loadVideoInstitucional();
    this.loadLineaMando();
    this.loadNoticias();
    this.loadPersonajesMes();
    this.loadEquiposAltoRendimiento();
    this.loadCategoriasPersonajeMes();
    this.loadEventosActivos();
  }

  private loadEventosActivos(): void {
    this.eventoService.getActivos().subscribe({
      next: (items) => {
        this.eventosActivos = (items ?? []).slice();
        this.buildCalendar();
      },
      error: () => {
        this.eventosActivos = [];
        this.buildCalendar();
      }
    });
  }

  private buildCalendar(): void {
    const first = new Date(this.calendarCurrent.getFullYear(), this.calendarCurrent.getMonth(), 1);
    const start = new Date(first);
    start.setDate(first.getDate() - first.getDay());

    const days: CalendarDay[] = [];
    const today = new Date();

    for (let i = 0; i < 42; i++) {
      const date = new Date(start);
      date.setDate(start.getDate() + i);

      const eventos = this.getEventosByDate(date);
      const isSelected = !!this.selectedCalendarDate && this.isSameDate(date, this.selectedCalendarDate);

      days.push({
        date,
        inCurrentMonth: date.getMonth() === this.calendarCurrent.getMonth(),
        isToday: this.isSameDate(date, today),
        isSelected,
        eventos
      });
    }

    this.calendarDays = days;

    if (!this.selectedCalendarDate) {
      const firstWithEvent = days.find((x) => x.inCurrentMonth && x.eventos.length > 0);
      if (firstWithEvent) {
        this.selectCalendarDay(firstWithEvent);
      }
    }
  }

  private getEventosByDate(date: Date): DtoEvento[] {
    const target = new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();

    return this.eventosActivos.filter((evento) => {
      const ini = this.parseOnlyDate(evento.fechaInicio);
      const fin = this.parseOnlyDate(evento.fechaFin);
      if (!ini || !fin) return false;
      return target >= ini.getTime() && target <= fin.getTime();
    });
  }

  private parseOnlyDate(raw: string | null | undefined): Date | null {
    if (!raw) return null;
    const d = new Date(raw);
    if (Number.isNaN(d.getTime())) return null;
    return new Date(d.getFullYear(), d.getMonth(), d.getDate());
  }

  private isSameDate(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  }

  prevCalendarMonth(): void {
    this.calendarCurrent = new Date(this.calendarCurrent.getFullYear(), this.calendarCurrent.getMonth() - 1, 1);
    this.buildCalendar();
  }

  nextCalendarMonth(): void {
    this.calendarCurrent = new Date(this.calendarCurrent.getFullYear(), this.calendarCurrent.getMonth() + 1, 1);
    this.buildCalendar();
  }

  getCalendarMonthLabel(): string {
    return this.calendarCurrent.toLocaleDateString('es-CO', { month: 'long', year: 'numeric' });
  }

  selectCalendarDay(day: CalendarDay): void {
    this.selectedCalendarDate = new Date(day.date);
    this.selectedCalendarEventos = day.eventos.slice();
    this.selectedCalendarEvento = this.selectedCalendarEventos.length > 0 ? this.selectedCalendarEventos[0] : null;
    this.calendarDays = this.calendarDays.map((x) => ({ ...x, isSelected: this.isSameDate(x.date, day.date) }));
  }

  selectCalendarEvento(evento: DtoEvento): void {
    this.selectedCalendarEvento = evento;
  }

  openCalendarEventoModal(evento: DtoEvento, event?: Event): void {
    event?.stopPropagation();
    this.selectedCalendarEvento = evento;
    this.eventoModalOpen = true;
    this.syncBodyModalClass();
  }

  closeCalendarEventoModal(): void {
    this.eventoModalOpen = false;
    this.syncBodyModalClass();
  }

  getEventoImageUrl(raw: string | null | undefined): string {
    const value = (raw ?? '').trim();
    if (!value) return '';
    if (value.startsWith('http://') || value.startsWith('https://') || value.startsWith('data:')) return value;
    if (value.startsWith('/')) return value;

    const normalized = value.replace(/\\/g, '/');
    if (normalized.includes('/')) {
      return `/${normalized.replace(/^\/+/, '')}`;
    }
    const file = normalized.split('/').filter(Boolean).pop() ?? '';
    if (!file) return '';
    return `/api/EventoUpload/Imagen/${encodeURIComponent(file)}`;
  }

  getEventoFechaRango(evento: DtoEvento): string {
    const ini = new Date(evento.fechaInicio);
    const fin = new Date(evento.fechaFin);
    const fmt = (d: Date) =>
      Number.isNaN(d.getTime())
        ? ''
        : d.toLocaleDateString('es-CO', { day: '2-digit', month: '2-digit', year: 'numeric' });

    const fi = fmt(ini);
    const ff = fmt(fin);
    if (!fi && !ff) return 'Sin fecha';
    if (fi && ff) return fi === ff ? fi : `${fi} - ${ff}`;
    return fi || ff;
  }

  getEventoBadgeClass(evento: DtoEvento): string {
    const inicio = this.parseOnlyDate(evento?.fechaInicio);
    if (!inicio) return 'is-green';

    const today = new Date();
    const hoy = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const diffMs = inicio.getTime() - hoy.getTime();
    const diffDays = Math.ceil(diffMs / 86400000);

    if (diffDays < 2) return 'is-red';
    if (diffDays <= 8) return 'is-orange';
    return 'is-green';
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
        
        this.news = sorted.map(n => {
          const images = this.getNoticiaImageUrls(n.imagenNoticia);
          return {
            id: n.idNoticia,
            date: new Date(n.fechaCreacion).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' }),
            tag: this.mapSeccionToTag(n.seccion),
            title: n.titulo,
            lead: n.subtitulo || '',
            content: n.contenido || '',
            image: images[0] ?? '',
            images,
            megusta: n.megusta || 0
          };
        });
        console.log('News mapeadas:', this.news);
      },
      error: (err) => {
        console.error('Error cargando noticias:', err);
        this.news = [];
      }
    });
  }

  private getNoticiaImageUrl(raw: string): string {
    if (!raw) return '';
    if (raw.startsWith('http://') || raw.startsWith('https://') || raw.startsWith('/api/') || raw.startsWith('data:')) return raw;
    const fileName = raw.split('/').filter(Boolean).pop() ?? '';
    return fileName ? `/api/NoticiaUpload/Imagen/${encodeURIComponent(fileName)}` : '';
  }

  private getNoticiaImageUrls(imagenNoticia: string | null | undefined): string[] {
    const raw = (imagenNoticia ?? '').trim();
    if (!raw) {
      return [];
    }

    const parts = raw
      .split('|')
      .map((x) => x.trim())
      .filter(Boolean);

    const urls = (parts.length > 0 ? parts : [raw])
      .map((x) => this.getNoticiaImageUrl(x))
      .filter(Boolean);

    return urls.length > 0 ? urls : [];
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

   private loadPersonajesMes(): void {
     this.personajeMesService.getAll().subscribe({
        next: (items) => {
          this.personajesMes = sortPersonajesMes(
            (items ?? []).filter((x) => Number(x?.vigente ?? 1) === 1),
            this.categoriasPersonajeMes.map(cat => ({ idDominio: Number(cat.idDominio), descripcion: cat.descripcion }))
          );
          this.agruparPersonajesMesPorCategoria();
        },
       error: () => {
         this.personajesMes = [];
         this.personajesMesPorCategoria = [];
       }
     });
   }

   private loadEquiposAltoRendimiento(): void {
     this.personajeMesService.getAllGrupo().subscribe({
       next: (grupos) => {
         if (!grupos || !Array.isArray(grupos)) {
           this.equiposAltoRendimiento = [];
           return;
         }
         // Para cada grupo, obtener sus integrantes
         const requests = grupos.map((grupo: any) =>
           this.personajeMesService.getByGrupo(grupo.idPersonajeGrupo).toPromise().then((integrantes: import('../../core/services/administracion/personaje-mes.service').DtoPersonajeMes[] | undefined) => {
             const integrantesList = Array.isArray(integrantes) ? integrantes : [];
             return {
               nombreGrupo: grupo.nombreGrupo || grupo.NombreGrupo,
               fotoGrupo: grupo.fotoGrupo || grupo.FotoGrupo,
               numeroActa: grupo.numeroActa || grupo.NumeroActa,
               mes: grupo.mes || grupo.Mes,
               anio: grupo.anio || grupo.Anio,
               integrantes: integrantesList.map(integrante => ({
                 nombres: integrante.nombres,
                 apellidos: integrante.apellidos,
                 grado: integrante.grado
               }))
             };
           })
         );
         Promise.all(requests).then((result) => {
           this.equiposAltoRendimiento = result;
           this.grupoSliderIndex = 0;
         });
       },
       error: () => {
         this.equiposAltoRendimiento = [];
       }
     });
   }

   private loadCategoriasPersonajeMes(): void {
     this.dominioService.getAll().subscribe({
       next: (items) => {
         this.categoriasPersonajeMes = (items ?? [])
           .filter((item) => Number(item?.idPadre ?? 0) === 153 && Number(item?.vigente ?? 0) === 1)
           .sort((a, b) => (a?.descripcion ?? '').localeCompare(b?.descripcion ?? ''));
           if (this.personajesMes.length > 0) {
              this.personajesMes = sortPersonajesMes(this.personajesMes, this.categoriasPersonajeMes.map(cat => ({ idDominio: Number(cat.idDominio), descripcion: cat.descripcion })));
              this.agruparPersonajesMesPorCategoria();
            }
       },
       error: () => {
         this.categoriasPersonajeMes = [];
       }
     });
   }

   getPersonajeMesCategoriaDisplay(item: DtoPersonajeMes): string {
     const idCategoria = Number(item?.idCategoria ?? 0);
     if (!idCategoria) {
       return 'Sin categoría';
     }

     const categoria = this.categoriasPersonajeMes.find(
       (dominio) => Number(dominio?.idDominio ?? 0) === idCategoria
     );

     return (categoria?.descripcion ?? '').trim() || 'Sin categoría';
   }

   
   getPersonajeMesCargoDisplay(item: DtoPersonajeMes): string {
    const unidad = (item?.unidad ?? '').trim();
    return unidad ? this.toTitleCase(unidad) : 'Sin unidad';
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
    if (this.newsModalOpen || this.lineaMandoLightboxOpen || this.personajeMesLightboxOpen || this.grupoLightboxOpen || this.eventoModalOpen) {
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

   getFotoUrl(item: DtoPersonajeMes): string {
     const raw = (item?.fotoModificada ?? '').trim();
     if (!raw) {
       return 'imagenes/policia.jpg';
     }

     if (
       raw.startsWith('http://') ||
       raw.startsWith('https://') ||
       raw.startsWith('/api/') ||
       raw.startsWith('data:image/')
     ) {
       return raw;
     }

     const fileName = raw.split('/').filter(Boolean).pop() ?? '';
     return fileName ? `/api/PersonajeMesUpload/Imagen/${encodeURIComponent(fileName)}` : 'imagenes/policia.jpg';
   }

   getNombreCompleto(item: DtoPersonajeMes): string {
     return `${item?.nombres ?? ''} ${item?.apellidos ?? ''}`.replace(/\s+/g, ' ').trim();
   }

   getGrupoFotoUrl(grupo: any): string {
     const raw = (grupo?.fotoGrupo ?? '').trim();
     if (!raw) {
       return 'imagenes/policia.jpg';
     }

     if (
       raw.startsWith('http://') ||
       raw.startsWith('https://') ||
       raw.startsWith('/api/') ||
       raw.startsWith('data:image/')
     ) {
       return raw;
     }

     const fileName = raw.split('/').filter(Boolean).pop() ?? '';
     return fileName ? `/api/PersonajeMesUpload/Imagen/${encodeURIComponent(fileName)}` : 'imagenes/policia.jpg';
   }

    getGrupoNombre(grupoId: number): string {
      // This would ideally come from the grupos data, but for now return a placeholder
      // In a real implementation, you'd look up the grupo by ID from the gruposAltoRendimiento data
      return `Grupo ${grupoId}`;
    }

    getPersonajeMesFotoUrl(item: DtoPersonajeMes): string {
      return this.getFotoUrl(item);
    }

    getPersonajeMesNombre(item: DtoPersonajeMes): string {
      return this.getNombreCompleto(item);
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


  openNews(item: NewsItem): void {
    this.selectedNews = item;
    this.newsModalImageIndex = 0;
    this.newsModalOpen = true;
    this.syncBodyModalClass();
  }

  closeNews(): void {
    this.newsModalOpen = false;
    this.selectedNews = null;
    this.newsModalImageIndex = 0;
    this.syncBodyModalClass();
  }

  getCurrentNewsModalImage(): string {
    const images = this.selectedNews?.images ?? [];
    if (!images.length) {
      return this.selectedNews?.image ?? '';
    }
    const safeIndex = Math.min(Math.max(this.newsModalImageIndex, 0), images.length - 1);
    return images[safeIndex];
  }

  prevNewsModalImage(): void {
    const images = this.selectedNews?.images ?? [];
    if (images.length <= 1) return;
    this.newsModalImageIndex = (this.newsModalImageIndex - 1 + images.length) % images.length;
  }

  nextNewsModalImage(): void {
    const images = this.selectedNews?.images ?? [];
    if (images.length <= 1) return;
    this.newsModalImageIndex = (this.newsModalImageIndex + 1) % images.length;
  }

  goToNewsModalImage(index: number): void {
    const images = this.selectedNews?.images ?? [];
    if (index < 0 || index >= images.length) return;
    this.newsModalImageIndex = index;
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

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.lineaMandoLightboxOpen) {
      this.closeLineaMandoLightbox();
      return;
    }

    if (this.newsModalOpen) {
      this.closeNews();
      return;
    }

    if (this.eventoModalOpen) {
      this.closeCalendarEventoModal();
    }
  }
}


