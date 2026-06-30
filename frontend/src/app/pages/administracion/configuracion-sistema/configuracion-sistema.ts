import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VideoUnidadInfo, VideoUnidadService } from '../../../core/services/administracion/video-unidad.service';
import { LoginVisualItem, LoginVisualService } from '../../../core/services/administracion/login-visual.service';
import { BrandingService } from '../../../core/services/administracion/branding.service';
import { UbicacionService, DtoUnidadOrg } from '../../../core/services/administracion/ubicacion.service';
import { ToastService } from '../../../core/services/toast.service';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-configuracion-sistema',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule],
  templateUrl: './configuracion-sistema.html',
  styleUrls: ['./configuracion-sistema.scss']
})
export class ConfiguracionSistemaComponent implements OnInit {
  videoPanelMinimized = false;
  loginPanelMinimized = false;
  brandingPanelMinimized = false;
  info: VideoUnidadInfo | null = null;
  previewUrl = '';
  loading = false;
  uploading = false;
  selectedVideoFile: File | null = null;
  selectedVideoPreviewUrl = '';
  videoDragOver = false;
  videoDescripcion = '';
  videoObservaciones = '';
  loginLoading = false;
  loginSaving = false;
  loginUploading = false;
  loginIntervalMs = 6000;
loginItems: LoginVisualItem[] = [];
selectedLoginImageFile: File | null = null;
  loginPreviewUrl = '';
  loginImageDragOver = false;
  newLoginImage = { order: 1, title: '', subtitle: '' };

  brandingLoading = false;
  brandingSaving = false;
  brandingUploading = false;
  brandingUploadingFavicon = false;
  brandingSistema = 'OFTIC';
  brandingNombreSistema = 'OFTIC';
  brandingLogoFileName: string | null = null;
  brandingLogoUrl: string | null = null;
  brandingFaviconFileName: string | null = null;
  brandingFaviconUrl: string | null = null;
  brandingSigla = '';
  brandingNombreUnidad = '';
  brandingIdBranding = 0;
  brandingUnidadSistemaId = 0;

  selectedBrandingLogoFile: File | null = null;
  selectedBrandingLogoPreviewUrl = '';
  logoDragOver = false;
  selectedBrandingFaviconFile: File | null = null;
  selectedBrandingFaviconPreviewUrl = '';
  faviconDragOver = false;
  
unidades: DtoUnidadOrg[] = [];
  selectedUnidadConsecutivo: number | null = null;

  showReorderModal = false;
  reorderItems: LoginVisualItem[] = [];

  constructor(
    private videoService: VideoUnidadService,
    private loginVisualService: LoginVisualService,
    private brandingService: BrandingService,
    private ubicacionService: UbicacionService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadCurrent();
    this.loadLoginVisualConfig();
    this.loadBrandingConfig();
    this.loadUnidadesOrganigrama();
  }

  togglePanel(panel: 'video' | 'login' | 'branding'): void {
    if (panel === 'video') {
      this.videoPanelMinimized = !this.videoPanelMinimized;
      return;
    }
    if (panel === 'login') {
      this.loginPanelMinimized = !this.loginPanelMinimized;
      return;
    }
    this.brandingPanelMinimized = !this.brandingPanelMinimized;
  }

  loadCurrent(): void {
    this.loading = true;
    this.videoService.getCurrent().subscribe({
      next: (info) => {
        this.info = info;
        this.previewUrl = info?.hasVideo ? info.url : '';
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.info = null;
        this.previewUrl = '';
        this.toast.error('Configuracion sistema', err?.error?.message ?? 'No fue posible consultar el video actual.');
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    if (!file) {
      return;
    }

    const allowed = ['video/mp4', 'video/webm', 'video/ogg', 'video/quicktime'];
    if (!allowed.includes(file.type)) {
      this.toast.warning('Configuracion sistema', 'Formato inválido. Use MP4, WEBM, OGG o MOV.');
      input.value = '';
      return;
    }

    if (file.size > 100 * 1024 * 1024) {
      this.toast.warning('Configuracion sistema', 'El archivo excede 100MB.');
      input.value = '';
      return;
    }

    this.selectedVideoFile = file;
if (this.selectedVideoPreviewUrl) {
    URL.revokeObjectURL(this.selectedVideoPreviewUrl);
}
this.selectedVideoPreviewUrl = URL.createObjectURL(file);
input.value = '';  // ← esto fuerza que el change event se dispare de nuevo
  }

  onVideoDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.add('drag-over');
    this.videoDragOver = true;
  }

  onVideoDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.videoDragOver = false;
  }

  onVideoDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.videoDragOver = false;
    const file = event.dataTransfer?.files[0];
    if (file) {
      const mockEvent = { target: { files: [file] } } as unknown as Event;
      this.onFileSelected(mockEvent);
    }
  }

  saveVideo(): void {
    if (!this.selectedVideoFile) {
      this.toast.warning('Configuracion sistema', 'Seleccione un archivo de video.');
      return;
    }

    const descripcion = (this.videoDescripcion ?? '').trim();
    if (!descripcion) {
      this.toast.warning('Configuracion sistema', 'La descripción del video es obligatoria.');
      return;
    }

    this.uploading = true;
    this.videoService.upload(
      this.selectedVideoFile,
      descripcion,
      (this.videoObservaciones ?? '').trim() || undefined
    ).subscribe({
      next: (resp) => {
        this.uploading = false;
        if (!resp?.success) {
          this.toast.warning('Configuracion sistema', resp?.message || 'No fue posible cargar el video.');
          return;
        }
        this.toast.success('Configuracion sistema', resp.message || 'Video cargado correctamente.');
        this.selectedVideoFile = null;
        this.selectedVideoPreviewUrl = '';
        this.videoDescripcion = '';
        this.videoObservaciones = '';
        this.loadCurrent();
      },
      error: (err) => {
        this.uploading = false;
        this.toast.error('Configuracion sistema', err?.error?.detail ?? err?.error?.message ?? 'Error cargando video.');
      }
    });
  }

  limpiarFormulario(): void {
    this.selectedVideoFile = null;
    if (this.selectedVideoPreviewUrl) {
      URL.revokeObjectURL(this.selectedVideoPreviewUrl);
    }
    this.selectedVideoPreviewUrl = '';
    this.videoDescripcion = '';
    this.videoObservaciones = '';
  }

loadLoginVisualConfig(): void {
    this.loginLoading = true;
    this.loginVisualService.getAdminConfig().subscribe({
      next: (config) => {
        this.loginIntervalMs = Number(config?.intervalMs ?? 6000);
        this.loginItems = (config?.items ?? [])
          .map((x, idx) => ({
            file: x.file,
            active: x.active ?? true,
            order: Number(x.order ?? idx + 1),
            title: x.title ?? '',
            subtitle: x.subtitle ?? '',
            url: x.url ?? ''
          }))
          .sort((a, b) => a.order - b.order);
        this.originalOrder = [...this.loginItems];
        this.loginLoading = false;
      },
      error: (err) => {
        this.loginLoading = false;
        this.loginItems = [];
        this.originalOrder = [];
        this.toast.error('Visual Login', err?.error?.message ?? 'No fue posible cargar la configuración.');
      }
    });
  }

  onLoginImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    if (!file) {
      return;
    }

    const allowed = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowed.includes(file.type)) {
      this.toast.warning('Visual Login', 'Formato inválido. Use JPG, PNG o WEBP.');
      input.value = '';
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.toast.warning('Visual Login', 'La imagen excede 10MB.');
      input.value = '';
      return;
    }

    this.selectedLoginImageFile = file;
    if (this.loginPreviewUrl) {
      URL.revokeObjectURL(this.loginPreviewUrl);
    }
    this.loginPreviewUrl = URL.createObjectURL(file);
this.newLoginImage = { order: 1, title: '', subtitle: '' };
    input.value = '';
  }

  onLoginImageDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.add('drag-over');
    this.loginImageDragOver = true;
  }

  onLoginImageDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.loginImageDragOver = false;
  }

  onLoginImageDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.loginImageDragOver = false;
    const file = event.dataTransfer?.files[0];
    if (file) {
      const mockEvent = { target: { files: [file] } } as unknown as Event;
      this.onLoginImageSelected(mockEvent);
    }
  }

saveLoginImage(): void {
    if (!this.selectedLoginImageFile) {
      this.toast.warning('Visual Login', 'Seleccione una imagen.');
      return;
    }

    if (this.newLoginImage.order === null || this.newLoginImage.order === undefined) {
      this.toast.warning('Visual Login', 'Seleccione un orden para la imagen.');
      return;
    }

    const orderUsed = this.loginItems.some(item => item.order === this.newLoginImage.order);
    if (orderUsed) {
      this.toast.warning('Visual Login', 'Ese orden ya está en uso. Seleccione otro.');
      return;
    }

    this.loginSaving = true;
    this.loginVisualService.upload(this.selectedLoginImageFile).subscribe({
      next: (resp) => {
        if (!resp?.success || !resp?.fileName) {
          this.loginSaving = false;
          this.toast.warning('Visual Login', resp?.message || 'No fue posible cargar la imagen.');
          return;
        }

        const newItem: LoginVisualItem = {
          file: resp.fileName,
          active: true,
          order: this.newLoginImage.order,
          title: this.newLoginImage.title || null,
          subtitle: this.newLoginImage.subtitle || null,
          url: resp.url
        };

        this.loginVisualService.saveConfig({
          intervalMs: this.loginIntervalMs,
          items: [...this.loginItems, newItem]
        }).subscribe({
          next: (saveResp) => {
            this.loginSaving = false;
            if (!saveResp?.success) {
              this.toast.warning('Visual Login', saveResp?.message || 'No fue posible guardar.');
              return;
            }

            this.selectedLoginImageFile = null;
            if (this.loginPreviewUrl) {
              URL.revokeObjectURL(this.loginPreviewUrl);
              this.loginPreviewUrl = '';
            }
            this.newLoginImage = { order: 1, title: '', subtitle: '' };
            this.loadLoginVisualConfig();
            this.toast.success('Visual Login', saveResp.message || 'Imagen guardada correctamente.');
          },
          error: (err) => {
            this.loginSaving = false;
            this.toast.error('Visual Login', err?.error?.detail ?? err?.error?.message ?? 'Error guardando.');
          }
        });
      },
      error: (err) => {
        this.loginSaving = false;
        this.toast.error('Visual Login', err?.error?.detail ?? err?.error?.message ?? 'Error al subir.');
      }
    });
  }

limpiarLoginForm(): void {
    this.selectedLoginImageFile = null;
    if (this.loginPreviewUrl) {
      URL.revokeObjectURL(this.loginPreviewUrl);
      this.loginPreviewUrl = '';
    }
    this.newLoginImage = { order: 1, title: '', subtitle: '' };
  }

  get availableOrders(): number[] {
    const usedOrders = this.loginItems.map(item => item.order).filter(o => o > 0);
    const maxOrder = usedOrders.length > 0 ? Math.max(...usedOrders) : 0;
    const allOrders = Array.from({ length: maxOrder + 1 }, (_, i) => i + 1);
    return allOrders.filter(order => !usedOrders.includes(order));
  }

    originalOrder: LoginVisualItem[] = [];

  openReorderInline(): void {
    this.originalOrder = [...this.loginItems].sort((a, b) => a.order - b.order);
    this.loginItems = [...this.originalOrder];
  }

  hasOrderChanged(): boolean {
    return this.loginItems.some((item, index) => 
      item.file !== this.originalOrder[index]?.file
    );
  }

  resetReorderInline(): void {
    this.loginItems = [...this.originalOrder];
  }

  onDropInline(event: CdkDragDrop<LoginVisualItem[]>): void {
    moveItemInArray(this.loginItems, event.previousIndex, event.currentIndex);
  }

  saveReorderInline(): void {
    const updatedItems = this.loginItems.map((item, index) => ({
      ...item,
      order: index + 1
    }));
    this.loginSaving = true;
    this.loginVisualService.saveConfig({
      intervalMs: this.loginIntervalMs,
      items: updatedItems
    }).subscribe({
      next: (resp) => {
        this.loginSaving = false;
        if (!resp?.success) {
          this.toast.warning('Visual Login', resp?.message || 'No fue posible guardar el orden.');
          return;
        }
        this.toast.success('Visual Login', resp.message || 'Orden guardado correctamente.');
        this.loadLoginVisualConfig();
      },
      error: (err) => {
        this.loginSaving = false;
        this.toast.error('Visual Login', err?.error?.detail ?? err?.error?.message ?? 'Error guardando orden.');
      }
    });
  }

  getImageUrl(item: LoginVisualItem): string {
    return item.url || '/api/LoginVisual/image/' + item.file;
  }

  getBrandingLogoUrl(): string {
    return this.brandingLogoUrl || (this.brandingLogoFileName ? '/api/Branding/logo/' + this.brandingLogoFileName : '');
  }

  getBrandingFaviconUrl(): string {
    return this.brandingFaviconUrl || (this.brandingFaviconFileName ? '/api/Branding/favicon/' + this.brandingFaviconFileName : '');
  }

  deleteLoginImage(item: LoginVisualItem): void {
    if (!item?.file) {
      return;
    }
    this.loginVisualService.delete(item.file).subscribe({
      next: (resp) => {
        if (!resp?.success) {
          this.toast.warning('Visual Login', resp?.message || 'No fue posible eliminar la imagen.');
          return;
        }
        this.toast.success('Visual Login', resp.message || 'Imagen eliminada.');
        this.loginItems = this.loginItems.filter((x) => x.file !== item.file);
      },
      error: (err) => {
        this.toast.error('Visual Login', err?.error?.detail ?? err?.error?.message ?? 'Error eliminando imagen.');
      }
    });
  }

  loadUnidadesOrganigrama(): void {
    this.ubicacionService.getUnidadesOrganigrama().subscribe({
      next: (data) => {
        this.unidades = data;
      },
      error: (err) => console.error('Error cargando unidades', err)
    });
  }

  onUnidadChange(): void {
    if (!this.selectedUnidadConsecutivo) return;
    const unidad = this.unidades.find(u => u.consecutivo === Number(this.selectedUnidadConsecutivo));
    if (unidad) {
      this.brandingSigla = unidad.sigla;
      this.brandingNombreUnidad = unidad.nombre;
      this.brandingUnidadSistemaId = unidad.consecutivo;
    }
  }

  loadBrandingConfig(): void {
    this.brandingLoading = true;
    this.brandingService.getAdminConfig().subscribe({
      next: (cfg) => {
        this.brandingSistema = (cfg?.sistema ?? 'OFTIC').trim() || 'OFTIC';
        this.brandingNombreSistema = (cfg?.nombreSistema ?? cfg?.systemName ?? 'OFTIC').trim() || 'OFTIC';
        this.brandingLogoFileName = cfg?.logoFileName ?? null;
        this.brandingLogoUrl = cfg?.logoUrl ?? null;
        this.brandingFaviconFileName = cfg?.faviconFileName ?? null;
        this.brandingFaviconUrl = cfg?.faviconUrl ?? null;
        this.brandingSigla = cfg?.sigla ?? '';
        this.brandingNombreUnidad = cfg?.nombreUnidad ?? '';
        this.brandingIdBranding = cfg?.idBranding ?? 0;
        this.brandingUnidadSistemaId = cfg?.unidadSistemaId ?? 0;
        this.selectedUnidadConsecutivo = cfg?.unidadSistemaId ?? null;
        this.brandingLoading = false;
      },
      error: (err) => {
        this.brandingLoading = false;
        this.toast.error('Marca del sistema', err?.error?.message ?? 'No fue posible cargar la configuración de marca.');
      }
    });
  }

onBrandingLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    if (!file) {
      return;
    }

    const allowed = ['image/jpeg', 'image/png', 'image/webp', 'image/svg+xml'];
    if (!allowed.includes(file.type)) {
      this.toast.warning('Marca del sistema', 'Formato inválido. Use JPG, PNG, WEBP o SVG.');
      input.value = '';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.toast.warning('Marca del sistema', 'El logo excede 5MB.');
      input.value = '';
      return;
    }

    this.selectedBrandingLogoFile = file;
    this.createPreviewUrl(file).then(url => {
      if (this.selectedBrandingLogoPreviewUrl) {
        URL.revokeObjectURL(this.selectedBrandingLogoPreviewUrl);
      }
      this.selectedBrandingLogoPreviewUrl = url;
    });
    input.value = '';
  }

  private createPreviewUrl(file: File): Promise<string> {
    return new Promise((resolve) => {
      const reader = new FileReader();
      reader.onload = (e) => resolve(e.target?.result as string);
      reader.onerror = () => resolve(URL.createObjectURL(file));
      reader.readAsDataURL(file);
    });
  }

  limpiarBrandingLogo(): void {
    this.selectedBrandingLogoFile = null;
    if (this.selectedBrandingLogoPreviewUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(this.selectedBrandingLogoPreviewUrl);
    }
    this.selectedBrandingLogoPreviewUrl = '';
  }

  onBrandingFaviconSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    if (!file) {
      return;
    }

    const allowed = ['image/x-icon', 'image/vnd.microsoft.icon', 'image/png', 'image/svg+xml', 'image/webp'];
    if (!allowed.includes(file.type)) {
      this.toast.warning('Marca del sistema', 'Formato inválido. Use ICO, PNG, WEBP o SVG.');
      input.value = '';
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      this.toast.warning('Marca del sistema', 'El favicon excede 2MB.');
      input.value = '';
      return;
    }

    this.selectedBrandingFaviconFile = file;
    this.createPreviewUrl(file).then(url => {
      if (this.selectedBrandingFaviconPreviewUrl) {
        URL.revokeObjectURL(this.selectedBrandingFaviconPreviewUrl);
      }
      this.selectedBrandingFaviconPreviewUrl = url;
    });
    input.value = '';
  }

limpiarBrandingFavicon(): void {
    this.selectedBrandingFaviconFile = null;
    if (this.selectedBrandingFaviconPreviewUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(this.selectedBrandingFaviconPreviewUrl);
    }
    this.selectedBrandingFaviconPreviewUrl = '';
  }

  limpiarBrandingConfig(): void {
    this.brandingSistema = '';
    this.brandingNombreSistema = '';
    this.brandingSigla = '';
    this.brandingUnidadSistemaId = 0;
    this.selectedUnidadConsecutivo = null;
    this.limpiarBrandingLogo();
    this.limpiarBrandingFavicon();
  }

  clearBrandingFileInputs(): void {
    this.selectedBrandingLogoFile = null;
    this.selectedBrandingLogoPreviewUrl = '';
    this.selectedBrandingFaviconFile = null;
    this.selectedBrandingFaviconPreviewUrl = '';
  }

  onBrandingLogoDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.add('drag-over');
    this.logoDragOver = true;
  }

  onBrandingLogoDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.logoDragOver = false;
  }

  onBrandingLogoDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.logoDragOver = false;
    const file = event.dataTransfer?.files[0];
    if (file) {
      const mockEvent = { target: { files: [file] } } as unknown as Event;
      this.onBrandingLogoSelected(mockEvent);
    }
  }

  onBrandingFaviconDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.add('drag-over');
    this.faviconDragOver = true;
  }

  onBrandingFaviconDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.faviconDragOver = false;
  }

  onBrandingFaviconDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    (event.currentTarget as HTMLElement).classList.remove('drag-over');
    this.faviconDragOver = false;
    const file = event.dataTransfer?.files[0];
    if (file) {
      const mockEvent = { target: { files: [file] } } as unknown as Event;
      this.onBrandingFaviconSelected(mockEvent);
    }
  }

  saveBrandingConfig(): void {
    const sistema = (this.brandingSistema ?? '').trim();
    const nombreSistema = (this.brandingNombreSistema ?? '').trim();

    if (!sistema) {
      this.toast.warning('Marca del sistema', 'El campo Sistema es obligatorio.');
      return;
    }
    if (sistema.length > 10) {
      this.toast.warning('Marca del sistema', 'El campo Sistema solo permite hasta 10 caracteres.');
      return;
    }
    if (!nombreSistema) {
      this.toast.warning('Marca del sistema', 'El campo Nombre del sistema es obligatorio.');
      return;
    }
    if (nombreSistema.length > 50) {
      this.toast.warning('Marca del sistema', 'Nombre del sistema solo permite hasta 50 caracteres.');
      return;
    }

    this.brandingSaving = true;

    let uploadedLogoFileName: string | null = null;
    let uploadedFaviconFileName: string | null = null;

    const uploadLogo$ = this.selectedBrandingLogoFile
      ? this.brandingService.uploadLogo(this.selectedBrandingLogoFile)
      : null;

    const uploadFavicon$ = this.selectedBrandingFaviconFile
      ? this.brandingService.uploadFavicon(this.selectedBrandingFaviconFile)
      : null;

    const uploads = [];
    if (uploadLogo$) uploads.push({ observable: uploadLogo$, type: 'logo' });
    if (uploadFavicon$) uploads.push({ observable: uploadFavicon$, type: 'favicon' });

    const saveAfterUpload = () => {
      this.brandingService.saveConfig({
        idBranding: this.brandingIdBranding,
        sistema,
        nombreSistema,
        sigla: this.brandingSigla,
        nombreUnidad: this.brandingNombreUnidad,
        unidadSistemaId: this.brandingUnidadSistemaId,
        logoFileName: uploadedLogoFileName ?? this.brandingLogoFileName,
        faviconFileName: uploadedFaviconFileName ?? this.brandingFaviconFileName
      }).subscribe({
        next: (resp) => {
          this.brandingSaving = false;
          if (!resp?.success) {
            this.toast.warning('Marca del sistema', resp?.message || 'No fue posible guardar la configuración.');
            return;
          }
          this.toast.success('Marca del sistema', resp.message || 'Configuración guardada correctamente.');
          this.clearBrandingFileInputs();
          this.loadBrandingConfig();
        },
        error: (err) => {
          this.brandingSaving = false;
          this.toast.error('Marca del sistema', err?.error?.detail ?? err?.error?.message ?? 'Error guardando configuración.');
        }
      });
    };

    if (uploads.length === 0) {
      saveAfterUpload();
      return;
    }

    let completed = 0;
    const total = uploads.length;

    for (const item of uploads) {
      item.observable.subscribe({
        next: (resp) => {
          if (!resp?.success) {
            this.brandingSaving = false;
            this.toast.warning('Marca del sistema', resp?.message || 'Error subiendo archivo.');
            return;
          }
          if (item.type === 'logo') {
            uploadedLogoFileName = resp.fileName;
            this.brandingLogoFileName = resp.fileName;
            this.brandingLogoUrl = resp.logoUrl;
          } else {
            uploadedFaviconFileName = resp.fileName;
            this.brandingFaviconFileName = resp.fileName;
            this.brandingFaviconUrl = resp.faviconUrl ?? null;
          }
          completed++;
          if (completed === total) {
            saveAfterUpload();
          }
        },
        error: (err) => {
          this.brandingSaving = false;
          this.toast.error('Marca del sistema', err?.error?.detail ?? err?.error?.message ?? 'Error subiendo archivo.');
        }
      });
    }
  }
}


