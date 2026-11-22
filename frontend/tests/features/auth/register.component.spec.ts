import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { RegisterComponent } from '../../../src/app/features/auth/components/register/register.component';
import { AuthService } from '../../../src/app/core/services/auth.service';

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let component: RegisterComponent;
  let authService: {
    register: ReturnType<typeof vi.fn>;
    loginWithGoogle: ReturnType<typeof vi.fn>;
    renderGoogleButton: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(async () => {
    authService = {
      register: vi.fn(),
      loginWithGoogle: vi.fn(),
      renderGoogleButton: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockReturnValue(Promise.resolve(true));

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('Given_InitialState_When_Rendered_Then_RoleDefaultsToStudent', () => {
    expect(component.registerForm.get('role')?.value).toBe('Student');
    expect(component.registerForm.valid).toBe(false);
  });

  it('Given_RoleButton_When_SetRoleCalled_Then_UpdatesFormRole', () => {
    component.setRole('Tutor');
    expect(component.registerForm.get('role')?.value).toBe('Tutor');

    component.setRole('Student');
    expect(component.registerForm.get('role')?.value).toBe('Student');
  });

  it('Given_ValidForm_When_Submit_Then_CallsRegisterAndNavigatesToSessions', () => {
    component.registerForm.setValue({
      fullName: 'Andrei Popa',
      email: 'andrei@tutormatch.ro',
      password: 'password123',
      role: 'Tutor'
    });
    authService.register.mockReturnValue(
      of({
        token: 'token-123',
        userId: 'u-tutor',
        email: 'andrei@tutormatch.ro',
        fullName: 'Andrei Popa',
        role: 'Tutor'
      })
    );

    component.onSubmit();

    expect(authService.register).toHaveBeenCalledWith({
      fullName: 'Andrei Popa',
      email: 'andrei@tutormatch.ro',
      password: 'password123',
      role: 'Tutor'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/sessions');
  });

  it('Given_RegistrationError_When_Submit_Then_DisplaysErrorMessage', () => {
    component.registerForm.setValue({
      fullName: 'Andrei Popa',
      email: 'existing@tutormatch.ro',
      password: 'password123',
      role: 'Student'
    });
    authService.register.mockReturnValue(
      throwError(() => ({
        error: { description: 'A user with this email already exists.' }
      }))
    );

    component.onSubmit();

    expect(component.errorMessage()).toBe('A user with this email already exists.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('Given_GoogleRegister_When_OnGoogleRegisterCalled_Then_PassesRoleAndNavigates', () => {
    component.setRole('Tutor');
    authService.loginWithGoogle.mockReturnValue(
      of({
        token: 'token-google',
        userId: 'u-google',
        email: 'andrei.google@tutormatch.ro',
        fullName: 'Andrei Google',
        role: 'Tutor'
      })
    );

    component.onGoogleRegister('mock-google-token');

    expect(authService.loginWithGoogle).toHaveBeenCalledWith({
      credential: 'mock-google-token',
      role: 'Tutor'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/sessions');
  });
});
