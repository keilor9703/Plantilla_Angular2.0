import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ToastService } from '../../../core/services/toast.service';
import { DominioService, DtoDominio } from '../../../core/services/administracion/dominio.service';
import { CuentaEmailService, DtoCuentaEmail, DtoCuentaEmailRequest } from '../../../core/services/administracion/cuenta-email.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-cuentas-email',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './cuentas-email.html',
  styleUrls: ['./cuentas-email.scss']
})
export class CuentasEmailComponent implements OnInit {
  loading = false;
  saving = false;
  
  listaCuentas: DtoCuentaEmail[] = [];
  gruposCOEST: DtoDominio[] = [];
  
  editingId: number | null = null;
  form: DtoCuentaEmailRequest = {
    IdDominioGrupo: 0,
    NombreCuenta: '',
    Email: '',
    ServidorSmtp: '',
    Puerto: 587,
    UsuarioSmtp: '',
    ClaveSmtp: '',
    UsarSsl: 1,
    Vigente: 1
  };

  constructor(
    private toast: ToastService,
    private dominioService: DominioService,
    private cuentaEmailService: CuentaEmailService
  ) {}

  ngOnInit(): void {
    this.cargarGrupos();
    this.cargarCuentas();
  }

  cargarGrupos(): void {
    this.dominioService.getAll().subscribe({
      next: (data) => {
        const todos = (data ?? []).map(d => ({
          idDominio: d.idDominio ?? (d as any).IdDominio,
          descripcion: d.descripcion ?? (d as any).Descripcion,
          idPadre: d.idPadre ?? (d as any).IdPadre
        } as DtoDominio));

        // 1. Buscamos el ID de "ORGANIGRAMA COEST"
        const organigrama = todos.find(d => 
          d.descripcion.toUpperCase() === 'ORGANIGRAMA COEST'
        );
        
        if (organigrama) {
          const idOrganigrama = organigrama.idDominio;
          this.gruposCOEST = [];
          // 2. Obtenemos toda la descendencia de forma recursiva
          this.obtenerDescendencia(idOrganigrama, todos, 0);
        }
      }
    });
  }

  private obtenerDescendencia(idPadre: number, todos: DtoDominio[], nivel: number): void {
    const hijos = todos.filter(d => d.idPadre === idPadre);
    
    hijos.forEach(h => {
      // Añadimos el hijo con sangría visual
      const sangria = '--'.repeat(nivel);
      this.gruposCOEST.push({
        ...h,
        descripcion: `${sangria} ${h.descripcion}`
      });
      // Llamada recursiva para buscar los hijos de este hijo
      this.obtenerDescendencia(h.idDominio, todos, nivel + 1);
    });
  }

  cargarCuentas(): void {
    this.loading = true;
    this.cuentaEmailService.getAll()
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (data) => {
          this.listaCuentas = data ?? [];
        },
        error: () => this.toast.error('Error', 'No se pudieron cargar las cuentas de correo')
      });
  }

  nuevo(): void {
    this.editingId = null;
    this.form = {
      IdDominioGrupo: 0,
      NombreCuenta: '',
      Email: '',
      ServidorSmtp: 'smtp.office365.com',
      Puerto: 587,
      UsuarioSmtp: '',
      ClaveSmtp: '',
      UsarSsl: 1,
      Vigente: 1
    };
  }

  editar(item: DtoCuentaEmail): void {
    this.editingId = item.idCuenta;
    this.form = {
      IdDominioGrupo: item.idDominioGrupo,
      NombreCuenta: item.nombreCuenta,
      Email: item.email,
      ServidorSmtp: item.servidorSmtp,
      Puerto: item.puerto,
      UsuarioSmtp: item.usuarioSmtp,
      ClaveSmtp: item.claveSmtp,
      UsarSsl: item.usarSsl,
      Vigente: item.vigente
    };
  }

  guardar(): void {
    if (!this.form.NombreCuenta || !this.form.Email || this.form.IdDominioGrupo === 0) {
      this.toast.warning('Validación', 'Por favor complete los campos obligatorios');
      return;
    }

    this.saving = true;
    if (this.editingId) {
      // Forzar valores de Outlook 365
      this.form.ServidorSmtp = 'smtp.office365.com';
      this.form.Puerto = 587;
      this.form.UsarSsl = 1;
      this.form.UsuarioSmtp = this.form.Email; // Sincronizar usuario con email

      this.cuentaEmailService.update(this.editingId, this.form)
        .pipe(finalize(() => this.saving = false))
        .subscribe({
          next: () => {
            this.toast.success('Éxito', 'Cuenta actualizada correctamente');
            this.nuevo();
            this.cargarCuentas();
          },
          error: () => this.toast.error('Error', 'No se pudo actualizar la cuenta')
        });
    } else {
      // Forzar valores de Outlook 365
      this.form.ServidorSmtp = 'smtp.office365.com';
      this.form.Puerto = 587;
      this.form.UsarSsl = 1;
      this.form.UsuarioSmtp = this.form.Email; // Sincronizar usuario con email

      this.cuentaEmailService.create(this.form)
        .pipe(finalize(() => this.saving = false))
        .subscribe({
          next: () => {
            this.toast.success('Éxito', 'Cuenta creada correctamente');
            this.nuevo();
            this.cargarCuentas();
          },
          error: () => this.toast.error('Error', 'No se pudo crear la cuenta')
        });
    }
  }

  eliminar(item: DtoCuentaEmail): void {
    if (!confirm(`¿Está seguro de eliminar la cuenta "${item.nombreCuenta}"?`)) return;
    
    this.cuentaEmailService.delete(item.idCuenta).subscribe({
      next: () => {
        this.toast.success('Éxito', 'Cuenta eliminada');
        this.cargarCuentas();
      },
      error: () => this.toast.error('Error', 'No se pudo eliminar la cuenta')
    });
  }

  getNombreGrupo(idDominio: number): string {
    const grupo = this.gruposCOEST.find(g => g.idDominio === idDominio);
    if (!grupo) return 'Desconocido';
    // Limpiamos los guiones para mostrar solo el nombre en la tabla
    return grupo.descripcion.replace(/^-+ /, '');
  }
}
