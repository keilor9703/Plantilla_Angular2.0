import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SetupService, SetupTestRequest } from '../../core/services/setup.service';

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './setup.html',
  styleUrls: ['./setup.scss']
})
export class SetupComponent implements OnInit {
  step = 1;
  useRawConnectionString = false;

  unidadesList: any[] = [];
  logoPreview: string | null = null;
  faviconPreview: string | null = null;

  devFuncionario: any = null;

  formData: SetupTestRequest = {
    provider: 'Oracle',
    host: '',
    port: 1538,
    databaseOrService: '',
    user: '',
    password: '',
    connectionStringRaw: '',
    pipUser: '',
    pipPassword: '',
    jwtKey: '',
    jwtIssuer: 'oftic.api',
    jwtAudience: 'oftic.api',
    sistema: 'SISGE',
    nombreSistema: 'Sistema de Gestión Institucional',
    sigla: 'POLICIA',
    nombreUnidad: '',
    logoFileName: '',
    faviconFileName: '',
    devUser: '',
    devPassword: '',
    devIdentificacion: '80100216'
  };

  testing = false;
  testResult: { success: boolean; message: string; requiresMigration?: boolean } | null = null;
  saving = false;
  saveResult: { success: boolean; message: string } | null = null;

  showMigrationModal = false;
  migrating = false;
  migrationResult: { success: boolean; message: string } | null = null;

  constructor(
    private setupService: SetupService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.checkStatus();
    this.loadUnidades();
  }

  checkStatus(): void {
    this.setupService.getStatus().subscribe({
      next: (res) => {
        if (res.configured) {
          // Si ya está configurado, continuar al login
        }
      },
      error: () => {}
    });
  }

  loadUnidades(): void {
    this.setupService.getUnidades().subscribe({
      next: (res) => {
        if (Array.isArray(res)) {
          this.unidadesList = res;
        }
      },
      error: () => {}
    });
  }

  onUnidadChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const selectedId = Number(target.value);
    this.formData.unidadSistemaId = selectedId;
    const found = this.unidadesList.find(u => u.consecutivo === selectedId || u.idOrganigrama === selectedId || u.id === selectedId);
    if (found) {
      this.formData.nombreUnidad = found.nombre || found.descripcionDependencia || found.nombreUnidad || found.sigla || '';
      if (found.sigla && (!this.formData.sigla || this.formData.sigla === 'POLICIA')) {
        this.formData.sigla = found.sigla;
      }
    }
  }

  onLogoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => (this.logoPreview = reader.result as string);
    reader.readAsDataURL(file);

    this.setupService.uploadLogo(file).subscribe({
      next: (res) => {
        if (res?.fileName) {
          this.formData.logoFileName = res.fileName;
        }
      },
      error: (err) => console.error('Error subiendo logo:', err)
    });
  }

  onFaviconSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => (this.faviconPreview = reader.result as string);
    reader.readAsDataURL(file);

    this.setupService.uploadFavicon(file).subscribe({
      next: (res) => {
        if (res?.fileName) {
          this.formData.faviconFileName = res.fileName;
        }
      },
      error: (err) => console.error('Error subiendo favicon:', err)
    });
  }

  selectProvider(provider: string): void {
    this.formData.provider = provider;
    if (provider === 'Oracle') {
      this.formData.port = 1538;
    } else if (provider === 'PostgreSQL') {
      this.formData.port = 5432;
    } else if (provider === 'MySQL') {
      this.formData.port = 3306;
    }
  }

  generateRandomJwtKey(): void {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=';
    let key = '';
    const array = new Uint8Array(64);
    crypto.getRandomValues(array);
    for (let i = 0; i < array.length; i++) {
      key += chars[array[i] % chars.length];
    }
    this.formData.jwtKey = key;
  }

  nextStep(): void {
    if (this.step === 2) {
      // Validar conexión a la BD
      this.testing = true;
      this.testResult = null;

      const payload = this.preparePayload();
      this.setupService.testConnection(payload).subscribe({
        next: (res) => {
          this.testing = false;
          this.testResult = res;
          if (res.success) {
            if (res.requiresMigration) {
              this.showMigrationModal = true;
            } else {
              this.step++;
            }
          }
        },
        error: (err) => {
          this.testing = false;
          this.testResult = {
            success: false,
            message: err?.error?.message || 'Error al conectar con la base de datos.'
          };
        }
      });
      return;
    }

    if (this.step === 3) {
      if (!this.formData.pipUser?.trim() || !this.formData.pipPassword?.trim()) {
        this.testResult = {
          success: false,
          message: 'Debe ingresar el Usuario PIP y la Clave PIP.'
        };
        return;
      }

      this.testing = true;
      this.testResult = null;

      const payload = this.preparePayload();
      this.setupService.testPipCredentials(payload).subscribe({
        next: (res) => {
          this.testing = false;
          this.testResult = res;
          if (res.success) {
            this.step++;
          }
        },
        error: (err) => {
          this.testing = false;
          this.testResult = {
            success: false,
            message: err?.error?.message || 'Error al validar las credenciales PIP/OUD.'
          };
        }
      });
      return;
    }

    if (this.step === 4) {
      if (!this.formData.jwtKey?.trim()) {
        this.generateRandomJwtKey();
      }
    }

    if (this.step === 5) {
      const ident = (this.formData.devIdentificacion || this.formData.devUser || '').trim();
      if (!ident) {
        this.testResult = {
          success: false,
          message: 'Debe ingresar el número de cédula/identificación del Desarrollador.'
        };
        return;
      }

      this.formData.devIdentificacion = ident;
      this.testing = true;
      this.testResult = null;

      const payload = this.preparePayload();
      this.setupService.validateDevUser(payload).subscribe({
        next: (res: any) => {
          this.testing = false;
          this.testResult = res;
          if (res.success) {
            if (res.funcionario) {
              this.devFuncionario = res.funcionario;
              this.formData.devUser = res.funcionario.usuario || res.funcionario.usuarioEmpresarial || this.formData.devUser;
              this.formData.devNombres = res.funcionario.nombres || this.formData.devNombres;
              this.formData.devApellidos = res.funcionario.apellidos || this.formData.devApellidos;
              this.formData.devEmail = res.funcionario.correo || res.funcionario.correoElectronico || this.formData.devEmail;
              this.formData.devGrad = res.funcionario.gradAlfabetico || res.funcionario.nombreGrado || this.formData.devGrad;
            }
            this.step++;
          }
        },
        error: (err) => {
          this.testing = false;
          this.testResult = {
            success: false,
            message: err?.error?.message || 'Error al consultar la identificación del Desarrollador en PIP.'
          };
        }
      });
      return;
    }

    if (this.step < 6) {
      this.step++;
    }
  }

  prevStep(): void {
    if (this.step > 1) {
      this.step--;
    }
  }

  onTestConnection(): void {
    this.testing = true;
    this.testResult = null;

    const payload = this.preparePayload();
    this.setupService.testConnection(payload).subscribe({
      next: (res) => {
        this.testing = false;
        this.testResult = res;
        if (res.success && res.requiresMigration) {
          this.showMigrationModal = true;
        }
      },
      error: (err) => {
        this.testing = false;
        this.testResult = {
          success: false,
          message: err?.error?.message || 'No se pudo contactar al servidor para la prueba.'
        };
      }
    });
  }

  closeMigrationModal(): void {
    this.showMigrationModal = false;
  }

  runMigration(): void {
    this.migrating = true;
    this.migrationResult = null;

    const payload = this.preparePayload();
    this.setupService.migrateDatabase(payload).subscribe({
      next: (res) => {
        this.migrating = false;
        this.migrationResult = res;
        if (res.success) {
          setTimeout(() => {
            this.showMigrationModal = false;
            this.step++;
          }, 1500);
        }
      },
      error: (err) => {
        this.migrating = false;
        this.migrationResult = {
          success: false,
          message: err?.error?.message || 'Error al ejecutar la migración del esquema.'
        };
      }
    });
  }

  onSaveConfig(): void {
    this.saving = true;
    this.saveResult = null;

    const payload = this.preparePayload();
    this.setupService.saveConfig(payload).subscribe({
      next: (res) => {
        this.saving = false;
        this.saveResult = res;
        if (res.success) {
          setTimeout(() => {
            this.router.navigate(['/login']);
          }, 2000);
        }
      },
      error: (err) => {
        this.saving = false;
        this.saveResult = {
          success: false,
          message: err?.error?.message || 'Error guardando la configuración.'
        };
      }
    });
  }

  private preparePayload(): SetupTestRequest {
    return {
      provider: this.formData.provider,
      host: this.formData.host,
      port: this.formData.port,
      databaseOrService: this.formData.databaseOrService,
      user: this.formData.user,
      password: this.formData.password,
      connectionStringRaw: this.useRawConnectionString ? this.formData.connectionStringRaw : undefined,
      pipUser: this.formData.pipUser,
      pipPassword: this.formData.pipPassword,
      jwtKey: this.formData.jwtKey,
      jwtIssuer: this.formData.jwtIssuer,
      jwtAudience: this.formData.jwtAudience,
      devUser: this.formData.devUser,
      devPassword: this.formData.devPassword,
      devIdentificacion: this.formData.devIdentificacion,
      devNombres: this.formData.devNombres,
      devApellidos: this.formData.devApellidos,
      devEmail: this.formData.devEmail,
      devGrad: this.formData.devGrad,
      sistema: this.formData.sistema,
      nombreSistema: this.formData.nombreSistema,
      sigla: this.formData.sigla,
      nombreUnidad: this.formData.nombreUnidad,
      unidadSistemaId: this.formData.unidadSistemaId,
      logoFileName: this.formData.logoFileName,
      faviconFileName: this.formData.faviconFileName
    };
  }
}
