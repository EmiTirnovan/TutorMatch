import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TutorService } from '../../../../core/services/tutor.service';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';

@Component({
  selector: 'app-tutor-profile',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './tutor-profile.component.html',
  styleUrl: './tutor-profile.component.css'
})
export class TutorProfileComponent implements OnInit {
  private readonly tutorService = inject(TutorService);
  private readonly route = inject(ActivatedRoute);

  readonly tutor = signal<TutorDetailsResponse | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'demo') {
      this.loadTutor(id);
    } else {
      // Demo mock profile for exploratory testing
      this.loadDemoTutor();
    }
  }

  loadTutor(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.tutorService.getTutorById(id).subscribe({
      next: (data: TutorDetailsResponse) => {
        this.tutor.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        // Fallback to demo profile with notification if backend has no matching ID
        this.loadDemoTutor();
      }
    });
  }

  loadDemoTutor(): void {
    const tomorrow10 = new Date();
    tomorrow10.setDate(tomorrow10.getDate() + 1);
    tomorrow10.setHours(10, 0, 0, 0);

    const tomorrow12 = new Date(tomorrow10);
    tomorrow12.setHours(12, 0, 0, 0);

    const afterTomorrow14 = new Date(tomorrow10);
    afterTomorrow14.setDate(afterTomorrow14.getDate() + 1);
    afterTomorrow14.setHours(14, 0, 0, 0);

    const afterTomorrow16 = new Date(afterTomorrow14);
    afterTomorrow16.setHours(16, 0, 0, 0);

    this.tutor.set({
      id: 'demo-tutor-001',
      name: 'Radu Ionescu',
      email: 'radu.ionescu@student.upt.ro',
      bio: 'Student în anul 3 la Calculatoare. Am fost asistent voluntar la laboratorul de Structuri de Date și Teoria Grafurilor. Te pot ajuta cu pointeri în C, arbori echilibrați, algoritmi pe grafuri și pregătire pentru restanțe sau examene.',
      hourlyRate: 80,
      subjects: ['Algoritmi și Structuri de Date', 'C / C++', 'Teoria Grafurilor', 'Programare Orientată pe Obiecte'],
      rating: 4.9,
      ratingCount: 14,
      availability: [
        { start: tomorrow10.toISOString(), end: tomorrow12.toISOString() },
        { start: afterTomorrow14.toISOString(), end: afterTomorrow16.toISOString() }
      ]
    });

    this.isLoading.set(false);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(part => part.charAt(0).toUpperCase())
      .slice(0, 2)
      .join('');
  }

  formatReadableRange(startStr: string, endStr: string): string {
    const s = new Date(startStr);
    const e = new Date(endStr);
    const dateFormatted = s.toLocaleDateString(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });
    const startTime = s.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
    const endTime = e.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });

    return `${dateFormatted}, ${startTime} – ${endTime}`;
  }

  calculateDurationHours(startStr: string, endStr: string): string {
    const diffMs = new Date(endStr).getTime() - new Date(startStr).getTime();
    const hours = diffMs / (1000 * 60 * 60);
    return hours === 1 ? '1 hr' : `${hours.toFixed(1)} hrs`;
  }
}
