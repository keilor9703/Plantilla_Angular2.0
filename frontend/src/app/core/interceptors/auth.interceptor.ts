import { HttpInterceptorFn, HttpErrorResponse, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError, Observable } from 'rxjs';

let isRefreshing = false;
let pendingRequests: Array<{ resolve: (token: string) => void; reject: (error: any) => void }> = [];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();

  const baseReq = req.clone({ withCredentials: true });

  if (req.url.includes('/Cuenta/Refresh') || req.url.includes('/Cuenta/Token') || req.url.includes('/setup/')) {
    return next(baseReq);
  }

  const authReq = token ? baseReq.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : baseReq;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && token && !authService.isImpersonating()) {
        if (!isRefreshing) {
          isRefreshing = true;
          return authService.refreshToken().pipe(
            switchMap((newToken) => {
              isRefreshing = false;
              pendingRequests.forEach(p => p.resolve(newToken));
              pendingRequests = [];
              const retryReq = req.clone({
                setHeaders: { Authorization: `Bearer ${newToken}` },
                withCredentials: true
              });
              return next(retryReq);
            }),
            catchError((refreshError) => {
              isRefreshing = false;
              pendingRequests.forEach(p => p.reject(refreshError));
              pendingRequests = [];
              authService.logout();
              router.navigate(['/login']);
              return throwError(() => refreshError);
            })
          );
        } else {
          return new Observable<HttpEvent<any>>(obs => {
            pendingRequests.push({
              resolve: (newToken: string) => {
                const retryReq = req.clone({
                  setHeaders: { Authorization: `Bearer ${newToken}` },
                  withCredentials: true
                });
                next(retryReq).subscribe(obs);
              },
              reject: (err: any) => {
                obs.error(err);
              }
            });
          });
        }
      }
      return throwError(() => error);
    })
  );
};
