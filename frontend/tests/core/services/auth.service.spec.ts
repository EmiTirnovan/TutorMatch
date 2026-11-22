import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from '../../../src/app/core/services/auth.service';
import { AuthResponse } from '../../../src/app/core/models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: { navigate: ReturnType<typeof vi.fn> };

  const fakeHeader = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');

  const createFakeJwt = (payload: object): string => {
    const encodedPayload = btoa(JSON.stringify(payload))
      .replace(/=/g, '')
      .replace(/\+/g, '-')
      .replace(/\//g, '_');
    return `${fakeHeader}.${encodedPayload}.signature`;
  };

  const validToken = createFakeJwt({
    nameid: 'user-1',
    email: 'user@tutormatch.ro',
    name: 'Ion Popescu',
    role: 'Student',
    exp: Math.floor(Date.now() / 1000) + 3600
  });

  const tutorToken = createFakeJwt({
    nameid: 'tutor-1',
    email: 'tutor@tutormatch.ro',
    name: 'Profesor Pop',
    role: 'Tutor',
    exp: Math.floor(Date.now() / 1000) + 3600
  });

  const expiredToken = createFakeJwt({
    nameid: 'user-old',
    email: 'old@tutormatch.ro',
    name: 'Old User',
    role: 'Student',
    exp: Math.floor(Date.now() / 1000) - 3600
  });

  beforeEach(() => {
    localStorage.clear();
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        { provide: Router, useValue: router }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('Given_NoStoredToken_When_Initialized_Then_UserIsNullAndUnauthenticated', () => {
    expect(service.currentUser()).toBeNull();
    expect(service.token()).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.isStudent()).toBe(false);
    expect(service.isTutor()).toBe(false);
  });

  it('Given_ValidLogin_When_Success_Then_SetsTokenAndCurrentUserAndIsAuthenticated', () => {
    const mockResponse: AuthResponse = {
      token: validToken,
      userId: 'user-1',
      email: 'user@tutormatch.ro',
      fullName: 'Ion Popescu',
      role: 'Student'
    };

    service.login({ email: 'user@tutormatch.ro', password: 'password123' }).subscribe((res) => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@tutormatch.ro', password: 'password123' });
    req.flush(mockResponse);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.isStudent()).toBe(true);
    expect(service.isTutor()).toBe(false);
    expect(service.token()).toBe(validToken);
    expect(service.currentUser()?.fullName).toBe('Ion Popescu');
    expect(localStorage.getItem('tutormatch_token')).toBe(validToken);
  });

  it('Given_ValidRegistration_When_Success_Then_SetsSession', () => {
    const mockResponse: AuthResponse = {
      token: tutorToken,
      userId: 'tutor-1',
      email: 'tutor@tutormatch.ro',
      fullName: 'Profesor Pop',
      role: 'Tutor'
    };

    service.register({
      fullName: 'Profesor Pop',
      email: 'tutor@tutormatch.ro',
      password: 'password123',
      role: 'Tutor'
    }).subscribe();

    const req = httpMock.expectOne('/api/auth/register');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.isTutor()).toBe(true);
    expect(service.isStudent()).toBe(false);
    expect(service.currentUser()?.id).toBe('tutor-1');
  });

  it('Given_GoogleAuth_When_Success_Then_SetsSession', () => {
    const mockResponse: AuthResponse = {
      token: validToken,
      userId: 'user-1',
      email: 'user@tutormatch.ro',
      fullName: 'Ion Popescu',
      role: 'Student'
    };

    service.loginWithGoogle({ credential: 'google-jwt-credential', role: 'Student' }).subscribe();

    const req = httpMock.expectOne('/api/auth/google');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ credential: 'google-jwt-credential', role: 'Student' });
    req.flush(mockResponse);

    expect(service.isAuthenticated()).toBe(true);
  });

  it('Given_AuthenticatedUser_When_Logout_Then_ClearsStorageAndNavigatesToLogin', () => {
    service.handleAuthSuccess({
      token: validToken,
      userId: 'user-1',
      email: 'user@tutormatch.ro',
      fullName: 'Ion Popescu',
      role: 'Student'
    });
    expect(service.isAuthenticated()).toBe(true);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(service.token()).toBeNull();
    expect(localStorage.getItem('tutormatch_token')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('Given_ValidTokenInLocalStorage_When_ServiceInstantiated_Then_RestoresUserSession', () => {
    localStorage.setItem('tutormatch_token', validToken);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        { provide: Router, useValue: router }
      ]
    });

    const restoredService = TestBed.inject(AuthService);
    expect(restoredService.isAuthenticated()).toBe(true);
    expect(restoredService.currentUser()?.email).toBe('user@tutormatch.ro');
    expect(restoredService.currentUser()?.fullName).toBe('Ion Popescu');
  });

  it('Given_ExpiredTokenInLocalStorage_When_ServiceInstantiated_Then_RemovesTokenAndRemainsUnauthenticated', () => {
    localStorage.setItem('tutormatch_token', expiredToken);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        { provide: Router, useValue: router }
      ]
    });

    const restoredService = TestBed.inject(AuthService);
    expect(restoredService.isAuthenticated()).toBe(false);
    expect(restoredService.currentUser()).toBeNull();
    expect(localStorage.getItem('tutormatch_token')).toBeNull();
  });
});
