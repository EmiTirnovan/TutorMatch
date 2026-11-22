import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { LoginComponent } from '../../../src/app/features/auth/components/login/login.component';
import { AuthService } from '../../../src/app/core/services/auth.service';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let authService: {
    login: ReturnType<typeof vi.fn>;
    loginWithGoogle: ReturnType<typeof vi.fn>;
    renderGoogleButton: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(async () => {
    authService = {
      login: vi.fn(),
      loginWithGoogle: vi.fn(),
      renderGoogleButton: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParams: { returnUrl: '/sessions' }
            }
          }
        }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockReturnValue(Promise.resolve(true));

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('Given_InitialState_When_Rendered_Then_FormIsInvalidAndFieldsEmpty', () => {
    expect(component.loginForm.valid).toBe(false);
    expect(component.loginForm.get('email')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
    expect(component.isLoading()).toBe(false);
  });

  it('Given_InvalidForm_When_Submit_Then_DoesNotCallAuthService', () => {
    component.onSubmit();
    expect(authService.login).not.toHaveBeenCalled();
    expect(component.loginForm.touched).toBe(true);
  });

  it('Given_ValidForm_When_SubmitSuccessful_Then_CallsLoginAndNavigatesToReturnUrl', () => {
    component.loginForm.setValue({
      email: 'alex@example.com',
      password: 'password123'
    });
    authService.login.mockReturnValue(
      of({
        token: 'token-abc',
        userId: 'u1',
        email: 'alex@example.com',
        fullName: 'Alex I',
        role: 'Student'
      })
    );

    component.onSubmit();

    expect(authService.login).toHaveBeenCalledWith({
      email: 'alex@example.com',
      password: 'password123'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/sessions');
    expect(component.isLoading()).toBe(false);
  });

  it('Given_ValidForm_When_SubmitFails_Then_DisplaysErrorMessage', () => {
    component.loginForm.setValue({
      email: 'alex@example.com',
      password: 'wrongpassword'
    });
    authService.login.mockReturnValue(
      throwError(() => ({
        error: { description: 'Invalid email or password.' }
      }))
    );

    component.onSubmit();

    expect(component.errorMessage()).toBe('Invalid email or password.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(component.isLoading()).toBe(false);
  });

  it('Given_GoogleCredential_When_OnGoogleLogin_Then_AuthenticatesAndRedirects', () => {
    authService.loginWithGoogle.mockReturnValue(
      of({
        token: 'token-google',
        userId: 'u2',
        email: 'alex.google@example.com',
        fullName: 'Alex Google',
        role: 'Student'
      })
    );

    component.onGoogleLogin('mock-google-id-token');

    expect(authService.loginWithGoogle).toHaveBeenCalledWith({
      credential: 'mock-google-id-token'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/sessions');
  });

  it('Given_Component_When_AfterViewInit_Then_RendersGoogleButton', () => {
    expect(authService.renderGoogleButton).toHaveBeenCalled();
  });
});
