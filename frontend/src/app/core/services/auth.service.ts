import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import {
  AuthResponse,
  AuthUser,
  GoogleAuthRequest,
  LoginRequest,
  RegisterRequest
} from '../models/auth.models';
import { environment } from '../../../environments/environment';

const TOKEN_KEY = 'tutormatch_token';

declare const google: any;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(null);
  readonly currentUser = signal<AuthUser | null>(null);

  readonly isAuthenticated = computed(() => !!this.currentUser());
  readonly isTutor = computed(() => this.currentUser()?.role?.toLowerCase() === 'tutor');
  readonly isStudent = computed(() => this.currentUser()?.role?.toLowerCase() === 'student');

  constructor() {
    this.initFromStorage();
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', credentials).pipe(
      tap((res) => this.handleAuthSuccess(res))
    );
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/register', data).pipe(
      tap((res) => this.handleAuthSuccess(res))
    );
  }

  loginWithGoogle(request: GoogleAuthRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/google', request).pipe(
      tap((res) => this.handleAuthSuccess(res))
    );
  }

  logout(): void {
    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem('tutor_match_my_tutor_id');
    } catch {
      // ignore
    }
    this.token.set(null);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this.token();
  }

  handleAuthSuccess(response: AuthResponse): void {
    if (response?.token) {
      try {
        localStorage.setItem(TOKEN_KEY, response.token);
      } catch {
        // ignore
      }
      this.token.set(response.token);
      this.currentUser.set({
        id: response.userId,
        email: response.email,
        fullName: response.fullName,
        role: response.role
      });
    }
  }

  getAuthConfig(): Observable<{ googleClientId: string }> {
    return this.http.get<{ googleClientId: string }>('/api/auth/config');
  }

  /**
   * Initializes Google Identity Services (GIS) button in a container element
   */
  renderGoogleButton(
    container: HTMLElement,
    onSuccess: (credential: string) => void,
    clientId?: string
  ): void {
    const effectiveClientId = clientId || environment.googleClientId;

    if (effectiveClientId && !effectiveClientId.includes('YOUR_GOOGLE_CLIENT_ID')) {
      this.doRenderGoogleButton(container, effectiveClientId, onSuccess);
      return;
    }

    // Auto-fetch Google Client ID from backend /api/auth/config
    this.getAuthConfig().subscribe({
      next: (config) => {
        if (config?.googleClientId) {
          this.doRenderGoogleButton(container, config.googleClientId, onSuccess);
        } else if (effectiveClientId) {
          this.doRenderGoogleButton(container, effectiveClientId, onSuccess);
        }
      },
      error: () => {
        if (effectiveClientId) {
          this.doRenderGoogleButton(container, effectiveClientId, onSuccess);
        }
      }
    });
  }

  private doRenderGoogleButton(
    container: HTMLElement,
    clientId: string,
    onSuccess: (credential: string) => void
  ): void {
    if (typeof google === 'undefined' || !google?.accounts?.id) {
      return;
    }

    try {
      google.accounts.id.initialize({
        client_id: clientId,
        callback: (res: any) => {
          if (res?.credential) {
            onSuccess(res.credential);
          }
        }
      });

      google.accounts.id.renderButton(container, {
        theme: 'outline',
        size: 'large',
        width: '100%',
        text: 'signin_with',
        shape: 'rectangular'
      });
    } catch (e) {
      console.warn('Google Identity Services render failed:', e);
    }
  }

  private initFromStorage(): void {
    try {
      const storedToken = localStorage.getItem(TOKEN_KEY);
      if (storedToken) {
        const user = this.parseJwt(storedToken);
        if (user) {
          this.token.set(storedToken);
          this.currentUser.set(user);
        } else {
          localStorage.removeItem(TOKEN_KEY);
        }
      }
    } catch {
      // ignore
    }
  }

  private parseJwt(token: string): AuthUser | null {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;

      const base64Url = parts[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );

      const decoded = JSON.parse(jsonPayload);

      // Check token expiration
      if (decoded.exp && decoded.exp * 1000 < Date.now()) {
        return null;
      }

      const id =
        decoded.nameid ||
        decoded.sub ||
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
      const email =
        decoded.email ||
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];
      const fullName =
        decoded.name ||
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
      const role =
        decoded.role ||
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'] ||
        'Student';

      if (!id || !email) return null;

      return {
        id,
        email,
        fullName: fullName || email.split('@')[0],
        role
      };
    } catch {
      return null;
    }
  }
}
