import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NoticiaService, DtoNoticia } from '../../core/services/administracion/noticia.service';

@Component({
  selector: 'app-noticias-web',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './noticias.html',
  styleUrl: './noticias.scss'
})
export class NoticiasWeb implements OnInit {
  minimized = false;
  closed = false;
  isLoading = true;
  hasError = false;
  errorMessage = '';

  currentPage = 1;
  itemsPerPage = 8;
  totalPages = 0;

  news: DtoNoticia[] = [];

  modalOpen = false;
  selectedNoticia: DtoNoticia | null = null;
  liked = false;
  modalImages: string[] = [];
  modalImageIndex = 0;

  constructor(private noticiaService: NoticiaService) {}

  ngOnInit(): void {
    this.loadNoticias();
  }

  loadNoticias(): void {
    this.isLoading = true;
    this.hasError = false;
    this.errorMessage = '';

    this.noticiaService.getActivas().subscribe({
      next: (news) => {
        this.news = news.sort((a, b) => 
          new Date(b.fechaCreacion).getTime() - new Date(a.fechaCreacion).getTime()
        );
        this.currentPage = 1;
        this.calculateTotalPages();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.hasError = true;
        this.errorMessage = 'No fue posible cargar las noticias. Intente nuevamente.';
      }
    });
  }

  calculateTotalPages(): void {
    this.totalPages = Math.ceil(this.news.length / this.itemsPerPage);
  }

  get paginatedNews(): DtoNoticia[] {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    const endIndex = startIndex + this.itemsPerPage;
    return this.news.slice(startIndex, endIndex);
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.scrollToTop();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.scrollToTop();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.scrollToTop();
    }
  }

  getImageUrl(noticia: DtoNoticia): string {
    if (!noticia.imagenNoticia) return 'imagenes/actividades/news2.jpg';
    const rawMulti = noticia.imagenNoticia.trim();
    const raw = rawMulti.includes('|') ? rawMulti.split('|')[0].trim() : rawMulti;
    if (raw.startsWith('http')) return raw;
    return `/api/NoticiaUpload/Imagen/${raw.split('/').pop()}`;
  }

  getCurrentModalImage(): string {
    if (this.modalImages.length === 0) {
      return 'imagenes/actividades/news2.jpg';
    }

    return this.modalImages[this.modalImageIndex];
  }

  getNoticiaUrl(noticia: DtoNoticia): string {
    return `/administracion/noticias`;
  }

  openNoticia(noticia: DtoNoticia): void {
    this.selectedNoticia = noticia;
    this.modalImages = this.getImageList(noticia.imagenNoticia);
    this.modalImageIndex = 0;
    this.modalOpen = true;
    document.body.classList.add('modal-open');
  }

  closeNoticia(): void {
    this.modalOpen = false;
    this.selectedNoticia = null;
    this.modalImages = [];
    this.modalImageIndex = 0;
    document.body.classList.remove('modal-open');
  }

  prevModalImage(event?: Event): void {
    event?.stopPropagation();
    if (this.modalImages.length <= 1) return;
    this.modalImageIndex = (this.modalImageIndex - 1 + this.modalImages.length) % this.modalImages.length;
  }

  nextModalImage(event?: Event): void {
    event?.stopPropagation();
    if (this.modalImages.length <= 1) return;
    this.modalImageIndex = (this.modalImageIndex + 1) % this.modalImages.length;
  }

  goToModalImage(index: number, event?: Event): void {
    event?.stopPropagation();
    if (index < 0 || index >= this.modalImages.length) return;
    this.modalImageIndex = index;
  }

  likeNoticia(noticia: DtoNoticia, event: Event): void {
    event.stopPropagation();
    
    const likedNews = this.getLikedNews();
    if (likedNews.includes(noticia.idNoticia)) {
      return;
    }
    
    this.liked = true;
    this.noticiaService.darLike(noticia.idNoticia).subscribe({
      next: () => {
        noticia.megusta = (noticia.megusta || 0) + 1;
        this.saveLikedNews(noticia.idNoticia);
        setTimeout(() => this.liked = false, 1000);
      },
      error: (err) => {
        console.error('Error dando like:', err);
        this.liked = false;
      }
    });
  }

  hasLiked(noticiaId: number): boolean {
    return this.getLikedNews().includes(noticiaId);
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

  private getImageList(rawValue?: string | null): string[] {
    const raw = (rawValue ?? '').trim();
    if (!raw) {
      return ['imagenes/actividades/news2.jpg'];
    }

    const images = raw
      .split('|')
      .map((x) => x.trim())
      .filter((x) => !!x)
      .map((x) => {
        if (x.startsWith('http://') || x.startsWith('https://') || x.startsWith('/api/')) {
          return x;
        }
        const file = x.split('/').filter(Boolean).pop() ?? '';
        return file ? `/api/NoticiaUpload/Imagen/${file}` : '';
      })
      .filter((x) => !!x);

    return images.length > 0 ? images : ['imagenes/actividades/news2.jpg'];
  }

  private scrollToTop(): void {
    const element = document.querySelector('.news-list');
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }
}
