import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, timeout, map, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

interface LoginRequest {
  Usuario: string;
  Contrasena: string;
  DeviceId?: string;
}

interface LoginResponse {
  success: boolean;
  Success?: boolean;
  token?: string;
  mensaje?: string;
  message?: string;
  usuario?: string;
  funcionario?: string;
  identificacion?: number;
  impersonating?: boolean;
  impersonatedBy?: string;
  // Segundo factor (SI2FA)
  requiere2Fa?: boolean;
  enrolar?: boolean;
  challenge?: string;
  qr?: string;
  claveManual?: string;
  enrollToken?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'sisge_token';
  private readonly userKey = 'sisge_usuario';
  private readonly authKey = 'sisge_auth';
  private readonly userIdKey = 'sisge_user_id';
  private readonly deviceIdKey = 'si2fa_device_id';
  private readonly impersonationFlagKey = 'sisge_impersonating';
  private readonly impersonationOriginalTokenKey = 'sisge_impersonation_original_token';
  private readonly impersonationOriginalUserKey = 'sisge_impersonation_original_user';
  private readonly impersonationOriginalUserIdKey = 'sisge_impersonation_original_user_id';
  private readonly impersonationActorUserKey = 'sisge_impersonation_actor_user';
  private readonly loginUrl = `${environment.apiBaseUrl}/Cuenta/Token`;
  private readonly impersonateUrl = `${environment.apiBaseUrl}/Cuenta/Impersonate`;
  private readonly refreshUrl = `${environment.apiBaseUrl}/Cuenta/Refresh`;
  private readonly mfaVerifyUrl = `${environment.apiBaseUrl}/Cuenta/Mfa/Verify`;
  private readonly mfaEnrollUrl = `${environment.apiBaseUrl}/Cuenta/Mfa/EnrollConfirm`;
  private readonly maxJwtLength = 8192;
  private readonly maxJwtPayloadB64Length = 4096;
  private readonly maxJwtPayloadJsonLength = 6144;

  constructor(private http: HttpClient) {}

  login(usuario: string, contrasena: string): Observable<LoginResponse> {
    const payload: LoginRequest = {
      Usuario: usuario,
      Contrasena: contrasena,
      DeviceId: this.getDeviceId()
    };

    return this.http.post<LoginResponse>(this.loginUrl, payload, { withCredentials: true }).pipe(
      timeout(30000),
      tap((resp) => {
        // Si se requiere segundo factor, NO se establece sesión todavía (no hay token).
        if (resp?.requiere2Fa) {
          return;
        }
        if (this.isLoginSuccessful(resp) && resp.token) {
          this.finalizeSession(resp, usuario);
        }
      })
    );
  }

  /** Paso 2: verificar el código del segundo factor. */
  mfaVerify(challenge: string, codigo: string, rememberDevice = false): Observable<LoginResponse> {
    const body = { Challenge: challenge, Codigo: codigo, RememberDevice: rememberDevice, DeviceId: this.getDeviceId() };
    return this.http.post<LoginResponse>(this.mfaVerifyUrl, body, { withCredentials: true }).pipe(
      timeout(30000),
      tap((resp) => {
        if (this.isLoginSuccessful(resp) && resp.token) {
          this.finalizeSession(resp, resp.usuario ?? '');
        }
      })
    );
  }

  /** Paso 2 (primera vez): confirmar el enrolamiento con el primer código. */
  mfaEnrollConfirm(challenge: string, codigo: string, enrollToken: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(this.mfaEnrollUrl, { Challenge: challenge, Codigo: codigo, EnrollToken: enrollToken }, { withCredentials: true }).pipe(
      timeout(30000),
      tap((resp) => {
        if (this.isLoginSuccessful(resp) && resp.token) {
          this.finalizeSession(resp, resp.usuario ?? '');
        }
      })
    );
  }

  /** Autoservicio: solicita el restablecimiento del segundo factor (envía código al correo). */
  mfaResetRequest(challenge: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/Cuenta/Mfa/ResetRequest`, { Challenge: challenge }, { withCredentials: true }).pipe(
      timeout(30000)
    );
  }

  /** Autoservicio: confirma el código del correo; la respuesta trae los datos para re-enrolar. */
  mfaResetConfirm(challenge: string, codigo: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/Cuenta/Mfa/ResetConfirm`, { Challenge: challenge, Codigo: codigo }, { withCredentials: true }).pipe(
      timeout(30000)
    );
  }

  private finalizeSession(resp: LoginResponse, usuario: string): void {
    this.clearImpersonationState();
    if (resp.token) {
      this.applySession(resp.token, resp.usuario ?? usuario);
    }
    localStorage.setItem(this.authKey, '1');
    localStorage.setItem(this.userKey, resp.usuario ?? usuario);
    sessionStorage.removeItem('modales_vistos');
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    localStorage.removeItem(this.authKey);
    localStorage.removeItem(this.userIdKey);
    this.clearImpersonationState();
  }

  startImpersonation(usuarioObjetivo: string): Observable<LoginResponse> {
    const usuario = (usuarioObjetivo ?? '').trim();
    return this.http.post<LoginResponse>(this.impersonateUrl, { usuario }).pipe(
      timeout(30000),
      tap((resp) => {
        if (!this.isLoginSuccessful(resp) || !resp?.token) {
          return;
        }

        if (!this.isImpersonating()) {
          const currentToken = this.getToken() ?? '';
          const currentUser = this.getUsuario();
          const currentUserId = this.getUserId();
          localStorage.setItem(this.impersonationOriginalTokenKey, currentToken);
          localStorage.setItem(this.impersonationOriginalUserKey, currentUser);
          if (currentUserId !== null) {
            localStorage.setItem(this.impersonationOriginalUserIdKey, String(currentUserId));
          } else {
            localStorage.removeItem(this.impersonationOriginalUserIdKey);
          }
        }

        this.applySession(resp.token, resp.usuario ?? usuario);
        localStorage.setItem(this.authKey, '1');
        localStorage.setItem(this.impersonationFlagKey, '1');
        localStorage.setItem(this.impersonationActorUserKey, this.getOriginalUser());
      })
    );
  }

  stopImpersonation(): boolean {
    if (!this.isImpersonating()) {
      return false;
    }

    const originalToken = localStorage.getItem(this.impersonationOriginalTokenKey);
    const originalUser = localStorage.getItem(this.impersonationOriginalUserKey);
    const originalUserId = localStorage.getItem(this.impersonationOriginalUserIdKey);

    if (!originalToken || !originalUser) {
      this.logout();
      return false;
    }

    localStorage.setItem(this.tokenKey, originalToken);
    localStorage.setItem(this.userKey, originalUser);
    localStorage.setItem(this.authKey, '1');
    if (originalUserId) {
      localStorage.setItem(this.userIdKey, originalUserId);
    } else {
      localStorage.removeItem(this.userIdKey);
    }

    this.clearImpersonationState();
    return true;
  }

  isImpersonating(): boolean {
    return localStorage.getItem(this.impersonationFlagKey) === '1';
  }

  getOriginalUser(): string {
    return localStorage.getItem(this.impersonationOriginalUserKey) ?? '';
  }

  getImpersonationActorUser(): string {
    return localStorage.getItem(this.impersonationActorUserKey) ?? this.getOriginalUser();
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getUsuario(): string {
    return localStorage.getItem(this.userKey) ?? 'Usuario';
  }

  refreshToken(): Observable<string> {
    const token = this.getToken();
    if (!token) {
      return throwError(() => new Error('No hay token para refrescar'));
    }
    return this.http.post<LoginResponse>(this.refreshUrl, { token }, { withCredentials: true }).pipe(
      tap((resp) => {
        if (resp.token) {
          this.applySession(resp.token, this.getUsuario());
        }
      }),
      map((resp) => {
        if (!resp.token) throw new Error('No se pudo renovar el token');
        return resp.token;
      })
    );
  }

  getUserId(): number | null {
    const raw = localStorage.getItem(this.userIdKey);
    if (!raw) {
      return null;
    }
    const value = Number(raw);
    return Number.isFinite(value) ? value : null;
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    if (this.isTokenExpired(token)) {
      this.logout();
      return false;
    }

    return true;
  }

  isLoginSuccessful(resp: LoginResponse | null | undefined): boolean {
    return !!resp && (resp.success === true || resp.Success === true || !!resp.token);
  }

  isCurrentUserSuperAdmin(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    try {
      const parsed = this.decodeJwtPayload(token);
      if (!parsed) {
        return false;
      }

      const roleClaimUri = ['http', '://schemas.microsoft.com/ws/2008/06/identity/claims/role'].join('');
      const roleClaimShort = 'role';
      const roleClaimLegacy = 'roles';

      const rawRoles = [
        parsed?.[roleClaimUri],
        parsed?.[roleClaimShort],
        parsed?.[roleClaimLegacy]
      ];

      const values = rawRoles.flatMap((r) => (Array.isArray(r) ? r : [r])).map((v) => String(v ?? '').trim());
      return values.some((v) => v === '1');
    } catch {
      return false;
    }
  }

  /** Identificador estable por navegador para el reconocimiento de dispositivos confiables. */
  getDeviceId(): string {
    try {
      let id = localStorage.getItem(this.deviceIdKey);
      if (!id) {
        id = (typeof crypto !== 'undefined' && (crypto as any).randomUUID)
          ? (crypto as any).randomUUID()
          : 'dev-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2);
        localStorage.setItem(this.deviceIdKey, id);
      }
      return id;
    } catch {
      return '';
    }
  }

  private applySession(token: string, usuario: string): void {
    localStorage.setItem(this.tokenKey, token);
    const userId = this.extractUserIdFromToken(token);
    if (userId !== null) {
      localStorage.setItem(this.userIdKey, String(userId));
    } else {
      localStorage.removeItem(this.userIdKey);
    }
    localStorage.setItem(this.userKey, usuario);
  }

  private clearImpersonationState(): void {
    localStorage.removeItem(this.impersonationFlagKey);
    localStorage.removeItem(this.impersonationOriginalTokenKey);
    localStorage.removeItem(this.impersonationOriginalUserKey);
    localStorage.removeItem(this.impersonationOriginalUserIdKey);
    localStorage.removeItem(this.impersonationActorUserKey);
  }

  private extractUserIdFromToken(token: string): number | null {
    try {
      const parsed = this.decodeJwtPayload(token);
      if (!parsed) {
        return null;
      }

      const rawId =
        parsed?.id_usuario ??
        parsed?.nameid ??
        parsed?.sub ??
        parsed?.[['http', '://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'].join('')];
      const userId = Number(rawId);
      return Number.isFinite(userId) ? userId : null;
    } catch {
      return null;
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      const parsed = this.decodeJwtPayload(token);
      if (!parsed) {
        return true;
      }

      const exp = Number(parsed?.exp);
      if (!Number.isFinite(exp) || exp <= 0) {
        return false;
      }

      const now = Math.floor(Date.now() / 1000);
      return exp <= now;
    } catch {
      return true;
    }
  }

  private decodeJwtPayload(token: string): any | null {
    const raw = String(token ?? '').trim();
    if (!raw || raw.length > this.maxJwtLength) {
      return null;
    }

    const parts = raw.split('.');
    if (parts.length < 2) {
      return null;
    }

    const payload = (parts[1] ?? '').trim();
    if (!payload || payload.length > this.maxJwtPayloadB64Length) {
      return null;
    }

    // Base64URL permitidos
    if (!/^[A-Za-z0-9\-_]+$/.test(payload)) {
      return null;
    }

    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
    const decoded = atob(padded);
    if (!decoded || decoded.length > this.maxJwtPayloadJsonLength) {
      return null;
    }

    return JSON.parse(decoded);
  }
}
