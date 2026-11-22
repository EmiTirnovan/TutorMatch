import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { UserContextService } from './core/services/user-context.service';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly userContext = inject(UserContextService);
  readonly authService = inject(AuthService);

  readonly title = signal('TutorMatch');
  readonly userRole = this.userContext.currentRole;
  readonly tutorId = this.userContext.tutorId;
  readonly studentId = this.userContext.studentId;
  readonly activeUserName = this.userContext.currentUserName;
  readonly isAuthenticated = this.authService.isAuthenticated;
  readonly currentUser = this.authService.currentUser;

  logout(): void {
    this.authService.logout();
  }
}
