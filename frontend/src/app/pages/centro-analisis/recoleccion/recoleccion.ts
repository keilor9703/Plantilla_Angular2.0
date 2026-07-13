import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import {
  RecoleccionService,
  DtoHechoNoticioso,
  DtoHechoNoticiosoRequest,
  DtoMedioCoest,
  DtoMedioCoestRequest,
  DtoIntervinienteCoest,
  DtoIntervinienteCoestRequest,
  DtoDominioCoest,
  DtoCatalogoMedio,
  DtoDependencia,
  DtoAnioItem
} from '../../../core/services/centro-analisis/recoleccion.service';
import { ToastService } from '../../../core/services/toast.service';
import { AlertService } from '../../../core/services/alert.service';

@Component({
  selector: 'app-recoleccion',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './recoleccion.html',
  styleUrls: ['./recoleccion.scss']
})
export class RecoleccionComponent implements OnInit, OnDestroy {
  // --- Estados de Carga ---
  loading = false;
  saving = false;
  deleting = false;
  catalogsLoading = false;

  // --- Listas de Datos ---
  hechos: DtoHechoNoticioso[] = [];
  anios: DtoAnioItem[] = [];
  selectedAnio: number = new Date().getFullYear();
  searchQuery = '';

  // --- Filtros de Búsqueda Avanzada ---
  filterFechaDesde = '';
  filterFechaHasta = '';
  filterUnidad: number | null = null;
  filterCaracter: number | null = null;
  filterTema: number | null = null;
  temasFilter: DtoDominioCoest[] = [];

  // --- Paginación ---
  pageSize = 10;
  pageSizeOptions = [5, 10, 20, 50];
  currentPage = 1;

  // --- Navegación / Vistas ---
  activeTab: 'list' | 'create-hecho' | 'detail-hecho' = 'list';

  // --- Menú de Acciones Desplegable ---
  openActionId: string | null = null;
  openActionAbove = false;
  private _docClickHandler?: (ev: MouseEvent) => void;

  // --- Hecho Seleccionado ---
  hechoSeleccionado: DtoHechoNoticioso | null = null;
  mediosDelHecho: DtoMedioCoest[] = [];
  loadingMedios = false;

  // --- Modales Auxiliares ---
  showMedioModal = false;
  showIntervinienteModal = false;
  selectedMedioIdForInterviniente = '';

  // --- Formularios ---
  hechoForm: DtoHechoNoticiosoRequest = this.emptyHechoForm();
  medioForm: DtoMedioCoestRequest = this.emptyMedioForm('');
  intervinienteForm: DtoIntervinienteCoestRequest = this.emptyIntervinienteForm('');

  // --- Catálogos / Listas de Dominio ---
  unidades: DtoDominioCoest[] = [];
  dependencias: DtoDependencia[] = [];
  caracteres: DtoDominioCoest[] = [];
  temas: DtoDominioCoest[] = [];
  descTemas: DtoDominioCoest[] = [];
  coberturas: DtoDominioCoest[] = [];
  tendencias: DtoDominioCoest[] = [];
  grados: DtoDominioCoest[] = []; // Vocerías (PADRE_ID=54)

  // Medios Catálogo
  todosMediosCatalogo: DtoCatalogoMedio[] = [];
  mediosFiltradosCatalogo: DtoCatalogoMedio[] = [];

  // Tipos de medio estáticos para HERA
  readonly tiposMedioCoest = [
    { id: 5, descripcion: 'Radio' },
    { id: 6, descripcion: 'Prensa' },
    { id: 7, descripcion: 'Televisión' },
    { id: 9, descripcion: 'Web' },
    { id: 75, descripcion: 'Redes Sociales' }
  ];
  selectedTipoMedioId: number | null = null;

  constructor(
    private service: RecoleccionService,
    private toast: ToastService,
    private alert: AlertService
  ) {}

  ngOnInit(): void {
    this.establecerRangoFechasDefecto();
    this.cargarAnios();
    this.cargarHechos();
    this.cargarCatalogosBusqueda();

    // Registrar manejador de clics para cerrar menús al hacer clic fuera
    this._docClickHandler = () => {
      if (this.openActionId !== null) {
        this.openActionId = null;
        this.openActionAbove = false;
      }
    };
    document.addEventListener('click', this._docClickHandler);
  }

  ngOnDestroy(): void {
    if (this._docClickHandler) {
      document.removeEventListener('click', this._docClickHandler);
    }
  }

  // --- CARGA DE DATOS ---

  cargarHechos(): void {
    this.loading = true;
    this.service.getHechos(this.selectedAnio).subscribe({
      next: (data) => {
        this.hechos = data || [];
        this.loading = false;
        this.currentPage = 1;
      },
      error: () => {
        this.hechos = [];
        this.loading = false;
        this.toast.error('Recolección y Análisis', 'Error al cargar los hechos noticiosos.');
      }
    });
  }

  cargarAnios(): void {
    this.service.getAnios().subscribe({
      next: (data) => {
        this.anios = data || [];
        if (!this.anios.some(a => a.anio === this.selectedAnio)) {
          this.anios.unshift({ anio: this.selectedAnio });
        }
      }
    });
  }

  // --- CARGA PEREZOSA (LAZY LOAD) Y CACHÉ DE CATÁLOGOS ---

  private cargarCatalogosBusqueda(): void {
    // Cargamos unidades y caracteres iniciales para que el usuario pueda filtrar de entrada
    this.service.getUnidades().subscribe({
      next: (data) => this.unidades = data || []
    });

    this.service.getDominios(10).subscribe({
      next: (data) => this.caracteres = data || []
    });
  }

  onFilterCaracterChange(caracterId: any): void {
    this.temasFilter = [];
    this.filterTema = null;

    if (!caracterId) return;

    this.service.getDominios(Number(caracterId)).subscribe({
      next: (data) => this.temasFilter = data || []
    });
  }

  establecerRangoFechasDefecto(): void {
    const hoy = new Date();
    const haceDosMeses = new Date();
    haceDosMeses.setMonth(hoy.getMonth() - 2);

    const formatearFecha = (d: Date) => {
      const y = d.getFullYear();
      const m = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      return `${y}-${m}-${day}`;
    };

    this.filterFechaDesde = formatearFecha(haceDosMeses);
    this.filterFechaHasta = formatearFecha(hoy);
  }

  limpiarFiltros(): void {
    this.searchQuery = '';
    this.establecerRangoFechasDefecto();
    this.filterUnidad = null;
    this.filterCaracter = null;
    this.filterTema = null;
    this.temasFilter = [];
    this.currentPage = 1;
  }

  private cargarCatalogosHecho(): void {
    // Si ya fueron cargados previamente, no volvemos a hacer la petición
    if (this.unidades.length > 0 && this.caracteres.length > 0) return;

    this.catalogsLoading = true;

    // Unidades
    this.service.getUnidades().subscribe({
      next: (data) => this.unidades = data || []
    });

    // Dependencias
    this.service.getDependencias().subscribe({
      next: (data) => this.dependencias = data || []
    });

    // Caracteres (PADRE_ID = 10)
    this.service.getDominios(10).subscribe({
      next: (data) => this.caracteres = data || []
    });

    // Coberturas (PADRE_ID = 32)
    this.service.getDominios(32).subscribe({
      next: (data) => this.coberturas = data || []
    });

    // Tendencias (PADRE_ID = 61)
    this.service.getDominios(61).subscribe({
      next: (data) => {
        this.tendencias = data || [];
        this.catalogsLoading = false;
      },
      error: () => this.catalogsLoading = false
    });
  }

  private cargarCatalogosMedio(): void {
    if (this.todosMediosCatalogo.length > 0) return;

    this.catalogsLoading = true;
    this.service.getMediosCatalogo().subscribe({
      next: (data) => {
        this.todosMediosCatalogo = data || [];
        this.catalogsLoading = false;
      },
      error: () => this.catalogsLoading = false
    });
  }

  private cargarCatalogosVocero(): void {
    if (this.grados.length > 0) return;

    this.catalogsLoading = true;
    this.service.getDominios(54).subscribe({
      next: (data) => {
        this.grados = data || [];
        this.catalogsLoading = false;
      },
      error: () => this.catalogsLoading = false
    });
  }

  // --- FILTROS DINÁMICOS HIERÁRQUICOS (CARÁCTER -> TEMA -> DESC TEMA) ---

  onCaracterChange(caracterId: any): void {
    this.temas = [];
    this.descTemas = [];
    this.hechoForm.idTema = undefined;
    this.hechoForm.idDescTema = undefined;

    if (!caracterId) return;

    this.service.getDominios(Number(caracterId)).subscribe({
      next: (data) => this.temas = data || []
    });
  }

  onTemaChange(temaId: any): void {
    this.descTemas = [];
    this.hechoForm.idDescTema = undefined;

    if (!temaId) return;

    this.service.getDominios(Number(temaId)).subscribe({
      next: (data) => this.descTemas = data || []
    });
  }

  onTipoMedioChange(tipoMedioId: any): void {
    this.mediosFiltradosCatalogo = [];
    this.medioForm.idDescMedio = '';

    if (!tipoMedioId) return;

    // Si viene la unidad del hecho seleccionado, intentamos traer medios específicos de la unidad
    if (this.hechoSeleccionado?.idUnidad) {
      this.service.getMediosPorTipoYUnidad(Number(tipoMedioId), this.hechoSeleccionado.idUnidad).subscribe({
        next: (data) => {
          this.mediosFiltradosCatalogo = data || [];
          if (this.mediosFiltradosCatalogo.length === 0) {
            // Si la unidad no tiene asignados, cargamos todos los de ese tipo
            this.mediosFiltradosCatalogo = this.todosMediosCatalogo.filter(m => m.tipoMedioId === Number(tipoMedioId));
          }
        },
        error: () => {
          this.mediosFiltradosCatalogo = this.todosMediosCatalogo.filter(m => m.tipoMedioId === Number(tipoMedioId));
        }
      });
    } else {
      this.mediosFiltradosCatalogo = this.todosMediosCatalogo.filter(m => m.tipoMedioId === Number(tipoMedioId));
    }
  }

  // --- PAGINACIÓN Y FILTRADO DE LA TABLA ---

  get hechosFiltrados(): DtoHechoNoticioso[] {
    let list = this.hechos;

    // 1. Búsqueda por texto (Titular, Unidad, Tema, Carácter)
    const q = (this.searchQuery || '').trim().toLowerCase();
    if (q) {
      list = list.filter(h =>
        h.titular.toLowerCase().includes(q) ||
        (h.unidadStr || '').toLowerCase().includes(q) ||
        (h.temaStr || '').toLowerCase().includes(q) ||
        (h.caracterStr || '').toLowerCase().includes(q)
      );
    }

    // 2. Filtro de Rango de Fechas
    if (this.filterFechaDesde) {
      const desde = new Date(this.filterFechaDesde + 'T00:00:00');
      list = list.filter(h => h.fechaCreacion && new Date(h.fechaCreacion) >= desde);
    }
    if (this.filterFechaHasta) {
      const hasta = new Date(this.filterFechaHasta + 'T23:59:59');
      list = list.filter(h => h.fechaCreacion && new Date(h.fechaCreacion) <= hasta);
    }

    // 3. Filtro por Unidad
    if (this.filterUnidad !== null && this.filterUnidad !== undefined && this.filterUnidad.toString() !== 'null') {
      list = list.filter(h => h.idUnidad === Number(this.filterUnidad));
    }

    // 4. Filtro por Carácter
    if (this.filterCaracter !== null && this.filterCaracter !== undefined && this.filterCaracter.toString() !== 'null') {
      list = list.filter(h => h.idCaracter === Number(this.filterCaracter));
    }

    // 5. Filtro por Tema Principal
    if (this.filterTema !== null && this.filterTema !== undefined && this.filterTema.toString() !== 'null') {
      list = list.filter(h => h.idTema === Number(this.filterTema));
    }

    return list;
  }

  get totalHechos(): number {
    return this.hechosFiltrados.length;
  }

  get totalPaginas(): number {
    return Math.max(1, Math.ceil(this.totalHechos / this.pageSize));
  }

  get pageNumbers(): Array<number | string> {
    const total = this.totalPaginas;
    const current = this.currentPage;
    const windowSize = 5;
    if (total <= windowSize * 2 + 5) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const pages: Array<number | string> = [];
    pages.push(1);
    let start = Math.max(2, current - windowSize);
    let end = Math.min(total - 1, current + windowSize);
    if (start <= 2) start = 2;
    if (end >= total - 1) end = total - 1;
    if (start > 2) pages.push('...');
    for (let i = start; i <= end; i++) pages.push(i);
    if (end < total - 1) pages.push('...');
    pages.push(total);
    return pages;
  }

  get hechosPaginados(): DtoHechoNoticioso[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.hechosFiltrados.slice(start, start + this.pageSize);
  }

  goToPage(page: number): void {
    if (!Number.isFinite(page) || page < 1 || page > this.totalPaginas) return;
    this.currentPage = page;
  }

  onPageClick(p: number | string): void {
    if (p === '...') return;
    const n = Number(p);
    if (Number.isFinite(n)) this.goToPage(n);
  }

  canGoPrev(): boolean {
    return this.currentPage > 1;
  }

  canGoNext(): boolean {
    return this.currentPage < this.totalPaginas;
  }

  prevPagina(): void {
    if (this.canGoPrev()) this.currentPage--;
  }

  nextPagina(): void {
    if (this.canGoNext()) this.currentPage++;
  }

  onPageSizeChange(): void {
    this.currentPage = 1;
  }

  toggleActions(itemId: string, event?: Event): void {
    if (event) event.stopPropagation();
    const willOpen = this.openActionId !== itemId;
    this.openActionId = willOpen ? itemId : null;
    
    try {
      const el = (event && (event.currentTarget as HTMLElement)) || (event && (event.target as HTMLElement));
      if (willOpen && el) {
        const rect = el.getBoundingClientRect();
        const spaceBelow = window.innerHeight - rect.bottom;
        const spaceAbove = rect.top;
        const approxPopover = 140; // popover actions height (Medios, Eliminar)
        this.openActionAbove = spaceBelow < approxPopover && spaceAbove > spaceBelow;
      } else {
        this.openActionAbove = false;
      }
    } catch (e) {
      this.openActionAbove = false;
    }
  }

  isActionsOpen(itemId: string): boolean {
    return this.openActionId === itemId;
  }

  // --- CONTROL DE NAVEGACIÓN ---

  verListado(): void {
    this.activeTab = 'list';
    this.hechoSeleccionado = null;
    this.cargarHechos();
  }

  nuevoHecho(): void {
    this.cargarCatalogosHecho();
    this.hechoForm = this.emptyHechoForm();
    this.temas = [];
    this.descTemas = [];
    this.activeTab = 'create-hecho';
  }

  verDetalleHecho(hecho: DtoHechoNoticioso): void {
    this.hechoSeleccionado = hecho;
    this.activeTab = 'detail-hecho';
    this.cargarMediosDelHecho();
  }

  // --- OPERACIONES HECHO NOTICIOSO ---

  guardarHecho(): void {
    if (!this.hechoForm.idUnidad || !this.hechoForm.titular || !this.hechoForm.idCaracter) {
      this.toast.warning('Recolección y Análisis', 'Por favor complete todos los campos obligatorios (*).');
      return;
    }

    this.saving = true;
    this.service.createHecho(this.hechoForm).subscribe({
      next: (res) => {
        this.saving = false;
        if (res.success) {
          this.toast.success('Recolección y Análisis', 'Hecho noticioso creado con éxito.');
          this.verListado();
        } else {
          this.toast.error('Recolección y Análisis', res.message || 'No fue posible guardar el registro.');
        }
      },
      error: (err) => {
        this.saving = false;
        this.toast.error('Recolección y Análisis', err?.error?.message || 'Error inesperado al guardar.');
      }
    });
  }

  eliminarHecho(hecho: DtoHechoNoticioso): void {
    this.alert.confirm(
      '¿Eliminar Hecho Noticioso?',
      `Esta acción desactivará de forma permanente el titular "${hecho.titular}" y todos los medios/voceros asociados. ¿Desea continuar?`,
      'Confirmar'
    ).then((confirmed: boolean) => {
      if (confirmed) {
        this.deleting = true;
        this.service.deleteHecho(hecho.noticiaId).subscribe({
          next: (res) => {
            this.deleting = false;
            if (res.success) {
              this.toast.success('Recolección y Análisis', 'Registro eliminado correctamente.');
              this.cargarHechos();
            } else {
              this.toast.error('Recolección y Análisis', res.message);
            }
          },
          error: () => {
            this.deleting = false;
            this.toast.error('Recolección y Análisis', 'No se pudo eliminar el registro.');
          }
        });
      }
    });
  }

  // --- OPERACIONES MEDIOS ---

  cargarMediosDelHecho(): void {
    if (!this.hechoSeleccionado) return;

    this.loadingMedios = true;
    this.service.getMediosByNoticia(this.hechoSeleccionado.noticiaId).subscribe({
      next: (data) => {
        this.mediosDelHecho = data || [];
        this.loadingMedios = false;
      },
      error: () => {
        this.mediosDelHecho = [];
        this.loadingMedios = false;
        this.toast.error('Recolección y Análisis', 'Error al cargar los medios del hecho.');
      }
    });
  }

  abrirAgregarMedio(): void {
    if (!this.hechoSeleccionado) return;
    this.cargarCatalogosMedio();
    this.medioForm = this.emptyMedioForm(this.hechoSeleccionado.noticiaId);
    this.selectedTipoMedioId = null;
    this.mediosFiltradosCatalogo = [];
    this.showMedioModal = true;
  }

  guardarMedio(): void {
    if (!this.medioForm.idMedio || !this.medioForm.idDescMedio || !this.medioForm.titular) {
      this.toast.warning('Medios', 'Por favor complete todos los campos obligatorios (*).');
      return;
    }

    // Validar tipo de medio Radio/TV -> Duración
    if ((this.medioForm.idMedio === 5 || this.medioForm.idMedio === 7) && !this.medioForm.duracion) {
      this.toast.warning('Medios', 'La duración es obligatoria para Radio o Televisión.');
      return;
    }

    // Validar tipo de medio Web/Redes -> URL
    if ((this.medioForm.idMedio === 9 || this.medioForm.idMedio === 75) && !this.medioForm.url) {
      this.toast.warning('Medios', 'El URL es obligatorio para Web o Redes Sociales.');
      return;
    }

    this.saving = true;
    this.service.createMedio(this.medioForm).subscribe({
      next: (res) => {
        this.saving = false;
        if (res.success) {
          this.toast.success('Medios', 'Medio registrado correctamente.');
          this.showMedioModal = false;
          this.cargarMediosDelHecho();
        } else {
          this.toast.error('Medios', res.message);
        }
      },
      error: (err) => {
        this.saving = false;
        this.toast.error('Medios', err?.error?.message || 'Error al guardar el medio.');
      }
    });
  }

  eliminarMedio(medio: DtoMedioCoest): void {
    this.alert.confirm(
      '¿Eliminar Medio?',
      'Se desactivará el medio y los intervinientes asociados. ¿Confirmar?',
      'Confirmar'
    ).then((confirmed: boolean) => {
      if (confirmed) {
        this.deleting = true;
        this.service.deleteMedio(medio.medioId).subscribe({
          next: (res) => {
            this.deleting = false;
            if (res.success) {
              this.toast.success('Medios', 'Medio eliminado con éxito.');
              this.cargarMediosDelHecho();
            } else {
              this.toast.error('Medios', res.message);
            }
          },
          error: () => {
            this.deleting = false;
            this.toast.error('Medios', 'Error al eliminar el medio.');
          }
        });
      }
    });
  }

  // --- OPERACIONES INTERVINIENTES ---

  abrirAgregarInterviniente(medioId: string): void {
    this.cargarCatalogosVocero();
    this.selectedMedioIdForInterviniente = medioId;
    this.intervinienteForm = this.emptyIntervinienteForm(medioId);
    this.showIntervinienteModal = true;
  }

  guardarInterviniente(): void {
    if (!this.intervinienteForm.grado || !this.intervinienteForm.nombre || !this.intervinienteForm.cargo) {
      this.toast.warning('Intervinientes', 'Por favor complete todos los campos obligatorios (*).');
      return;
    }

    this.saving = true;
    this.service.createInterviniente(this.intervinienteForm).subscribe({
      next: (res) => {
        this.saving = false;
        if (res.success) {
          this.toast.success('Intervinientes', 'Vocero/Interviniente agregado correctamente.');
          this.showIntervinienteModal = false;
          this.cargarMediosDelHecho();
        } else {
          this.toast.error('Intervinientes', res.message);
        }
      },
      error: (err) => {
        this.saving = false;
        this.toast.error('Intervinientes', err?.error?.message || 'Error al guardar el interviniente.');
      }
    });
  }

  eliminarInterviniente(intItem: DtoIntervinienteCoest): void {
    this.alert.confirm(
      '¿Eliminar Vocero/Interviniente?',
      `Se removerá a ${intItem.nombre} de este medio. ¿Confirmar?`,
      'Confirmar'
    ).then((confirmed: boolean) => {
      if (confirmed) {
        this.deleting = true;
        this.service.deleteInterviniente(intItem.intervinienteId).subscribe({
          next: (res) => {
            this.deleting = false;
            if (res.success) {
              this.toast.success('Intervinientes', 'Vocero eliminado.');
              this.cargarMediosDelHecho();
            } else {
              this.toast.error('Intervinientes', res.message);
            }
          },
          error: () => {
            this.deleting = false;
            this.toast.error('Intervinientes', 'Error al eliminar vocero.');
          }
        });
      }
    });
  }

  // --- EXPORTAR A EXCEL (CSV DE ALTO IMPACTO) ---

  exportarExcel(): void {
    if (this.hechosFiltrados.length === 0) {
      this.toast.warning('Exportar', 'No hay datos en la lista actual para exportar.');
      return;
    }

    // Encabezados
    const headers = [
      'TITULAR',
      'FECHA',
      'UNIDAD',
      'CARACTER',
      'TEMA',
      'DESCRIPCION TEMA',
      'COBERTURA',
      'TENDENCIA',
      'DEPENDENCIA',
      'ESTACION',
      'CANTIDAD MEDIOS'
    ];

    // Formatear filas en CSV
    const rows = this.hechosFiltrados.map(h => [
      `"${(h.titular || '').replace(/"/g, '""')}"`,
      h.fecha ? `"${new Date(h.fecha).toLocaleDateString()}"` : '""',
      `"${(h.unidadStr || '').replace(/"/g, '""')}"`,
      `"${(h.caracterStr || '').replace(/"/g, '""')}"`,
      `"${(h.temaStr || '').replace(/"/g, '""')}"`,
      `"${(h.descTemaStr || '').replace(/"/g, '""')}"`,
      `"${(h.coberturaStr || '').replace(/"/g, '""')}"`,
      `"${(h.tendenciaStr || '').replace(/"/g, '""')}"`,
      `"${(h.dependenciaStr || '').replace(/"/g, '""')}"`,
      `"${(h.estacionStr || '').replace(/"/g, '""')}"`,
      h.contadorMedios
    ]);

    // Crear contenido CSV con UTF-8 BOM para soporte de acentos en Excel
    const csvContent = '\uFEFF' + [
      headers.join(';'),
      ...rows.map(e => e.join(';'))
    ].join('\n');

    // Trigger de descarga
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `hechos_noticiosos_${this.selectedAnio}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    this.toast.success('Exportar', 'Archivo exportado correctamente.');
  }

  // --- HELPERS VACÍOS ---

  private emptyHechoForm(): DtoHechoNoticiosoRequest {
    return {
      idUnidad: 0,
      titular: '',
      fecha: new Date().toISOString().substring(0, 10),
      idCaracter: undefined,
      idTema: undefined,
      idDescTema: undefined,
      idCobertura: undefined,
      idTendencia: undefined,
      idDependencia: undefined,
      idModalidad: undefined,
      idEstacion: undefined
    };
  }

  private emptyMedioForm(noticiaId: string): DtoMedioCoestRequest {
    return {
      noticiaId,
      idMedio: undefined,
      idDescMedio: '',
      titular: '',
      url: '',
      fecha: new Date().toISOString().substring(0, 10),
      idEvidencia: undefined,
      duracion: undefined
    };
  }

  private emptyIntervinienteForm(medioId: string): DtoIntervinienteCoestRequest {
    return {
      medioId,
      grado: undefined,
      nombre: '',
      cargo: ''
    };
  }
}
