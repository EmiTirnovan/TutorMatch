import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from '../../../src/app/core/interceptors/auth.interceptor';
import { AuthService } from '../../../src/app/core/services/auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        AuthService
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('Given_TokenAvailable_When_MakingApiRequest_Then_AppendsAuthorizationBearerHeader', () => {
    authService.token.set('jwt-test-token-123');

    http.get('/api/v2/sessions').subscribe();

    const req = httpMock.expectOne('/api/v2/sessions');
    expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-test-token-123');
    req.flush([]);
  });

  it('Given_NoToken_When_MakingApiRequest_Then_DoesNotAddAuthorizationHeader', () => {
    authService.token.set(null);

    http.get('/api/v2/sessions').subscribe();

    const req = httpMock.expectOne('/api/v2/sessions');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('Given_TokenAvailable_When_RequestIsExternal_Then_DoesNotAddAuthorizationHeader', () => {
    authService.token.set('jwt-test-token-123');

    http.get('https://nominatim.openstreetmap.org/search?format=json').subscribe();

    const req = httpMock.expectOne('https://nominatim.openstreetmap.org/search?format=json');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });
});
