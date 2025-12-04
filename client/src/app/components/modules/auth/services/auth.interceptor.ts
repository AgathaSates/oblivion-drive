import { HttpErrorResponse, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { catchError, throwError } from 'rxjs';

export const authInterceptor = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  const accessToken = authService.accessTokenSubject$.getValue();

  if (accessToken?.key) {
    const request = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${accessToken.key}`),
    });

    return next(request).pipe(
      catchError((err) => {
        if (err instanceof HttpErrorResponse && err.status === 401) {
          authService.accessTokenSubject$.next(undefined);
          router.navigate(['/auth', 'login']);
        }

        return throwError(() => err);
      }),
    );
  }

  return next(req);
};
