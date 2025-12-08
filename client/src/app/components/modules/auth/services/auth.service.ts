import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import {
  BehaviorSubject,
  catchError,
  defer,
  distinctUntilChanged,
  finalize,
  merge,
  Observable,
  of,
  shareReplay,
  skip,
  tap,
} from 'rxjs';
import { AccessTokenModel, LoginModel, RegisterModel } from '../models/auth.models';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl + '/api/auth';
  private readonly keyAccessToken: string = 'oblivion-drive:access-token';

  public readonly accessTokenSubject$ = new BehaviorSubject<AccessTokenModel | undefined>(
    undefined,
  );

  public readonly accessTokenStored$ = defer(() => {
    const accessToken = this.getAccessToken();

    if (!accessToken) return of(undefined);

    const { expiration } = accessToken;
    const valid = new Date(expiration) > new Date();

    if (!valid) return of(undefined);

    return of(accessToken);
  });

  public readonly accessToken$ = merge(
    this.accessTokenStored$,
    this.accessTokenSubject$.pipe(skip(1)),
  ).pipe(
    distinctUntilChanged((a, b) => a?.key === b?.key),
    tap((accessToken) => {
      if (accessToken) {
        this.saveAccessToken(accessToken);
      } else {
        this.cleanAccessToken();
      }

      this.accessTokenSubject$.next(accessToken);
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public registro(registerModel: RegisterModel): Observable<AccessTokenModel> {
    const fullUrl = `${this.apiUrl}/register`;

    return this.http
      .post<AccessTokenModel>(fullUrl, registerModel)
      .pipe(tap((token) => this.accessTokenSubject$.next(token)));
  }

  public login(loginModel: LoginModel): Observable<AccessTokenModel> {
    const fullUrl = `${this.apiUrl}/login`;

    return this.http
      .post<AccessTokenModel>(fullUrl, loginModel)
      .pipe(tap((token) => this.accessTokenSubject$.next(token)));
  }

  public logout(): Observable<null> {
    const fullUrl = `${this.apiUrl}/logout`;

    return this.http.post<null>(fullUrl, {}).pipe(
      catchError((err) => {
        console.warn('[AuthService] Erro no logout, ignorando:', err);
        return of(null);
      }),
      finalize(() => {
        this.accessTokenSubject$.next(undefined);
      }),
    );
  }

  private saveAccessToken(token: AccessTokenModel): void {
    const jsonString = JSON.stringify(token);

    localStorage.setItem(this.keyAccessToken, jsonString);
  }

  private cleanAccessToken(): void {
    localStorage.removeItem(this.keyAccessToken);
  }

  private getAccessToken(): AccessTokenModel | undefined {
    const jsonString = localStorage.getItem(this.keyAccessToken);

    if (!jsonString) return undefined;

    return JSON.parse(jsonString);
  }
}
