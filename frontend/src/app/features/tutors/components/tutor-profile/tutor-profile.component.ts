import { Component, ElementRef, Injector, OnInit, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TutorService } from '../../../../core/services/tutor.service';
import { SessionService } from '../../../../core/services/session.service';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';
import { DateRangeDto } from '../../../../core/models/date-range.dto';
import { SessionResponse } from '../../../../core/models/session.models';
import { SlotPickerComponent } from '../../../bookings/components/slot-picker/slot-picker.component';
import { BookSessionModalComponent } from '../../../bookings/components/book-session-modal/book-session-modal.component';
import { ConcurrencyConflictDialogComponent } from '../../../bookings/components/concurrency-conflict-dialog/concurrency-conflict-dialog.component';
import { UserContextService } from '../../../../core/services/user-context.service';

const DEMO_TUTOR_ID = 'demo-tutor-001';

@Component({
  selector: 'app-tutor-profile',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    SlotPickerComponent,
    BookSessionModalComponent,
    ConcurrencyConflictDialogComponent
  ],
  templateUrl: './tutor-profile.component.html',
  styleUrl: './tutor-profile.component.css'
})
export class TutorProfileComponent implements OnInit {
  private readonly tutorService = inject(TutorService);
  private readonly sessionService = inject(SessionService);
  private readonly route = inject(ActivatedRoute);
  private readonly injector = inject(Injector);
  readonly userContext = inject(UserContextService);
  private readonly slotsHeading = viewChild<ElementRef<HTMLElement>>('slotsHeading');

  readonly tutor = signal<TutorDetailsResponse | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly userRole = this.userContext.currentRole;
  readonly isOwnProfile = computed(() => {
    const t = this.tutor();
    return t ? t.id.toLowerCase() === this.userContext.tutorId().toLowerCase() : false;
  });

  readonly isDemoTutor = computed(() => this.tutor()?.id === DEMO_TUTOR_ID);
  readonly bookedSessions = signal<SessionResponse[]>([]);
  readonly slotsLoading = signal(false);
  readonly slotsError = signal(false);
  readonly selectedSlot = signal<DateRangeDto | null>(null);
  readonly isBookingOpen = signal(false);
  readonly hasConflict = signal(false);
  readonly successMessage = signal<string | null>(null);

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
        this.loadBookedSessions(data.id);
      },
      error: () => {
        // Fallback to demo profile with notification if backend has no matching ID
        this.loadDemoTutor();
      }
    });
  }

  loadBookedSessions(tutorId: string): void {
    this.slotsLoading.set(true);
    this.slotsError.set(false);

    this.sessionService.getBookedSessionsForTutor(tutorId).subscribe({
      next: sessions => {
        this.bookedSessions.set(sessions);
        this.slotsLoading.set(false);
      },
      error: () => {
        this.slotsError.set(true);
        this.slotsLoading.set(false);
      }
    });
  }

  onBooked(tutorId: string): void {
    const slot = this.selectedSlot();
    this.resetBooking();
    this.successMessage.set(
      slot ? `Your session on ${new Date(slot.start).toLocaleString()} is booked.` : 'Your session is booked.'
    );
    this.loadBookedSessions(tutorId);
  }

  onConflict(): void {
    this.isBookingOpen.set(false);
    this.hasConflict.set(true);
  }

  onRefreshAfterConflict(tutorId: string): void {
    this.hasConflict.set(false);
    this.resetBooking();
    this.loadBookedSessions(tutorId);
  }

  private resetBooking(): void {
    this.isBookingOpen.set(false);
    this.selectedSlot.set(null);
    this.successMessage.set(null);
    // The button that opened the dialog goes away with the reload, so keyboard users restart from the slots section.
    afterNextRender(() => this.slotsHeading()?.nativeElement.focus(), { injector: this.injector });
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
      id: DEMO_TUTOR_ID,
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
}
