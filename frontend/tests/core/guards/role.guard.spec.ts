import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { roleGuard } from '../../../src/app/core/guards/role.guard';
import { AuthService } from '../../../src/app/core/services/auth.service';

describe('roleGuard', () => {
  let authServiceMock: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    currentUser: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authServiceMock = {
      isAuthenticated: vi.fn(),
      currentUser: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock }
      ]
    });
  });

  it('Given_UnauthenticatedUser_When_GuardRuns_Then_RedirectsToLogin', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);

    const guard = roleGuard(['tutor']);
    const result = TestBed.runInInjectionContext(() =>
      guard({} as any, { url: '/tutors/1/availability' } as any)
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/login?returnUrl=%2Ftutors%2F1%2Favailability');
  });

  it('Given_UserWithMatchingRole_When_GuardRuns_Then_AllowsActivation', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);
    authServiceMock.currentUser.mockReturnValue({ role: 'Tutor' });

    const guard = roleGuard(['tutor']);
    const result = TestBed.runInInjectionContext(() =>
      guard({} as any, { url: '/tutors/1/availability' } as any)
    );

    expect(result).toBe(true);
  });

  it('Given_UserWithoutMatchingRole_When_GuardRuns_Then_RedirectsToSessions', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);
    authServiceMock.currentUser.mockReturnValue({ role: 'Student' });

    const guard = roleGuard(['tutor']);
    const result = TestBed.runInInjectionContext(() =>
      guard({} as any, { url: '/tutors/1/availability' } as any)
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/sessions');
  });
});
