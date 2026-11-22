import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { authGuard } from '../../../src/app/core/guards/auth.guard';
import { AuthService } from '../../../src/app/core/services/auth.service';

describe('authGuard', () => {
  let authServiceMock: { isAuthenticated: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    authServiceMock = {
      isAuthenticated: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock }
      ]
    });

    router = TestBed.inject(Router);
  });

  it('Given_AuthenticatedUser_When_GuardRuns_Then_ReturnsTrue', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as any, { url: '/sessions' } as any)
    );

    expect(result).toBe(true);
  });

  it('Given_UnauthenticatedUser_When_GuardRuns_Then_ReturnsUrlTreeToLoginWithReturnUrl', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as any, { url: '/sessions' } as any)
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/login?returnUrl=%2Fsessions');
  });
});
