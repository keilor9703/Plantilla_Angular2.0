import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LoginVisualPublicItem, LoginVisualService } from '../../core/services/administracion/login-visual.service';
import { BrandingService } from '../../core/services/administracion/branding.service';
import { SetupService } from '../../core/services/setup.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
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

  // Segundo factor (SI2FA)
  mfaPaso = false;
  mfaEnrolar = false;
  mfaChallenge = '';
  mfaQr = '';
  mfaClave = '';
  mfaEnrollToken = '';
  mfaCodigo = '';
  // Restablecimiento (autoservicio)
  mfaReset = false;
  mfaResetCodigo = '';
  infoMessage = '';

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

        // Requiere segundo factor: pasar al paso 2.
        if (resp?.requiere2Fa) {
          this.mfaPaso = true;
          this.mfaEnrolar = !!resp.enrolar;
          this.mfaChallenge = resp.challenge ?? '';
          this.mfaQr = this.toDataUri(resp.qr ?? '');
          this.mfaClave = resp.claveManual ?? '';
          this.mfaEnrollToken = resp.enrollToken ?? '';
          this.mfaCodigo = '';
          this.mfaReset = false;
          this.infoMessage = '';
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

  confirmar2fa(): void {
    this.errorMessage = '';
    const codigo = (this.mfaCodigo ?? '').trim();
    if (!/^\d{6}$/.test(codigo)) {
      this.errorMessage = 'El código debe ser de 6 dígitos.';
      return;
    }

    this.isLoading = true;
    const obs = this.mfaEnrolar
      ? this.authService.mfaEnrollConfirm(this.mfaChallenge, codigo, this.mfaEnrollToken)
      : this.authService.mfaVerify(this.mfaChallenge, codigo);

    obs.subscribe({
      next: (resp: any) => {
        this.isLoading = false;
        if (this.authService.isLoginSuccessful(resp) && resp?.token) {
          this.router.navigate(['/home']);
          return;
        }
        this.errorMessage = resp?.mensaje ?? resp?.message ?? 'No fue posible verificar el código.';
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.mensaje ?? err?.error?.message ?? 'Código inválido o sesión expirada.';
      }
    });
  }

  solicitarReset(): void {
    this.errorMessage = '';
    this.infoMessage = '';
    this.isLoading = true;
    this.authService.mfaResetRequest(this.mfaChallenge).subscribe({
      next: (resp: any) => {
        this.isLoading = false;
        if (resp?.success) {
          this.mfaReset = true;
          this.mfaResetCodigo = '';
          if (resp.challenge) this.mfaChallenge = resp.challenge;
          this.infoMessage = resp?.message ?? 'Se envió un código a su correo institucional.';
        } else {
          this.errorMessage = resp?.message ?? resp?.mensaje ?? 'No fue posible iniciar el restablecimiento.';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.message ?? err?.error?.mensaje ?? 'No fue posible iniciar el restablecimiento.';
      }
    });
  }

  confirmarReset(): void {
    this.errorMessage = '';
    const codigo = (this.mfaResetCodigo ?? '').trim();
    if (!/^\d{6}$/.test(codigo)) {
      this.errorMessage = 'El código debe ser de 6 dígitos.';
      return;
    }
    this.isLoading = true;
    this.authService.mfaResetConfirm(this.mfaChallenge, codigo).subscribe({
      next: (resp: any) => {
        this.isLoading = false;
        if (resp?.requiere2Fa && resp?.enrolar) {
          // Restablecido: pasar a re-enrolar con el nuevo QR.
          this.mfaReset = false;
          this.mfaEnrolar = true;
          this.mfaChallenge = resp.challenge ?? this.mfaChallenge;
          this.mfaQr = this.toDataUri(resp.qr ?? '');
          this.mfaClave = resp.claveManual ?? '';
          this.mfaEnrollToken = resp.enrollToken ?? '';
          this.mfaCodigo = '';
          this.infoMessage = resp?.message ?? 'Segundo factor restablecido. Enrole nuevamente.';
        } else {
          this.errorMessage = resp?.message ?? resp?.mensaje ?? 'No fue posible restablecer el segundo factor.';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.message ?? err?.error?.mensaje ?? 'Código inválido o sesión expirada.';
      }
    });
  }

  cancelar2fa(): void {
    this.mfaPaso = false;
    this.mfaEnrolar = false;
    this.mfaChallenge = '';
    this.mfaQr = '';
    this.mfaClave = '';
    this.mfaEnrollToken = '';
    this.mfaCodigo = '';
    this.mfaReset = false;
    this.mfaResetCodigo = '';
    this.errorMessage = '';
    this.infoMessage = '';
    this.contrasena = '';
  }

  private toDataUri(qr: string): string {
    if (!qr) return '';
    return qr.startsWith('data:') ? qr : `data:image/png;base64,${qr}`;
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }
}

