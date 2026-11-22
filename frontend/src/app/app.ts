import { Component, OnInit, inject, signal } from '@angular/core';
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
export class App implements OnInit {
  readonly userContext = inject(UserContextService);
  readonly authService = inject(AuthService);

  readonly title = signal('TutorMatch');
  readonly userRole = this.userContext.currentRole;
  readonly tutorId = this.userContext.tutorId;
  readonly studentId = this.userContext.studentId;
  readonly activeUserName = this.userContext.currentUserName;
  readonly isAuthenticated = this.authService.isAuthenticated;
  readonly currentUser = this.authService.currentUser;
  readonly myTutorId = signal<string | null>(null);

  ngOnInit(): void {
    this.refreshMyTutor();
  }

  refreshMyTutor(): void {
    try {
      const stored = localStorage.getItem('tutor_match_my_tutor_id');
      if (stored) {
        this.myTutorId.set(stored);
      }
    } catch {
      // ignore in environments without localStorage
    }
  }

  logout(): void {
    this.authService.logout();
  }
}
