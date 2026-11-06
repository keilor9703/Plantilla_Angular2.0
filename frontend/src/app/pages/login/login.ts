import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { AuthService } from '../../core/auth/auth.service';
import { LoginVisualPublicItem, LoginVisualService } from '../../core/services/administracion/login-visual.service';
import { BrandingService } from '../../core/services/administracion/branding.service';
import { SetupService } from '../../core/services/setup.service';
// Lógica estándar institucional de 2FA (@policia/mfa). La UI se renderiza con
// el diseño propio del login (glass-card) para mantener la coherencia visual.
import { MfaLoginChallenge, MfaStepResponse, MfaService } from '@policia/mfa';

type MfaVista = 'verify' | 'enroll' | 'blocked' | 'svcdown' | null;
type PushEstado = 'ninguno' | 'enviando' | 'esperando' | 'denegado' | 'vencido';

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

  // ───────────── Notificación push a la app (por defecto al llegar al paso del 2FA) ─────────────
  pushEstado: PushEstado = 'ninguno';
  pushPairing = '';
  pushRestante = 0;
  private pushSid = '';
  private pushWebToken = '';
  private pushExpira = 0;
  private pushTimer: any = null;
  mfaError = '';
  mfaInfo = '';
  // 0 = verificar/enrolar normal; 1 = pedir código de correo; 2 = ingresar código de correo
  resetStep = 0;

  // ───────────── Estado del login por QR (passwordless, opción primaria) ─────────────
  qrVisible = false;          // la sección del QR está disponible/mostrada
  qrDisponible = true;        // el servicio de QR respondió (si no, no se ofrece "volver al QR")
  showCredenciales = false;   // el formulario de usuario/contraseña está desplegado (oculta el QR)
  qrPng = '';
  qrPairing = '';
  qrError = '';
  qrInfo = 'Abra la app Autenticador 2FA y escanee el código.';
  qrCargando = false;
  private qrSid = '';
  private qrWebToken = '';
  private qrPollHandle: any = null;

  constructor(
    private authService: AuthService,
    private router: Router,
    private loginVisualService: LoginVisualService,
    private brandingService: BrandingService,
    private setupService: SetupService,
    private mfaService: MfaService,
    private sanitizer: DomSanitizer,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.checkSetup();
    this.loadBranding();
    this.loadLoginBackgrounds();
    // El QR es la opción primaria: se genera de inmediato, listo para escanear.
    this.cargarQr();
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
    this.detenerPush();
    if (this.loginTimeoutHandle) {
      clearTimeout(this.loginTimeoutHandle);
    }
    if (this.slideTimerHandle) {
      clearInterval(this.slideTimerHandle);
    }
    this.detenerQrPoll();
  }

  // ───────────────────────────── Login por QR ─────────────────────────────

  /** Genera el QR (opción primaria). Si el servicio no está disponible, muestra el login por contraseña. */
  cargarQr(): void {
    this.qrError = '';
    this.qrPng = '';
    this.qrCargando = true;

    this.authService.qrCreate().subscribe({
      next: (resp: any) => {
        this.qrCargando = false;
        if (!resp?.success) { this.qrNoDisponible(); return; }
        this.qrVisible = true;
        this.qrSid = resp.sid;
        this.qrWebToken = resp.webPollToken;
        this.qrPng = resp.qrPng ? 'data:image/png;base64,' + resp.qrPng : '';
        this.qrPairing = resp.pairing ?? '';
        this.qrInfo = 'Abra la app Autenticador 2FA y escanee el código.';
        this.iniciarQrPoll();
      },
      error: () => {
        this.qrCargando = false;
        this.qrNoDisponible();
      }
    });
  }

  /** El QR no está disponible (servicio apagado/caído): se deja a la mano el login por contraseña. */
  private qrNoDisponible(): void {
    this.detenerQrPoll();
    this.qrVisible = false;
    this.qrDisponible = false;
    this.qrPng = '';
    this.showCredenciales = true;
  }

  /** Cambia a la vista de credenciales (oculta el QR). Útil si abre desde el mismo celular. */
  mostrarCredenciales(): void {
    this.showCredenciales = true;
    this.detenerQrPoll();   // no se consulta el QR mientras se usa la otra opción
  }

  /** Vuelve a la vista del QR (oculta las credenciales). Regenera el QR si ya no es válido. */
  volverAQr(): void {
    this.showCredenciales = false;
    this.errorMessage = '';
    if (!this.qrPng || this.qrError) {
      this.cargarQr();
    } else {
      this.iniciarQrPoll();
    }
  }

  private iniciarQrPoll(): void {
    this.detenerQrPoll();
    this.qrPollHandle = setInterval(() => this.consultarQr(), 1800);
  }

  private detenerQrPoll(): void {
    if (this.qrPollHandle) {
      clearInterval(this.qrPollHandle);
      this.qrPollHandle = null;
    }
  }

  private consultarQr(): void {
    if (!this.qrSid || !this.qrWebToken) return;
    this.authService.qrStatus(this.qrSid, this.qrWebToken).subscribe({
      next: (resp: any) => {
        const estado = resp?.estado;
        if (resp?.success && estado === 'APPROVED' && resp?.token) {
          this.detenerQrPoll();
          this.authService.storeLoginData(resp.token, resp.usuario ?? this.usuario);
          this.router.navigate(['/home']);
          return;
        }
        if (estado === 'SCANNED') {
          this.qrInfo = 'Código escaneado. Confirme el ingreso en su dispositivo.';
        }
        if (estado === 'EXPIRED') {
          this.detenerQrPoll();
          this.qrError = 'El código expiró. Genere uno nuevo.';
          this.qrPng = '';
        }
        if (estado === 'REJECTED') {
          this.detenerQrPoll();
          this.qrError = 'El ingreso fue rechazado desde el dispositivo.';
          this.qrPng = '';
        }
        if (estado === 'SVCDOWN' || estado === 'ERROR') {
          this.detenerQrPoll();
          this.qrError = resp?.message ?? 'No fue posible completar el ingreso.';
          this.qrPng = '';
        }
      },
      error: () => { /* reintenta en el siguiente tick */ }
    });
  }

  reintentarQr(): void {
    this.cargarQr();
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
    // El cursor queda listo en la primera casilla: se puede escribir sin hacer clic.
    if (this.mfaVista === 'verify' || this.mfaVista === 'enroll') { this.focusOtp(0); }
    // Por defecto se envía la notificación a la app; si no llega, se puede escribir el código TOTP.
    this.detenerPush();
    if (this.mfaVista === 'verify') { this.iniciarPush(); }
  }

  /** Envía la notificación de ingreso y empieza a consultar la respuesta del funcionario. */
  iniciarPush(): void {
    this.detenerPush();
    this.pushEstado = 'enviando';
    this.http.post<any>(`${environment.apiBaseUrl}/Cuenta/MfaPushStart`, { mfaSessionToken: this.mfaSessionToken }).subscribe({
      next: (r) => {
        if (!r?.success) { this.pushEstado = 'ninguno'; return; }   // sin celular / límite → solo TOTP
        this.pushSid = r.sid;
        this.pushWebToken = r.webPollToken;
        this.pushPairing = r.pairing;
        this.pushExpira = Date.now() + (Number(r.expiraSeg) || 300) * 1000;
        this.pushRestante = Math.max(0, Math.round((this.pushExpira - Date.now()) / 1000));
        this.pushEstado = 'esperando';
        this.pushTimer = setInterval(() => this.consultarPush(), 2000);
      },
      error: () => { this.pushEstado = 'ninguno'; }
    });
  }

  private consultarPush(): void {
    if (this.pushEstado !== 'esperando') { return; }
    this.pushRestante = Math.max(0, Math.round((this.pushExpira - Date.now()) / 1000));
    this.http.post<any>(`${environment.apiBaseUrl}/Cuenta/MfaPushStatus`, {
      mfaSessionToken: this.mfaSessionToken, sid: this.pushSid, webPollToken: this.pushWebToken
    }).subscribe({
      next: (r) => {
        if (this.pushEstado !== 'esperando') { return; }
        switch (r?.estado) {
          case 'APPROVED':
            this.detenerPush();
            this.handleStep({ success: true, token: r.token } as MfaStepResponse);
            break;
          case 'REJECTED': this.detenerPush(); this.pushEstado = 'denegado'; break;
          case 'EXPIRED':  this.detenerPush(); this.pushEstado = 'vencido'; break;
          case 'ERROR':    this.detenerPush(); this.pushEstado = 'ninguno'; break;
          default: if (this.pushRestante <= 0) { this.detenerPush(); this.pushEstado = 'vencido'; }
        }
      },
      error: () => { /* red intermitente: se reintenta en el siguiente ciclo */ }
    });
  }

  private detenerPush(): void {
    if (this.pushTimer) { clearInterval(this.pushTimer); this.pushTimer = null; }
    if (this.pushEstado === 'esperando' || this.pushEstado === 'enviando') { this.pushEstado = 'ninguno'; }
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
    this.detenerPush();
    this.pushEstado = 'ninguno';
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
    this.otpUltimoEnviado = '';
  }

  /** Las casillas se identifican por posición (no por valor): así el DOM y el foco no se recrean al escribir. */
  trackOtp = (index: number): number => index;

  /** Cantidad de dígitos del código. */
  private readonly otpLen = 6;
  /** Evita enviar dos veces el mismo código (p. ej. pegar + escribir). */
  private otpUltimoEnviado = '';

  /** Entrada de respaldo (teclados móviles, autocompletado): reparte lo escrito desde la casilla actual. */
  onOtpInput(idx: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const digitos = (input.value || '').replace(/\D/g, '');
    if (!digitos) {
      this.otpDigits[idx] = '';
      input.value = '';
      return;
    }
    // Si llegan varios dígitos juntos (autocompletado), se reparten en las casillas siguientes.
    let pos = idx;
    for (const d of digitos) {
      if (pos >= this.otpLen) { break; }
      this.otpDigits[pos++] = d;
    }
    input.value = this.otpDigits[idx];
    this.trasEscribir(pos);
  }

  onOtpKeydown(idx: number, event: KeyboardEvent): void {
    if (this.mfaLoading) { event.preventDefault(); return; }
    const k = event.key;

    // Dígito: se escribe (reemplazando lo que hubiera) y se avanza.
    if (/^\d$/.test(k) && !event.ctrlKey && !event.metaKey && !event.altKey) {
      event.preventDefault();
      this.otpDigits[idx] = k;
      this.trasEscribir(idx + 1);
      return;
    }

    if (k === 'Backspace') {
      event.preventDefault();
      if (this.otpDigits[idx]) {
        // 1.er borrar: vacía la casilla actual y se queda en ella.
        this.otpDigits[idx] = '';
      } else if (idx > 0) {
        // 2.º borrar (casilla ya vacía): pasa a la anterior, con su dígito seleccionado.
        this.focusOtp(idx - 1);
      }
      this.mfaError = '';
      return;
    }

    if (k === 'Delete') {
      event.preventDefault();
      this.otpDigits[idx] = '';
      return;
    }
    if (k === 'ArrowLeft' && idx > 0) { event.preventDefault(); this.focusOtp(idx - 1); return; }
    if (k === 'ArrowRight' && idx < this.otpLen - 1) { event.preventDefault(); this.focusOtp(idx + 1); return; }
    if (k === 'Enter') { event.preventDefault(); this.enviarSiCompleto(true); return; }
    // Cualquier otra tecla imprimible (letras, símbolos) se ignora; Tab y atajos siguen funcionando.
    if (k.length === 1 && !event.ctrlKey && !event.metaKey) { event.preventDefault(); }
  }

  onOtpPaste(event: ClipboardEvent): void {
    event.preventDefault();
    if (this.mfaLoading) { return; }
    const text = (event.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, this.otpLen);
    if (!text) { return; }
    for (let i = 0; i < this.otpLen; i++) {
      this.otpDigits[i] = text[i] ?? '';
    }
    this.trasEscribir(text.length);
  }

  /** Mueve el cursor a la siguiente casilla (o envía si ya está completo). */
  private trasEscribir(siguiente: number): void {
    this.mfaError = '';
    if (siguiente < this.otpLen) {
      this.focusOtp(siguiente);
    } else {
      this.focusOtp(this.otpLen - 1);
      this.enviarSiCompleto(false);
    }
  }

  /** Verifica automáticamente cuando el código está completo, según el paso actual. */
  private enviarSiCompleto(forzar: boolean): void {
    const codigo = this.otpCode;
    if (this.mfaLoading || codigo.length !== this.otpLen) { return; }
    if (!forzar && codigo === this.otpUltimoEnviado) { return; }
    this.otpUltimoEnviado = codigo;

    if (this.mfaVista === 'enroll') { this.submitEnroll(); return; }
    if (this.mfaVista === 'verify') {
      if (this.resetStep === 0) { this.submitVerify(); }
      else if (this.resetStep === 2) { this.submitResetConfirm(); }
    }
  }

  /**
   * Enfoca una casilla. Se difiere un instante para que Angular termine de dibujar la vista
   * (p. ej. al pasar de "restablecer" a "activar") y se reintenta si aún no existe.
   */
  focusOtp(idx: number): void {
    this.intentarFocoOtp(idx, 15, 0);
  }

  private intentarFocoOtp(idx: number, intentos: number, demora: number): void {
    setTimeout(() => {
      const el = document.getElementById('otp-' + idx) as HTMLInputElement | null;
      if (el) {
        el.focus();
        el.select();
        return;
      }
      if (intentos > 0) { this.intentarFocoOtp(idx, intentos - 1, 40); }
    }, demora);
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
    this.focusOtp(0);
  }

  private handleStep(r: MfaStepResponse): void {
    this.mfaLoading = false;

    // 2FA completo → JWT definitivo.
    if (r.success && r.token) {
      this.detenerPush();
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
      this.focusOtp(0);
      return;
    }

    // Mensaje informativo (p. ej. "código enviado a su correo").
    if (r.isInfo) {
      this.mfaInfo = r.message || 'Operación realizada.';
      if (this.resetStep === 1) {
        this.resetStep = 2;
        this.clearOtp();
      }
      if (this.resetStep === 2) { this.focusOtp(0); }
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
