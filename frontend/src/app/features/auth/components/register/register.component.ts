import {
  AfterViewInit,
  Component,
  ElementRef,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent implements AfterViewInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly googleBtn = viewChild<ElementRef<HTMLElement>>('googleBtn');
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly registerForm = new FormGroup({
    fullName: new FormControl('', [Validators.required, Validators.minLength(2)]),
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(6)]),
    role: new FormControl<'Student' | 'Tutor'>('Student', [Validators.required])
  });

  ngAfterViewInit(): void {
    const btnElem = this.googleBtn()?.nativeElement;
    if (btnElem) {
      this.authService.renderGoogleButton(btnElem, (credential) =>
        this.onGoogleRegister(credential)
      );
    }
  }

  setRole(role: 'Student' | 'Tutor'): void {
    this.registerForm.get('role')?.setValue(role);
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { fullName, email, password, role } = this.registerForm.getRawValue();

    this.authService
      .register({
        fullName: fullName!.trim(),
        email: email!.trim(),
        password: password!,
        role: role || 'Student'
      })
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.router.navigateByUrl('/sessions');
        },
        error: (err) => {
          this.isLoading.set(false);
          const message =
            err.error?.description ||
            err.error?.message ||
            'Registration failed. Please check your data and try again.';
          this.errorMessage.set(message);
        }
      });
  }

  onGoogleRegister(credential: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const role = this.registerForm.get('role')?.value || 'Student';

    this.authService
      .loginWithGoogle({ credential, role })
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.router.navigateByUrl('/sessions');
        },
        error: (err) => {
          this.isLoading.set(false);
          const message =
            err.error?.description ||
            err.error?.message ||
            'Google Sign-Up failed. Please try again.';
          this.errorMessage.set(message);
        }
      });
  }
}
