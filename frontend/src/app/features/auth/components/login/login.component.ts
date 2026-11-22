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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements AfterViewInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly googleBtn = viewChild<ElementRef<HTMLElement>>('googleBtn');
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly loginForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(6)])
  });

  private returnUrl = '/sessions';

  constructor() {
    const queryReturn = this.route.snapshot.queryParams['returnUrl'];
    if (queryReturn) {
      this.returnUrl = queryReturn;
    }
  }

  ngAfterViewInit(): void {
    const btnElem = this.googleBtn()?.nativeElement;
    if (btnElem) {
      this.authService.renderGoogleButton(btnElem, (credential) =>
        this.onGoogleLogin(credential)
      );
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.loginForm.getRawValue();

    this.authService
      .login({
        email: email!.trim(),
        password: password!
      })
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.router.navigateByUrl(this.returnUrl);
        },
        error: (err) => {
          this.isLoading.set(false);
          const message =
            err.error?.description ||
            err.error?.message ||
            'Invalid email or password. Please try again.';
          this.errorMessage.set(message);
        }
      });
  }

  onGoogleLogin(credential: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService
      .loginWithGoogle({ credential })
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.router.navigateByUrl(this.returnUrl);
        },
        error: (err) => {
          this.isLoading.set(false);
          const message =
            err.error?.description ||
            err.error?.message ||
            'Google Sign-In failed. Please try again.';
          this.errorMessage.set(message);
        }
      });
  }
}
