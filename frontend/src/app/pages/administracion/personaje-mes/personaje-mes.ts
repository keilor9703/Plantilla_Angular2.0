// ...existing code...
import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../../../core/services/toast.service';
import { DominioService, DtoDominio } from '../../../core/services/administracion/dominio.service';
import {
  DtoPersonajeMes,
  DtoPersonajeMesRequest,
  PersonajeMesService,
  sortPersonajesMes
} from '../../../core/services/administracion/personaje-mes.service';
import { UsuarioAdminService } from '../../../core/services/administracion/usuario-admin.service';
import { AlertService } from '../../../core/services/alert.service';

@Component({
  selector: 'app-personaje-mes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './personaje-mes.html',
  styleUrls: ['./personaje-mes.scss']
})
export class PersonajeMesAdminComponent implements OnInit {
  // Permite cerrar la modal de Personaje del Mes con ESC
  @HostListener('document:keydown.escape')
  onEscCerrarModales(): void {
    if (this.mostrarModalEquipoAltoRendimiento) {
      this.cerrarModalEquipoAltoRendimiento();
      return;
    }
    if (this.mostrarModalPersonajeMes) {
      this.cerrarModalPersonajeMes();
    }
  }
          // Método para editar un personaje del mes
          editar(item: DtoPersonajeMes): void {
            this.modoEdicion = true;
            this.idEditando = item.idPersonajeMes;
            this.hayFuncionario = true;
            this.formData = {
              identificacion: item.identificacion ?? '',
              nombres: item.nombres ?? '',
              apellidos: item.apellidos ?? '',
              grado: item.grado ?? '',
              cargo: item.cargo ?? '',
              unidad: item.unidad ?? '',
              IdCategoria: Number(item.idCategoria ?? 0),
              NumeroActa: item.numeroActa ?? '',
              FotoModificada: item.fotoModificada ?? '',
              Mes: this.getMesValue(item),
              Anio: this.getAnioValue(item)
            };
            this.fotoPreview = this.getImageUrl(item.fotoModificada);
            this.mostrarModalPersonajeMes = true;
            
          }
        // Devuelve la descripción de la categoría según el idCategoria
        getCategoriaDescripcion(idCategoria: number | string | null | undefined): string {
          const id = Number(idCategoria ?? 0);
          const cat = this.categorias.find(c => Number(c.idDominio) === id);
          return cat?.descripcion ?? 'Sin categoría';
        }

        // Getter para mostrar la descripción de la categoría seleccionada en el formulario
        get categoriaDescripcion(): string {
          return this.getCategoriaDescripcion(this.formData.IdCategoria);
        }
      // Limpia el formulario de personaje del mes
      limpiarForm(): void {
        this.formData = this.createEmptyForm();
        this.fotoPreview = 'imagenes/policia.jpg';
        this.modoEdicion = false;
        this.hayFuncionario = false;
        this.idEditando = null;
        this.searchIdentificacion = '';
        this.selectedImageFile = null;
        this.uploading = false;
      }

      // Resuelve la categoría por grado/categoría descripción
        private resolverCategoriaPorFuncionario(grado: string | null | undefined, categoriaDescripcion: string | null | undefined): DtoDominio | null {
            const gradoNorm = this.normalizarTexto(grado);
            const categNorm = this.normalizarTexto(categoriaDescripcion);

            // Caso especial: grado PATRULLERO con categoría NIVEL EJECUTIVO → PATRULLEROS
            if (gradoNorm.includes('patrullero') && categNorm.includes('nivel ejecutivo')) {
              return this.buscarCategoriaPorKeyword('patrullero');
            }
            if (categNorm.includes('no uniformado')) return this.buscarCategoriaPorKeyword('no uniformado');
            if (categNorm.includes('nivel ejecutivo')) return this.buscarCategoriaPorKeyword('nivel ejecutivo');
            if (categNorm.includes('suboficial')) return this.buscarCategoriaPorKeyword('suboficial');
            if (categNorm.includes('oficial') && !categNorm.includes('suboficial')) return this.buscarCategoriaPorKeyword('oficial');
            if (categNorm.includes('agente') || categNorm.includes('categoria agentes') || categNorm.includes('categoría agentes')) return this.buscarCategoriaPorKeyword('agente');
            if (categNorm.includes('patrullero')) return this.buscarCategoriaPorKeyword('patrullero');
            if (categNorm.includes('alumno')) return this.buscarCategoriaPorKeyword('estudiante');
            if (categNorm.includes('auxiliar')) return this.buscarCategoriaPorKeyword('auxiliar');

            return null;
        }

          private buscarCategoriaPorKeyword(keyword: string): DtoDominio | null {
      return this.categorias.find((item) => this.normalizarTexto(item.descripcion).includes(keyword)) ?? null;
    }

    private normalizarTexto(value: string | null | undefined): string {
      return (value ?? '')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[^a-zA-Z0-9\s]/g, ' ')
        .replace(/\s+/g, ' ')
        .trim()
        .toLowerCase();
    }

      // Obtiene la extensión de imagen a partir del mimeType
      getImageExtension(mimeType: string): string {
        switch (mimeType) {
          case 'image/jpeg':
          case 'image/jpg':
            return '.jpg';
          case 'image/png':
            return '.png';
          case 'image/webp':
            return '.webp';
          default:
            return '';
        }
      }
    // --- EDICIÓN Y ELIMINACIÓN DE EQUIPOS DE ALTO RENDIMIENTO ---
    modoEdicionEquipo = false;
    idEditandoEquipo: number | null = null;

    editarEquipo(grupo: any): void {
      // Cargar datos del grupo en la modal
      this.modoEdicionEquipo = true;
      this.idEditandoEquipo = grupo.idPersonajeGrupo || grupo.IdPersonajeGrupo;
      this.nombreGrupoEquipo = grupo.nombreGrupo;
      this.fotoPreviewEquipo = this.getImageUrl(grupo.fotoGrupo);
      this.fotoModificadaEquipo = grupo.fotoGrupo;
      this.datosFijosEquipo = {
        NumeroActa: grupo.numeroActa,
        Mes: grupo.mes,
        Anio: grupo.anio
      };
      // Cargar integrantes (ya están en grupo.integrantes)
      this.integrantesEquipo = (grupo.integrantes || []).map((i: any) => ({
        identificacion: i.identificacion,
        nombres: i.nombres,
        apellidos: i.apellidos,
        grado: i.grado,
        cargo: i.cargo,
        unidad: i.unidad,
        IdCategoria: i.IdCategoria || 0,
        NumeroActa: grupo.numeroActa,
        FotoModificada: i.FotoModificada || '',
        Mes: grupo.mes,
        Anio: grupo.anio
      }));
      this.mostrarModalEquipoAltoRendimiento = true;
      
    }

    async eliminarEquipo(grupo: any): Promise<void> {
      const confirmed = await this.alert.confirm(
        'Eliminar equipo',
        '¿Está seguro que desea eliminar este equipo? Esta acción no se puede deshacer.',
        'Eliminar',
        'Cancelar'
      );
      if (!confirmed) return;

      const idGrupo = grupo.idPersonajeGrupo || grupo.IdPersonajeGrupo;
      this.saving = true;
      this.personajeMesService.deleteGrupo(idGrupo).subscribe({
        next: (resp) => {
          this.saving = false;
          if (resp.success) {
            this.toast.success('Equipo', resp.message || 'Equipo eliminado correctamente');
            this.cargarEquipos();
          } else {
            this.toast.error('Equipo', resp.message || 'No se pudo eliminar el equipo');
          }
        },
        error: (err) => {
          this.saving = false;
          this.toast.error('Equipo', err?.error?.message || 'Error al eliminar el equipo');
        }
      });
    }

    // Al guardar equipo, si está en modo edición, actualizar; si no, crear
    guardarEquipoAltoRendimiento(): void {
      if (this.integrantesEquipo.length === 0) {
        this.toast.warning('Equipo', 'Debe agregar al menos un integrante');
        return;
      }
      if (!this.nombreGrupoEquipo?.trim()) {
        this.toast.warning('Equipo', 'Debe ingresar el número de grupo o equipo');
        return;
      }

      const subirYGuardarEquipo = () => {
        this.saving = true;
        const grupoRequest = {
          NombreGrupo: this.nombreGrupoEquipo.trim(),
          FotoGrupo: this.fotoModificadaEquipo,
          NumeroActa: this.datosFijosEquipo.NumeroActa,
          Mes: this.datosFijosEquipo.Mes,
          Anio: this.datosFijosEquipo.Anio
        };
        if (this.modoEdicionEquipo && this.idEditandoEquipo) {
          // Actualizar grupo existente
          this.personajeMesService.updateGrupo(this.idEditandoEquipo, grupoRequest).subscribe({
            next: (grupoResp) => {
              if (!grupoResp.success) {
                this.saving = false;
                this.toast.error('Equipo', grupoResp.message ?? 'Error al actualizar el grupo');
                return;
              }
              this.saving = false;
              this.toast.success('Equipo', grupoResp.message ?? 'Equipo actualizado correctamente');
              this.cerrarModalEquipoAltoRendimiento();
              this.cargarPersonajes();
              this.cargarEquipos();
            },
            error: (err) => {
              this.saving = false;
              this.toast.error('Equipo', err?.error?.message ?? 'Error al actualizar el grupo');
            }
          });
        } else {
          // Crear grupo nuevo (flujo original)
          this.personajeMesService.createGrupo(grupoRequest).subscribe({
            next: (grupoResp) => {
              if (!grupoResp.success) {
                this.saving = false;
                this.toast.error('Equipo', grupoResp.message ?? 'Error al crear el grupo');
                return;
              }
              const integrantesConGrupoId = this.integrantesEquipo.map(integrante => ({
                ...integrante,
                IdPersonajeGrupo: grupoResp.id
              }));
              this.personajeMesService.createBulk(integrantesConGrupoId).subscribe({
                next: (bulkResp) => {
                  this.saving = false;
                  if (!bulkResp.success) {
                    this.toast.error('Equipo', bulkResp.message ?? 'Error al guardar los integrantes');
                    return;
                  }
                  this.toast.success('Equipo', bulkResp.message ?? 'Registros guardados correctamente');
                  this.cerrarModalEquipoAltoRendimiento();
                  this.cargarPersonajes();
                  this.cargarEquipos();
                },
                error: (err) => {
                  this.saving = false;
                  this.toast.error('Equipo', err?.error?.message ?? 'Error al guardar los integrantes');
                }
              });
            },
            error: (err) => {
              this.saving = false;
              this.toast.error('Equipo', err?.error?.message ?? 'Error al crear el grupo');
            }
          });
        }
      };

      // Subir imagen solo si hay una nueva seleccionada
      if (this.selectedImageFileEquipo) {
        this.uploadingEquipo = true;
        this.optimizeImageFile(this.selectedImageFileEquipo)
          .then((optimizedFile) => {
            if (optimizedFile.size > this.maxImageBytes) {
              this.uploadingEquipo = false;
              this.toast.warning('Imagen', 'El archivo no puede superar 2MB.');
              return;
            }
            this.personajeMesService.uploadImage(optimizedFile).subscribe({
              next: (resp) => {
                this.uploadingEquipo = false;
                if (!resp.success) {
                  this.toast.warning('Imagen', resp.message ?? 'No fue posible cargar la imagen.');
                  return;
                }
                this.fotoModificadaEquipo = resp.fileName;
                this.fotoPreviewEquipo = resp.url || this.getImageUrl(resp.fileName);
                this.selectedImageFileEquipo = null;
                subirYGuardarEquipo();
              },
              error: (err) => {
                this.uploadingEquipo = false;
                this.toast.error('Imagen', err?.error?.message ?? 'Error cargando la imagen.');
              }
            });
          })
          .catch(() => {
            this.uploadingEquipo = false;
            this.toast.warning('Imagen', 'No fue posible procesar la imagen seleccionada.');
          });
      } else {
        if (!this.fotoModificadaEquipo?.trim()) {
          this.toast.warning('Equipo', 'Debe subir una foto del equipo');
          return;
        }
        subirYGuardarEquipo();
      }
    }

    cerrarModalEquipoAltoRendimiento(): void {
      this.mostrarModalEquipoAltoRendimiento = false;
      this.integrantesEquipo = [];
      this.searchIdentificacionEquipo = '';
      this.datosFijosEquipo = { Mes: 0, Anio: 0, NumeroActa: '' };
      this.idCategoriaEquipo = 0;
      this.fotoPreviewEquipo = 'imagenes/policia.jpg';
      this.fotoModificadaEquipo = '';
      this.uploadingEquipo = false;
      this.nombreGrupoEquipo = '';
      this.modoEdicionEquipo = false;
      this.idEditandoEquipo = null;
    }
  personajes: DtoPersonajeMes[] = [];
  categorias: DtoDominio[] = [];
  equipos: any[] = [];
  private readonly todosLosMeses = [
    { value: 1, label: 'Enero' },
    { value: 2, label: 'Febrero' },
    { value: 3, label: 'Marzo' },
    { value: 4, label: 'Abril' },
    { value: 5, label: 'Mayo' },
    { value: 6, label: 'Junio' },
    { value: 7, label: 'Julio' },
    { value: 8, label: 'Agosto' },
    { value: 9, label: 'Septiembre' },
    { value: 10, label: 'Octubre' },
    { value: 11, label: 'Noviembre' },
    { value: 12, label: 'Diciembre' }
  ];

  get mesesDisponibles(): { value: number; label: string }[] {
    const mesVigente = new Date().getMonth() + 1;
    const mesAnterior = mesVigente === 1 ? 12 : mesVigente - 1;
    return this.todosLosMeses.filter(m => m.value === mesAnterior || m.value === mesVigente);
  }

  get aniosDisponibles(): number[] {
    const currentYear = new Date().getFullYear();
    return this.formData.Mes === 1 ? [currentYear - 1, currentYear] : [currentYear];
  }

loading = false;
  saving = false;
  uploading = false;
  searchingFuncionario = false;
  modoEdicion = false;
  hayFuncionario = false;
  idEditando: number | null = null;
  mostrarModalPersonajeMes = false;
  mostrarModalEquipoAltoRendimiento = false;

  searchIdentificacion = '';
  fotoPreview = 'imagenes/policia.jpg';

  searchIdentificacionEquipo = '';
  integrantesEquipo: DtoPersonajeMesRequest[] = [];
  datosFijosEquipo = {
    Mes: 0,
    Anio: 0,
    NumeroActa: ''
  };
   nombreGrupoEquipo = '';
   searchingFuncionarioEquipo = false;
   idCategoriaEquipo = 0;
   fotoPreviewEquipo = 'imagenes/policia.jpg';
   fotoModificadaEquipo = '';
   uploadingEquipo = false;

  private readonly maxImageBytes = 2 * 1024 * 1024;
  private readonly targetOptimizedImageBytes = 90 * 1024;
  private readonly maxImageDimension = 960;

  formData: DtoPersonajeMesRequest = this.createEmptyForm();

  constructor(
    private personajeMesService: PersonajeMesService,
    private dominioService: DominioService,
    private toast: ToastService,
    private usuarioAdminService: UsuarioAdminService
    ,private alert: AlertService
  ) {}

  ngOnInit(): void {
    this.cargarCategoriasYPersonajesYEquipos();
  }
  private cargarCategoriasYPersonajesYEquipos(): void {
    this.dominioService.getAll().subscribe({
      next: (data) => {
        this.categorias = (data ?? [])
          .filter((item) => Number(item.idPadre) === 153 && Number(item.vigente) === 1)
          .sort((a, b) => (a.descripcion ?? '').localeCompare(b.descripcion ?? ''));
        this.cargarPersonajes();
        this.cargarEquipos();
      },
      error: (err) => {
        this.toast.error('Categorías', err?.error?.message ?? 'No se pudieron cargar las categorías.');
        this.cargarPersonajes();
        this.cargarEquipos();
      }
    });
  }

  private cargarEquipos(): void {
    this.personajeMesService.getAllGrupo().subscribe({
      next: (data) => {
        const grupos = data ?? [];
        if (grupos.length === 0) {
          this.equipos = [];
          return;
        }
        // Para cada grupo, cargar sus integrantes
        let pendientes = grupos.length;
        grupos.forEach((grupo: any) => {
          this.personajeMesService.getByGrupo(grupo.idPersonajeGrupo || grupo.IdPersonajeGrupo).subscribe({
            next: (integrantes) => {
              grupo.integrantes = (integrantes ?? []).map(i => ({
                nombres: i.nombres,
                apellidos: i.apellidos,
                grado: i.grado
              }));
              pendientes--;
              if (pendientes === 0) {
                this.equipos = grupos;
              }
            },
            error: () => {
              grupo.integrantes = [];
              pendientes--;
              if (pendientes === 0) {
                this.equipos = grupos;
              }
            }
          });
        });
      },
      error: (err) => {
        this.toast.error('Equipos', err?.error?.message ?? 'No se pudieron cargar los equipos.');
      }
    });
  }

  private createEmptyForm(): DtoPersonajeMesRequest {
    const today = new Date();
    return {
      identificacion: '',
      nombres: '',
      apellidos: '',
      grado: '',
      cargo: '',
      unidad: '',
      IdCategoria: 0,
      NumeroActa: '',
      FotoModificada: '',
      Mes: today.getMonth() + 1,
      Anio: today.getFullYear()
    };
  }

  onMesChange(nuevoMes: number): void {
    const currentYear = new Date().getFullYear();
    if (nuevoMes !== 1 && this.formData.Anio === currentYear - 1) {
      this.formData.Anio = currentYear;
    }
  }

  // --- Máscara y validación para Número de Acta ---
  private readonly numeroActaRegex = /^[A-Z]{2}-\d{4}-\d{6}-[A-Z]{5}$/;

  getNumeroActaMask(): string {
    const val = (this.formData.NumeroActa || '').toUpperCase();
    let mask = '__-____-______-_____';
    let out = '';
    let vi = 0;
    for (let i = 0; i < mask.length; i++) {
      if (mask[i] === '_' && vi < val.length) {
        out += val[vi];
        vi++;
      } else if (mask[i] === '_') {
        out += '_';
      } else {
        out += mask[i];
      }
    }
    return out;
  }

  onNumeroActaInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value.toUpperCase();
    // Eliminar guiones y guiones bajos de la máscara
    value = value.replace(/[-_]/g, '');
    // Solo letras y números
    value = value.replace(/[^A-Z0-9]/g, '');
    // Construir el valor bloque a bloque, en orden de ingreso
    let result = '';
    let i = 0;
    // Primer bloque: 2 letras
    while (result.length < 2 && i < value.length) {
      if (/[A-Z]/.test(value[i])) result += value[i];
      i++;
    }
    // Segundo bloque: 4 números
    let count = 0;
    while (count < 4 && i < value.length) {
      if (/[0-9]/.test(value[i])) {
        result += value[i];
        count++;
      }
      i++;
    }
    // Tercer bloque: 6 números
    count = 0;
    while (count < 6 && i < value.length) {
      if (/[0-9]/.test(value[i])) {
        result += value[i];
        count++;
      }
      i++;
    }
    // Cuarto bloque: 5 letras
    count = 0;
    while (count < 5 && i < value.length) {
      if (/[A-Z]/.test(value[i])) {
        result += value[i];
        count++;
      }
      i++;
    }
    this.formData.NumeroActa = result;
    // Actualizar el input con la máscara
    const masked = this.getNumeroActaMask();
    input.value = masked;
    // Mantener el cursor en la posición correcta
    setTimeout(() => {
      let pos = input.selectionStart || 0;
      // Si el usuario intenta escribir en un guion virtual, saltar al siguiente editable
      if (masked[pos] === '-') pos++;
      input.setSelectionRange(pos, pos);
    }, 0);
  }

  onNumeroActaKeydown(event: KeyboardEvent): void {
    const input = event.target as HTMLInputElement;
    const pos = input.selectionStart || 0;
    // Permitir borrar normalmente si no está en un guion
    if (event.key === 'Backspace') {
      // Si el cursor está justo después de un guion, saltar el guion y borrar el carácter anterior real
      if (pos > 0 && input.value[pos - 1] === '-') {
        event.preventDefault();
        // Mover el cursor antes del guion y borrar el carácter anterior real
        let before = input.value.slice(0, pos - 1);
        let after = input.value.slice(pos);
        // Eliminar el carácter anterior al guion en la máscara interna
        let raw = (this.formData.NumeroActa || '').split('');
        // Calcular a qué bloque corresponde
        let blockIdx = 0;
        if (pos <= 2) blockIdx = 0;
        else if (pos <= 7) blockIdx = 1;
        else if (pos <= 14) blockIdx = 2;
        else blockIdx = 3;
        // Eliminar el último carácter del bloque anterior si existe
        if (blockIdx > 0) {
          let blockLens = [2, 4, 6, 5];
          let idx = blockLens.slice(0, blockIdx).reduce((a, b) => a + b, 0) - 1;
          if (idx >= 0) raw.splice(idx, 1);
          this.formData.NumeroActa = raw.join('');
          setTimeout(() => {
            input.value = this.getNumeroActaMask();
            input.setSelectionRange(pos - 2, pos - 2);
          }, 0);
        }
      } else if (pos === input.value.length) {
        // Si el cursor está al final, borrar el último carácter real
        event.preventDefault();
        let raw = (this.formData.NumeroActa || '').split('');
        if (raw.length > 0) {
          raw.pop();
          this.formData.NumeroActa = raw.join('');
          setTimeout(() => {
            input.value = this.getNumeroActaMask();
            input.setSelectionRange(input.value.length, input.value.length);
          }, 0);
        }
      }
    }
  }

  onNumeroActaFocus(event: Event): void {
    setTimeout(() => {
      const input = event.target as HTMLInputElement;
      input.setSelectionRange(0, 0);
    }, 0);
  }

  onNumeroActaBlur(event: Event): void {
    // Opcional: limpiar guiones bajos si el valor es válido
    if (!this.numeroActaInvalido()) {
      const input = event.target as HTMLInputElement;
      input.value = this.formData.NumeroActa;
    }
  }

  numeroActaInvalido(): boolean {
    const val = (this.formData.NumeroActa || '').trim();
    return !this.numeroActaRegex.test(val);
  }

  cargarPersonajes(): void {
    this.loading = true;
    this.personajeMesService.getAll().subscribe({
      next: (data) => {
        // El backend ya filtra por grupo y vigencia, así que solo ordenamos
        this.personajes = sortPersonajesMes(data ?? [], this.categorias);
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toast.error('Personaje del mes', err?.error?.message ?? 'No se pudo cargar el listado.');
      }
    });
  }

  cargarCategorias(): void {
    this.dominioService.getAll().subscribe({
      next: (data) => {
        this.categorias = (data ?? [])
          .filter((item) => Number(item.idPadre) === 1 && Number(item.vigente) === 1)
          .sort((a, b) => (a.descripcion ?? '').localeCompare(b.descripcion ?? ''));
        if (this.personajes.length > 0) {
          this.personajes = sortPersonajesMes(this.personajes, this.categorias);
        }
      },
      error: (err) => {
        this.toast.error('Categorías', err?.error?.message ?? 'No se pudieron cargar las categorías.');
      }
    });
  }

  private cargarCategoriasYPersonajes(): void {
    this.dominioService.getAll().subscribe({
      next: (data) => {
        this.categorias = (data ?? [])
          .filter((item) => Number(item.idPadre) === 1 && Number(item.vigente) === 1)
          .sort((a, b) => (a.descripcion ?? '').localeCompare(b.descripcion ?? ''));
        this.cargarPersonajes();
      },
      error: (err) => {
        this.toast.error('Categorías', err?.error?.message ?? 'No se pudieron cargar las categorías.');
        this.cargarPersonajes();
      }
    });
  }


  onSearchEnter(event: Event): void {
    event.preventDefault();
    this.buscarFuncionario();
  }

  buscarFuncionario(): void {
    const doc = this.searchIdentificacion.trim();
    if (!doc) {
      this.toast.warning('Buscar', 'Ingrese un número de identificación');
      return;
    }

    this.searchingFuncionario = true;
    this.hayFuncionario = false;

    this.usuarioAdminService.consultarFuncionarioPersonajeMes(doc).subscribe({
      next: (resp) => {
        const func = resp.funcionario;

        if (!func || !func.nombres) {
          this.searchingFuncionario = false;
          this.toast.warning('Buscar', 'No se encontró funcionario con esa identificación');
          this.fotoPreview = 'imagenes/policia.jpg';
          return;
        }

        const activo = func.activo !== false;
        if (!activo) {
          this.searchingFuncionario = false;
          this.toast.warning('Buscar', 'El usuario está inactivo en el sistema');
          this.fotoPreview = 'imagenes/policia.jpg';
          return;
        }

        const identificacion = (func.identificacion ?? doc).trim();
        const fotoConsultada = (resp.fotoBase64 ?? '').trim();
        const grado = (func.nombreGrado ?? func.cargo ?? '').trim();
        const categoriaResuelta = this.resolverCategoriaPorFuncionario(grado, func.categoriaDescripcion ?? undefined);

        this.formData = {
          identificacion,
          nombres: (func.nombres ?? '').trim(),
          apellidos: (func.apellidos ?? '').trim(),
          grado,
          cargo: (func.cargo ?? '').trim(),
          unidad: (func.dependencia ?? '').trim(),
          IdCategoria: Number(categoriaResuelta?.idDominio ?? 0),
          NumeroActa: (this.formData.NumeroActa ?? '').trim(),
          FotoModificada: '',
          Mes: Number(this.formData.Mes ?? 0),
          Anio: Number(this.formData.Anio ?? 0),
        };

        this.fotoPreview = this.getImageUrl(fotoConsultada);
        this.hayFuncionario = true;
        this.searchingFuncionario = false;

        if (fotoConsultada) {
          this.persistirFotoConsultada(fotoConsultada, identificacion);
        }

        this.toast.success('Buscar', 'Datos del funcionario cargados');
      },
      error: (err) => {
        this.searchingFuncionario = false;
        this.hayFuncionario = false;
        this.fotoPreview = 'imagenes/policia.jpg';
        this.toast.error('Buscar', err?.error?.message ?? 'Error al consultar funcionario');
      }
    });
  }

  guardar(): void {
    const identificacion = (this.formData.identificacion ?? '').trim();
    const nombres = (this.formData.nombres ?? '').trim();
    const apellidos = (this.formData.apellidos ?? '').trim();
    const grado = (this.formData.grado ?? '').trim();
    const cargo = (this.formData.cargo ?? '').trim();
    const unidad = (this.formData.unidad ?? '').trim();
    const numeroActa = (this.formData.NumeroActa ?? '').trim();
    const idCategoria = Number(this.formData.IdCategoria ?? 0);
    const mes = Number(this.formData.Mes ?? 0);
    const anio = Number(this.formData.Anio ?? 0);

    if (!this.modoEdicion && !this.hayFuncionario) {
      this.toast.warning('Personaje del mes', 'Primero consulte un funcionario por cédula.');
      return;
    }
    if (!identificacion) {
      this.toast.warning('Personaje del mes', 'La identificación es obligatoria.');
      return;
    }
    if (!nombres) {
      this.toast.warning('Personaje del mes', 'Los nombres son obligatorios.');
      return;
    }
    if (!cargo) {
      this.toast.warning('Personaje del mes', 'El cargo es obligatorio.');
      return;
    }
    if (!unidad) {
      this.toast.warning('Personaje del mes', 'La unidad es obligatoria.');
      return;
    }
    this.formData.IdCategoria = idCategoria;
    if (!idCategoria) {
      this.toast.warning('Personaje del mes', 'Debe seleccionar una categoría.');
      return;
    }
    if (!numeroActa) {
      this.toast.warning('Personaje del mes', 'El número de acta es obligatorio.');
      return;
    }
    if (!mes) {
      this.toast.warning('Personaje del mes', 'Debe seleccionar un mes.');
      return;
    }
    if (!anio) {
      this.toast.warning('Personaje del mes', 'Debe seleccionar un año.');
      return;
    }
    if (mes < 1 || mes > 12) {
      this.toast.warning('Personaje del mes', 'El mes seleccionado no es válido.');
      return;
    }
    if (!this.aniosDisponibles.includes(anio)) {
      this.toast.warning('Personaje del mes', 'El año seleccionado no está dentro del rango permitido.');
      return;
    }

    // Subir imagen si hay una nueva seleccionada
    const subirYGuardar = (fileName: string | null) => {
      const fotoModificada = fileName ?? ((this.formData.FotoModificada ?? '').trim() || null);
      const payload: DtoPersonajeMesRequest = {
        identificacion,
        nombres,
        apellidos,
        grado,
        cargo,
        unidad,
        IdCategoria: idCategoria,
        NumeroActa: numeroActa,
        FotoModificada: fotoModificada,
        Mes: mes,
        Anio: anio
      };
      this.saving = true;
      const request$ = this.modoEdicion && this.idEditando
        ? this.personajeMesService.update(this.idEditando, payload)
        : this.personajeMesService.create(payload);
      request$.subscribe({
        next: (resp) => {
          this.saving = false;
          if (!resp.success) {
            this.toast.warning('Personaje del mes', resp.message ?? 'No fue posible guardar el registro.');
            return;
          }
          this.toast.success('Personaje del mes', resp.message ?? 'Registro guardado correctamente.');
          this.limpiarForm();
          this.cargarPersonajes();
          this.cargarEquipos();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error('Personaje del mes', err?.error?.message ?? 'Error guardando el registro.');
        }
      });
    };

    if (this.selectedImageFile) {
      this.uploading = true;
      this.optimizeImageFile(this.selectedImageFile)
        .then((optimizedFile) => {
          if (optimizedFile.size > this.maxImageBytes) {
            this.uploading = false;
            this.toast.warning('Imagen', 'El archivo no puede superar 2MB.');
            return;
          }
          this.personajeMesService.uploadImage(optimizedFile).subscribe({
            next: (resp) => {
              this.uploading = false;
              if (!resp.success) {
                this.toast.warning('Imagen', resp.message ?? 'No fue posible cargar la imagen.');
                return;
              }
              this.formData.FotoModificada = resp.fileName;
              this.fotoPreview = resp.url || this.getImageUrl(resp.fileName);
              this.selectedImageFile = null;
              subirYGuardar(resp.fileName);
            },
            error: (err) => {
              this.uploading = false;
              this.toast.error('Imagen', err?.error?.message ?? 'Error cargando la imagen.');
            }
          });
        })
        .catch(() => {
          this.uploading = false;
          this.toast.warning('Imagen', 'No fue posible procesar la imagen seleccionada.');
        });
    } else {
      subirYGuardar((this.formData.FotoModificada ?? '').trim() || null);
    }
  }

  async eliminar(item: DtoPersonajeMes): Promise<void> {
    const confirmed = await this.alert.confirm(
      'Eliminar registro',
      `¿Está seguro de eliminar el registro de ${this.getNombreCompleto(item)}?`,
      'Eliminar',
      'Cancelar'
    );
    if (!confirmed) return;

    this.personajeMesService.delete(item.idPersonajeMes).subscribe({
      next: (resp) => {
        if (!resp.success) {
          this.toast.warning('Personaje del mes', resp.message ?? 'No fue posible eliminar el registro.');
          return;
        }

        this.toast.success('Personaje del mes', resp.message ?? 'Registro eliminado correctamente.');
        if (this.idEditando === item.idPersonajeMes) {
          this.limpiarForm();
        }
        this.cargarPersonajes();
        this.cargarEquipos();
      },
      error: (err) => {
        this.toast.error('Personaje del mes', err?.error?.message ?? 'Error eliminando el registro.');
      }
    });
  }

  async cambiarEstado(item: DtoPersonajeMes): Promise<void> {
    const nuevoEstado = Number(item.vigente) === 1 ? 0 : 1;
    const accion = nuevoEstado === 1 ? 'activar' : 'inactivar';
    const accionCompleta = nuevoEstado === 1 ? 'activado' : 'inactivado';

    const confirmed = await this.alert.confirm(
      'Confirmar',
      `¿Desea ${accion} el registro de ${this.getNombreCompleto(item)}?`,
      'Aceptar',
      'Cancelar'
    );
    if (!confirmed) return;

    this.personajeMesService.setVigente(item.idPersonajeMes, nuevoEstado).subscribe({
      next: (resp) => {
        if (!resp.success) {
          this.toast.warning('Personaje del mes', resp.message ?? `No fue posible ${accion} el registro.`);
          return;
        }

        this.toast.success('Personaje del mes', resp.message ?? `Registro ${accionCompleta} correctamente.`);
        this.cargarPersonajes();
      },
      error: (err) => {
        this.toast.error('Personaje del mes', err?.error?.message ?? `Error al ${accion} el registro.`);
      }
    });
  }

  selectedImageFile: File | null = null;

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;

    if (!file) {
      return;
    }

    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
    if (!validTypes.includes(file.type)) {
      this.toast.warning('Imagen', 'Solo se permiten imágenes JPG, JPEG, PNG o WEBP.');
      input.value = '';
      return;
    }

    if (file.size > this.maxImageBytes) {
      this.toast.warning('Imagen', 'El archivo no puede superar 2MB.');
      input.value = '';
      return;
    }

    this.selectedImageFile = file;
    this.fotoPreview = URL.createObjectURL(file);
    input.value = '';
    this.toast.success('Imagen', 'Imagen lista para guardar.');
  }

  selectedImageFileEquipo: File | null = null;

  onImageSelectedEquipo(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;

    if (!file) {
      return;
    }

    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
    if (!validTypes.includes(file.type)) {
      this.toast.warning('Imagen', 'Solo se permiten imágenes JPG, JPEG, PNG o WEBP.');
      input.value = '';
      return;
    }

    if (file.size > this.maxImageBytes) {
      this.toast.warning('Imagen', 'El archivo no puede superar 2MB.');
      input.value = '';
      return;
    }

    this.selectedImageFileEquipo = file;
    this.fotoPreviewEquipo = URL.createObjectURL(file);
    input.value = '';
    this.toast.success('Imagen', 'Imagen lista para guardar.');
  }

  private persistirFotoConsultada(fotoBase64: string, identificacion: string): void {
    const file = this.buildImageFileFromBase64(fotoBase64, identificacion);

    if (!file) {
      this.toast.warning('Imagen', 'No fue posible preparar la foto consultada. Puede cargarla manualmente.');
      return;
    }

    // Solo genera el preview y guarda el archivo en memoria, NO sube ni asigna FotoModificada
    this.optimizeImageFile(file)
      .then((optimizedFile) => {
        if (optimizedFile.size > this.maxImageBytes) {
          this.toast.warning('Imagen', 'No fue posible optimizar la foto consultada. Puede cargarla manualmente.');
          return;
        }
        this.selectedImageFile = optimizedFile;
        this.fotoPreview = URL.createObjectURL(optimizedFile);
        this.toast.success('Imagen', 'Imagen lista para guardar.');
      })
      .catch(() => {
        this.toast.warning('Imagen', 'No fue posible optimizar la foto consultada. Puede cargarla manualmente.');
      });
  }

  private buildImageFileFromBase64(source: string, identificacion: string): File | null {
    const raw = (source ?? '').trim();
    if (!raw) {
      return null;
    }

    let mimeType = 'image/jpeg';
    let base64Content = raw;

    const match = raw.match(/^data:(image\/[a-zA-Z0-9.+-]+);base64,([\s\S]+)$/i);
    if (match) {
      mimeType = match[1];
      base64Content = match[2];
    }

    const normalizedBase64 = base64Content
      .trim()
      .replace(/\s+/g, '')
      .replace(/-/g, '+')
      .replace(/_/g, '/')
      .replace(/[^A-Za-z0-9+/=]/g, '');

    if (!normalizedBase64) {
      return null;
    }

    const remainder = normalizedBase64.length % 4;
    const paddedBase64 = remainder === 0
      ? normalizedBase64
      : normalizedBase64.padEnd(normalizedBase64.length + (4 - remainder), '=');

    try {
      const binary = atob(paddedBase64);
      const bytes = new Uint8Array(binary.length);

      for (let index = 0; index < binary.length; index += 1) {
        bytes[index] = binary.charCodeAt(index);
      }

      const safeId = (identificacion || 'personaje-mes').replace(/[^a-zA-Z0-9_-]/g, '');
      const extension = this.getImageExtension(mimeType);
      return new File([bytes], `${safeId || 'personaje-mes'}${extension}`, {
        type: mimeType,
        lastModified: Date.now()
      });
    } catch {
      return null;
    }
  }

  private async optimizeImageFile(file: File): Promise<File> {
    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
    if (!file || !validTypes.includes(file.type)) {
      return file;
    }

    if (file.size <= this.targetOptimizedImageBytes) {
      return file;
    }

    const dataUrl = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        if (typeof reader.result === 'string') {
          resolve(reader.result);
          return;
        }
        reject(new Error('No se pudo leer la imagen.'));
      };
      reader.onerror = () => reject(reader.error ?? new Error('No se pudo leer la imagen.'));
      reader.readAsDataURL(file);
    });

    const image = await new Promise<HTMLImageElement>((resolve, reject) => {
      const img = new Image();
      img.onload = () => resolve(img);
      img.onerror = () => reject(new Error('No se pudo procesar la imagen.'));
      img.src = dataUrl;
    });

    const ratio = Math.min(1, this.maxImageDimension / Math.max(image.width, image.height));
    let width = Math.max(1, Math.round(image.width * ratio));
    let height = Math.max(1, Math.round(image.height * ratio));
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.drawImage(image, 0, 0, width, height);
    }
    const quality = 0.85;
    const mimeType = file.type === 'image/png' ? 'image/png' : 'image/jpeg';
    const blob: Blob = await new Promise((resolve) =>
      canvas.toBlob((b) => resolve(b as Blob), mimeType, quality)
    );
    return new File([blob], file.name, { type: mimeType, lastModified: Date.now() });
  }

  getNombreCompleto(item: DtoPersonajeMes): string {
    return `${item.grado ?? ''} ${item.nombres ?? ''} ${item.apellidos ?? ''}`.replace(/\s+/g, ' ').trim();
  }

  getImageUrl(value: string | null | undefined): string {
    const raw = (value ?? '').trim();
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

  getFotoUrl(item: DtoPersonajeMes): string {
    return this.getImageUrl(item.fotoModificada);
  }

  getMesValue(item: Partial<DtoPersonajeMes> | null | undefined): number {
    return Number(item?.mes ?? item?.Mes ?? 0);
  }

  getAnioValue(item: Partial<DtoPersonajeMes> | null | undefined): number {
    return Number(item?.anio ?? item?.Anio ?? 0);
  }

getMesDescripcion(mes: number | null | undefined): string {
    return this.todosLosMeses.find((item) => item.value === Number(mes))?.label ?? 'Sin mes';
  }

  abrirmodalpersonajemes(): void {
    this.mostrarModalPersonajeMes = true;
  }

  cerrarModalPersonajeMes(): void {
    this.mostrarModalPersonajeMes = false;
    this.limpiarForm();
  }

  abrirmodalequipoaltorendi(): void {
    this.mostrarModalEquipoAltoRendimiento = true;
  }


  buscarFuncionarioEquipo(): void {
    const doc = this.searchIdentificacionEquipo.trim();
    if (!doc) {
      this.toast.warning('Buscar', 'Ingrese un número de identificación');
      return;
    }

    this.searchingFuncionarioEquipo = true;

    this.usuarioAdminService.consultarFuncionarioPersonajeMes(doc).subscribe({
      next: (resp) => {
        const func = resp.funcionario;

        if (!func || !func.nombres) {
          this.searchingFuncionarioEquipo = false;
          this.nombreGrupoEquipo = ''; // Limpiar nombreGrupoEquipo después de guardar
          this.toast.warning('Buscar', 'No se encontró funcionario con esa identificación');
          return;
        }

        const activo = func.activo !== false;
        if (!activo) {
          this.searchingFuncionarioEquipo = false;
          this.toast.warning('Buscar', 'El usuario está inactivo en el sistema');
          return;
        }

        const categoria = this.resolverCategoriaPorFuncionario(
          func.nombreGrado ?? func.cargo ?? '',
          func.categoriaDescripcion ?? undefined
        );

        this.agregarIntegranteEquipo({
          identificacion: (func.identificacion ?? doc).trim(),
          nombres: (func.nombres ?? '').trim(),
          apellidos: (func.apellidos ?? '').trim(),
          grado: ((func.nombreGrado ?? func.cargo ?? '')).trim(),
          cargo: (func.cargo ?? '').trim(),
          unidad: (func.dependencia ?? '').trim(),
          IdCategoria: Number(categoria?.idDominio ?? 0),
          NumeroActa: this.datosFijosEquipo.NumeroActa,
          FotoModificada: this.fotoModificadaEquipo || null,
          Mes: this.datosFijosEquipo.Mes,
          Anio: this.datosFijosEquipo.Anio
        });

        this.searchingFuncionarioEquipo = false;
        this.searchIdentificacionEquipo = '';
        this.toast.success('Buscar', 'Integrante agregado a la lista');
      },
      error: (err) => {
        this.searchingFuncionarioEquipo = false;
        this.toast.error('Buscar', err?.error?.message ?? 'Error al consultar funcionario');
      }
    });
  }

  agregarIntegranteEquipo(data: DtoPersonajeMesRequest): void {
    const existe = this.integrantesEquipo.some(i => i.identificacion === data.identificacion);
    if (existe) {
      this.toast.warning('Equipo', 'Este funcionario ya está en la lista');
      return;
    }
    this.integrantesEquipo.push(data);
  }

  quitarIntegranteEquipo(index: number): void {
    this.integrantesEquipo.splice(index, 1);
  }

}
