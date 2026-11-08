import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { UserContextService } from './core/services/user-context.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly userContext = inject(UserContextService);

  readonly title = signal('TutorMatch');
  readonly userRole = this.userContext.currentRole;
  readonly tutorId = this.userContext.tutorId;
  readonly studentId = this.userContext.studentId;
  readonly activeUserName = this.userContext.currentUserName;

  toggleRole(): void {
    this.userContext.toggleRole();
  }
}
