import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HomeService, HomeStats } from '../../core/services/home.service';
import { VideoUnidadService } from '../../core/services/administracion/video-unidad.service';
import { DtoLineaMando, LineaMandoService } from '../../core/services/administracion/linea-mando.service';
import { NoticiaService, DtoNoticia } from '../../core/services/administracion/noticia.service';
import { SafeHtmlPipe } from '../../shared/pipes/safe-html.pipe';
import { environment } from '../../../environments/environment';
import { DtoPersonajeMes, PersonajeMesService, sortPersonajesMes } from '../../core/services/administracion/personaje-mes.service';
import { DominioService } from '../../core/services/administracion/dominio.service';

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

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, SafeHtmlPipe, RouterLink],
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
})
export class HomeComponent implements OnInit, OnDestroy {
  // Slider de grupos de alto rendimiento
  grupoSliderIndex: number = 0;
  grupoSliderTimer: any = null;
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
    if (this.grupoSliderTimer) {
      clearInterval(this.grupoSliderTimer);
      this.grupoSliderTimer = null;
    }
    if (this.grupoScrollTimer) {
      clearInterval(this.grupoScrollTimer);
      this.grupoScrollTimer = null;
    }
  }

  resumeGrupoSlider(): void {
    if (this.grupoSliderPaused) {
      this.grupoSliderPaused = false;
      this.startGrupoSlider();
      this.startGrupoScroll();
    }
  }

  startGrupoSlider(): void {
    if (this.grupoSliderTimer) {
      clearInterval(this.grupoSliderTimer);
    }
    if (!this.equiposAltoRendimiento || this.equiposAltoRendimiento.length <= 1) return;
    this.grupoSliderTimer = setInterval(() => {
      if (!this.grupoSliderPaused) {
        this.grupoSliderIndex = (this.grupoSliderIndex + 1) % this.equiposAltoRendimiento.length;
      }
    }, 3000);
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
  categoriasPersonajeMes: any[] = [];

  stats: HomeStats = {
    usuariosActivos: 0,
    reportesGenerados: 0,
    alertasSistema: 0
  };
  showCursorVerMas = false;
  showSliderCursor = false;
  cursorX = 0;
  cursorY = 0;
  videoUnidadUrl = '';
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

  constructor(
    private homeService: HomeService,
    private videoUnidadService: VideoUnidadService,
    private lineaMandoService: LineaMandoService,
    private noticiaService: NoticiaService,
    private personajeMesService: PersonajeMesService,
    private dominioService: DominioService
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
    this.startGrupoSlider();
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
    this.loadStats();
    this.loadVideoUnidad();
    this.loadLineaMando();
    this.loadNoticias();
    this.loadPersonajesMes();
    this.loadEquiposAltoRendimiento();
    this.loadCategoriasPersonajeMes();
  }




  loadNoticias(): void {
    this.noticiaService.getActivas().subscribe({
      next: (noticias) => {
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
     if (this.grupoSliderTimer) {
      clearInterval(this.grupoSliderTimer);
      this.grupoSliderTimer = null;
    }
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
           this.startGrupoSlider();
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
    if (this.newsModalOpen || this.lineaMandoLightboxOpen || this.personajeMesLightboxOpen || this.grupoLightboxOpen) {
      document.body.classList.add('ui-modal-open');
      return;
    }

    document.body.classList.remove('ui-modal-open');
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
      return `Grupo ${grupoId}`;
    }

    getPersonajeMesFotoUrl(item: DtoPersonajeMes): string {
      return this.getFotoUrl(item);
    }

    getPersonajeMesNombre(item: DtoPersonajeMes): string {
      return this.getNombreCompleto(item);
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
    }
  }

  getNombreMes(mes: number): string {
    const meses = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];
    if (!mes || isNaN(mes)) return '';
    return meses[(mes - 1) % 12] || '';
  }

  prevGrupo(): void {
    if (!this.equiposAltoRendimiento || this.equiposAltoRendimiento.length <= 1) return;
    this.grupoSliderIndex = (this.grupoSliderIndex - 1 + this.equiposAltoRendimiento.length) % this.equiposAltoRendimiento.length;
    this.restartGrupoSlider();
  }

  nextGrupo(): void {
    if (!this.equiposAltoRendimiento || this.equiposAltoRendimiento.length <= 1) return;
    this.grupoSliderIndex = (this.grupoSliderIndex + 1) % this.equiposAltoRendimiento.length;
    this.restartGrupoSlider();
  }

  goToGrupo(idx: number): void {
    if (!this.equiposAltoRendimiento || idx < 0 || idx >= this.equiposAltoRendimiento.length) return;
    this.grupoSliderIndex = idx;
    this.restartGrupoSlider();
  }

  restartGrupoSlider(): void {
    this.startGrupoSlider();
  }

  trackByIndex(index: number): number { return index; }
  trackByNews(_: number, item: NewsItem): number { return item.id; }
  trackByGrupo(_: number, grupo: { categoria: string }): string { return grupo.categoria; }
  trackByPersonajeMes(_: number, item: DtoPersonajeMes): number { return item.idPersonajeMes; }
  trackByLineaMando(_: number, item: DtoLineaMando): number { return item.idLineaMando; }
}


