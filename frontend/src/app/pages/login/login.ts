import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LoginVisualPublicItem, LoginVisualService } from '../../core/services/administracion/login-visual.service';
import { BrandingService } from '../../core/services/administracion/branding.service';
import { SetupService } from '../../core/services/setup.service';
import { MfaLoginChallenge, PoliciaMfaFlowComponent } from '@policia/mfa';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, PoliciaMfaFlowComponent],
  templateUrl: './login.html',
  styleUrls: ['./login.scss']
})
export class LoginComponent implements OnInit, OnDestroy {
  systemName = 'OFTIC';
  logoUrl = '/escudo.png';
  usuario = '';
  contrasena = '';
  showPassword = false;
  isLoading = false;
  errorMessage = '';
  slides: LoginVisualPublicItem[] = [];
  currentSlideIndex = 0;
  private loginTimeoutHandle: any = null;
  private slideTimerHandle: any = null;

  // Reto de doble autenticación (estándar @policia/mfa). Al asignarse, <pmfa-flow> abre la modal.
  mfaChallenge: MfaLoginChallenge | null = null;

  constructor(
    private authService: AuthService,
    private router: Router,
    private loginVisualService: LoginVisualService,
    private brandingService: BrandingService,
    private setupService: SetupService
  ) {}

  ngOnInit(): void {
    this.checkSetup();
    this.loadBranding();
    this.loadLoginBackgrounds();
  }

  private checkSetup(): void {
    this.setupService.getStatus().subscribe({
      next: (res) => {
        if (!res.configured) {
          this.router.navigate(['/setup']);
        }
      },
      error: () => {}
    });
  }

  ngOnDestroy(): void {
    if (this.loginTimeoutHandle) {
      clearTimeout(this.loginTimeoutHandle);
    }
    if (this.slideTimerHandle) {
      clearInterval(this.slideTimerHandle);
    }
  }

  loadLoginBackgrounds(): void {
    this.loginVisualService.getPublicConfig().subscribe({
      next: (config) => {
        const items = (config?.items ?? [])
          .filter((x) => !!x?.url)
          .sort((a, b) => (a.order ?? 0) - (b.order ?? 0));

        if (items.length === 0) {
          this.applyFallbackSlides();
          return;
        }

        this.slides = items;
        this.currentSlideIndex = 0;
        this.startSlideRotation(Number(config?.intervalMs ?? 6000));
      },
      error: () => {
        this.applyFallbackSlides();
      }
    });
  }

  private loadBranding(): void {
    this.brandingService.getPublicConfig().subscribe({
      next: (cfg) => {
        const sigla = (cfg?.sistema ?? cfg?.sigla ?? cfg?.systemName ?? '').trim();
        this.systemName = sigla || 'OFTIC';
        this.logoUrl = (cfg?.logoUrl ?? '').trim() || '/escudo.png';
      },
      error: () => {
        this.systemName = 'OFTIC';
        this.logoUrl = '/escudo.png';
      }
    });
  }

  private applyFallbackSlides(): void {
    this.slides = [
      { fileName: 'banner1.jpg', url: '/background/banner1.jpg', order: 1 },
      { fileName: 'banner2.jpg', url: '/background/banner2.jpg', order: 2 },
      { fileName: 'banner3.jpg', url: '/background/banner3.jpg', order: 3 }
    ];
    this.currentSlideIndex = 0;
    this.startSlideRotation(6000);
  }

  private startSlideRotation(intervalMs: number): void {
    if (this.slideTimerHandle) {
      clearInterval(this.slideTimerHandle);
      this.slideTimerHandle = null;
    }

    if (!this.slides || this.slides.length <= 1) {
      return;
    }

    const safeInterval = Number.isFinite(intervalMs) && intervalMs >= 1500 ? intervalMs : 6000;
    this.slideTimerHandle = setInterval(() => {
      this.currentSlideIndex = (this.currentSlideIndex + 1) % this.slides.length;
    }, safeInterval);
  }

  onSubmit(): void {
    this.errorMessage = '';

    if (!this.usuario?.trim() || !this.contrasena?.trim()) {
      this.errorMessage = 'Debe diligenciar usuario y contraseña.';
      return;
    }

    this.isLoading = true;

    this.loginTimeoutHandle = setTimeout(() => {
      if (this.isLoading) {
        this.isLoading = false;
        this.errorMessage = 'La autenticación está tardando demasiado. Intente nuevamente.';
      }
    }, 35000);

    this.authService.login(this.usuario.trim(), this.contrasena).subscribe({
      next: (resp: any) => {
        clearTimeout(this.loginTimeoutHandle);
        this.isLoading = false;

        // Requiere segundo factor: el componente <pmfa-flow> se encarga de las modales.
        if (resp?.requiresMfa) {
          this.mfaChallenge = resp as MfaLoginChallenge;
          return;
        }

        if (this.authService.isLoginSuccessful(resp)) {
          this.router.navigate(['/home']);
          return;
        }

        this.errorMessage = resp?.mensaje ?? resp?.message ?? 'No fue posible iniciar sesión.';
      },
      error: (err) => {
        clearTimeout(this.loginTimeoutHandle);
        this.isLoading = false;
        this.errorMessage = err?.error?.mensaje ?? err?.error?.message ?? 'Error de conexión con el servicio de autenticación.';
      }
    });
  }

  /** El componente de 2FA emite el JWT definitivo. */
  onMfaAutenticado(token: string): void {
    this.authService.storeLoginData(token, this.usuario.trim());
    this.mfaChallenge = null;
    this.router.navigate(['/home']);
  }

  /** El usuario cerró/canceló el flujo de 2FA. */
  onMfaCancelado(): void {
    this.mfaChallenge = null;
    this.contrasena = '';
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }
}
