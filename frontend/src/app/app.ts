import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly title = signal('TutorMatch');
  readonly userRole = signal<'student' | 'tutor'>('tutor');

  toggleRole(): void {
    this.userRole.update(r => r === 'tutor' ? 'student' : 'tutor');
  }
}
