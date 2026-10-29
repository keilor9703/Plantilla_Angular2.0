import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { AuthService } from '../../core/auth/auth.service';
import { LoginVisualPublicItem, LoginVisualService } from '../../core/services/administracion/login-visual.service';
import { BrandingService } from '../../core/services/administracion/branding.service';
import { SetupService } from '../../core/services/setup.service';
// Lógica estándar institucional de 2FA (@policia/mfa). La UI se renderiza con
// el diseño propio del login (glass-card) para mantener la coherencia visual.
import { MfaLoginChallenge, MfaStepResponse, MfaService } from '@policia/mfa';

type MfaVista = 'verify' | 'enroll' | 'blocked' | 'svcdown' | null;

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

  // ───────────── Estado del flujo 2FA (diseño login + lógica @policia/mfa) ─────────────
  mfaChallenge: MfaLoginChallenge | null = null;
  mfaVista: MfaVista = null;
  private mfaSessionToken = '';
  mfaQrBase64 = '';
  mfaManualKey = '';
  private mfaEnrollToken = '';
  mfaOtpauthUri: SafeUrl | null = null;
  mfaBloqueoHasta = '';
  mfaSvcMsg = '';
  otpDigits: string[] = ['', '', '', '', '', ''];
  mfaRememberDevice = false;
  mfaLoading = false;
  mfaError = '';
  mfaInfo = '';
  // 0 = verificar/enrolar normal; 1 = pedir código de correo; 2 = ingresar código de correo
  resetStep = 0;

  constructor(
    private authService: AuthService,
    private router: Router,
    private loginVisualService: LoginVisualService,
    private brandingService: BrandingService,
    private setupService: SetupService,
    private mfaService: MfaService,
    private sanitizer: DomSanitizer
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

        // Requiere segundo factor: se abre el flujo 2FA con el diseño del login.
        if (resp?.requiresMfa) {
          this.iniciarMfa(resp as MfaLoginChallenge);
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

  // ───────────────────────────── Flujo 2FA ─────────────────────────────

  private iniciarMfa(ch: MfaLoginChallenge): void {
    this.mfaChallenge = ch;
    this.mfaSessionToken = ch.mfaSessionToken ?? '';
    this.mfaQrBase64 = ch.mfaQrBase64 ?? '';
    this.mfaManualKey = ch.mfaManualKey ?? '';
    this.mfaEnrollToken = ch.mfaEnrollToken ?? '';
    this.mfaBloqueoHasta = ch.bloqueoHasta ?? '';
    this.mfaSvcMsg = ch.message ?? '';
    this.mfaError = '';
    this.mfaInfo = '';
    this.resetStep = 0;
    this.mfaRememberDevice = false;
    this.clearOtp();
    this.construirOtpauth();

    switch (ch.mfaMode) {
      case 'enroll':  this.mfaVista = 'enroll';  break;
      case 'blocked': this.mfaVista = 'blocked'; break;
      case 'svcdown': this.mfaVista = 'svcdown'; break;
      default:        this.mfaVista = 'verify';
    }
  }

  private construirOtpauthUri(): void {
    if (!this.mfaManualKey) { this.mfaOtpauthUri = null; return; }
    const issuer = 'SI2FA';
    const label = encodeURIComponent(`${issuer}:${this.usuario || 'usuario'}`);
    const uri = `otpauth://totp/${label}?secret=${encodeURIComponent(this.mfaManualKey)}&issuer=${encodeURIComponent(issuer)}&algorithm=SHA1&digits=6&period=30`;
    this.mfaOtpauthUri = this.sanitizer.bypassSecurityTrustUrl(uri);
  }
  // Alias interno usado en iniciarMfa (compat).
  private construirOtpauth(): void { this.construirOtpauthUri(); }

  cancelarMfa(): void {
    this.mfaChallenge = null;
    this.mfaVista = null;
    this.contrasena = '';
    this.clearOtp();
    this.mfaError = '';
    this.mfaInfo = '';
    this.resetStep = 0;
  }

  get otpCode(): string { return this.otpDigits.join(''); }

  clearOtp(): void {
    this.otpDigits = ['', '', '', '', '', ''];
  }

  onOtpInput(idx: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const val = (input.value || '').replace(/\D/g, '');
    this.otpDigits[idx] = val.slice(-1);
    input.value = this.otpDigits[idx];
    if (this.otpDigits[idx] && idx < 5) {
      this.focusOtp(idx + 1);
    }
  }

  onOtpKeydown(idx: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace' && !this.otpDigits[idx] && idx > 0) {
      this.focusOtp(idx - 1);
    }
  }

  onOtpPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const text = (event.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 6);
    if (!text) { return; }
    for (let i = 0; i < 6; i++) {
      this.otpDigits[i] = text[i] ?? '';
    }
    this.focusOtp(Math.min(text.length, 5));
  }

  private focusOtp(idx: number): void {
    setTimeout(() => {
      const el = document.getElementById('otp-' + idx) as HTMLInputElement | null;
      el?.focus();
      el?.select();
    }, 0);
  }

  submitVerify(): void {
    if (this.otpCode.length !== 6) { this.mfaError = 'Ingrese los 6 dígitos del código.'; return; }
    this.mfaLoading = true;
    this.mfaError = '';
    this.mfaService.verify(this.mfaSessionToken, this.otpCode, this.mfaRememberDevice)
      .subscribe({ next: (r) => this.handleStep(r), error: () => this.onMfaHttpError() });
  }

  submitEnroll(): void {
    if (this.otpCode.length !== 6) { this.mfaError = 'Ingrese los 6 dígitos del código.'; return; }
    this.mfaLoading = true;
    this.mfaError = '';
    this.mfaService.enrollConfirm(this.mfaSessionToken, this.otpCode, this.mfaEnrollToken)
      .subscribe({ next: (r) => this.handleStep(r), error: () => this.onMfaHttpError() });
  }

  /** Paso 1 del reset: solicitar código al correo institucional. */
  submitResetRequest(): void {
    this.mfaLoading = true;
    this.mfaError = '';
    this.mfaInfo = '';
    this.mfaService.resetRequest(this.mfaSessionToken)
      .subscribe({ next: (r) => this.handleStep(r), error: () => this.onMfaHttpError() });
  }

  /** Paso 2 del reset: confirmar el código de correo (reinicia el 2FA → nuevo QR). */
  submitResetConfirm(): void {
    if (this.otpCode.length !== 6) { this.mfaError = 'Ingrese el código de 6 dígitos enviado a su correo.'; return; }
    this.mfaLoading = true;
    this.mfaError = '';
    this.mfaService.resetConfirm(this.mfaSessionToken, this.otpCode)
      .subscribe({ next: (r) => this.handleStep(r), error: () => this.onMfaHttpError() });
  }

  /** Abre el paso de restablecimiento (pide confirmación de envío de código). */
  irAReset(): void {
    this.resetStep = 1;
    this.mfaError = '';
    this.mfaInfo = '';
    this.clearOtp();
  }

  volverAVerify(): void {
    this.resetStep = 0;
    this.mfaVista = 'verify';
    this.mfaError = '';
    this.mfaInfo = '';
    this.clearOtp();
  }

  private handleStep(r: MfaStepResponse): void {
    this.mfaLoading = false;

    // 2FA completo → JWT definitivo.
    if (r.success && r.token) {
      this.authService.storeLoginData(r.token, this.usuario.trim());
      this.mfaChallenge = null;
      this.mfaVista = null;
      this.router.navigate(['/home']);
      return;
    }

    // Tras un reset exitoso llega un nuevo QR para re-enrolar.
    if (r.qrBase64) {
      this.mfaQrBase64 = r.qrBase64;
      this.mfaManualKey = r.manualKey ?? '';
      this.mfaEnrollToken = r.enrollToken ?? '';
      this.construirOtpauthUri();
      this.resetStep = 0;
      this.mfaVista = 'enroll';
      this.mfaInfo = r.message || '2FA restablecido. Vuelva a registrar su aplicación.';
      this.clearOtp();
      return;
    }

    // Mensaje informativo (p. ej. "código enviado a su correo").
    if (r.isInfo) {
      this.mfaInfo = r.message || 'Operación realizada.';
      if (this.resetStep === 1) {
        this.resetStep = 2;
        this.clearOtp();
      }
      return;
    }

    // Bloqueo por intentos fallidos.
    if (r.bloqueoHasta) {
      this.mfaBloqueoHasta = r.bloqueoHasta;
      this.mfaVista = 'blocked';
      return;
    }

    // Error normal (código incorrecto, etc.).
    this.mfaError = r.message || 'No fue posible validar el código.';
    this.clearOtp();
    this.focusOtp(0);
  }

  private onMfaHttpError(): void {
    this.mfaLoading = false;
    this.mfaError = 'No fue posible contactar el servicio de doble autenticación. Intente nuevamente.';
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }
}
