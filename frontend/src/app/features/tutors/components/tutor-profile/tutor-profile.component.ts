import { Component, ElementRef, Injector, OnInit, afterNextRender, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TutorService } from '../../../../core/services/tutor.service';
import { SessionService } from '../../../../core/services/session.service';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';
import { DateRangeDto } from '../../../../core/models/date-range.dto';
import { SessionResponse } from '../../../../core/models/session.models';
import { SlotPickerComponent } from '../../../bookings/components/slot-picker/slot-picker.component';
import { BookSessionModalComponent } from '../../../bookings/components/book-session-modal/book-session-modal.component';
import { ConcurrencyConflictDialogComponent } from '../../../bookings/components/concurrency-conflict-dialog/concurrency-conflict-dialog.component';
import { SubjectAutocompleteComponent } from '../subject-autocomplete/subject-autocomplete.component';
import { StarRatingComponent } from '../star-rating/star-rating.component';
import { TutorReviewsComponent } from '../tutor-reviews/tutor-reviews.component';
import { UserContextService } from '../../../../core/services/user-context.service';

@Component({
  selector: 'app-tutor-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    SlotPickerComponent,
    BookSessionModalComponent,
    ConcurrencyConflictDialogComponent,
    SubjectAutocompleteComponent,
    StarRatingComponent,
    TutorReviewsComponent
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

  readonly ratingsCount = computed(() => this.tutor()?.ratingsCount ?? 0);
  readonly averageRating = computed(() => this.tutor()?.averageRating ?? 0);
  readonly hasReviews = computed(() => this.ratingsCount() > 0);
  readonly reviewsLabel = computed(() => `${this.ratingsCount()} ${this.ratingsCount() === 1 ? 'review' : 'reviews'}`);
  readonly bookedSessions = signal<SessionResponse[]>([]);
  readonly slotsLoading = signal(false);
  readonly slotsError = signal(false);
  readonly selectedSlot = signal<DateRangeDto | null>(null);
  readonly isBookingOpen = signal(false);
  readonly hasConflict = signal(false);
  readonly successMessage = signal<string | null>(null);

  // Profile edit modal state
  readonly isEditProfileOpen = signal(false);
  readonly isSavingProfile = signal(false);
  readonly editErrorMessage = signal<string | null>(null);
  readonly editName = signal('');
  readonly editBio = signal('');
  readonly editHourlyRate = signal<number>(0);
  readonly editSubjects = signal<string[]>([]);
  newEditSubjectInput = '';

  ngOnInit(): void {
    const handleId = (id: string | null) => {
      const resolvedId = id || this.userContext.tutorId();
      if (resolvedId) {
        this.loadTutor(resolvedId);
      } else {
        this.isLoading.set(false);
        this.errorMessage.set('No tutor ID provided.');
      }
    };

    if (this.route?.paramMap) {
      this.route.paramMap.subscribe((params) => {
        handleId(params.get('id'));
      });
    } else {
      const id = this.route?.snapshot?.paramMap?.get('id');
      handleId(id);
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
      error: (err) => {
        this.isLoading.set(false);
        this.tutor.set(null);
        this.errorMessage.set(err?.error?.detail || err?.error?.message || 'Tutor profile not found.');
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
    afterNextRender(() => this.slotsHeading()?.nativeElement.focus(), { injector: this.injector });
  }

  openEditProfile(): void {
    const t = this.tutor();
    if (!t) return;
    this.editName.set(t.name);
    this.editBio.set(t.bio);
    this.editHourlyRate.set(t.hourlyRate);
    this.editSubjects.set(t.subjects ? [...t.subjects] : []);
    this.newEditSubjectInput = '';
    this.editErrorMessage.set(null);
    this.isEditProfileOpen.set(true);
  }

  closeEditProfile(): void {
    this.isEditProfileOpen.set(false);
    this.editErrorMessage.set(null);
  }

  onEditSubjectSelected(subject: string): void {
    const trimmed = subject.trim();
    if (!trimmed) return;
    if (!this.editSubjects().some(s => s.toLowerCase() === trimmed.toLowerCase())) {
      this.editSubjects.update(list => [...list, trimmed]);
    }
  }

  addEditSubject(): void {
    const trimmed = this.newEditSubjectInput.trim();
    if (!trimmed) return;
    if (!this.editSubjects().some(s => s.toLowerCase() === trimmed.toLowerCase())) {
      this.editSubjects.update(list => [...list, trimmed]);
    }
    this.newEditSubjectInput = '';
  }

  removeEditSubject(index: number): void {
    this.editSubjects.update(list => list.filter((_, i) => i !== index));
  }

  saveProfile(): void {
    const t = this.tutor();
    if (!t) return;

    const name = this.editName().trim();
    const bio = this.editBio().trim();
    const hourlyRate = Number(this.editHourlyRate());
    const subjects = this.editSubjects();

    if (name.length < 2) {
      this.editErrorMessage.set('Name must be at least 2 characters.');
      return;
    }
    if (bio.length < 10) {
      this.editErrorMessage.set('Bio must be at least 10 characters.');
      return;
    }
    if (hourlyRate <= 0) {
      this.editErrorMessage.set('Hourly rate must be greater than 0.');
      return;
    }
    if (subjects.length === 0) {
      this.editErrorMessage.set('Please add at least one subject.');
      return;
    }

    this.isSavingProfile.set(true);
    this.editErrorMessage.set(null);

    this.tutorService.updateTutor(t.id, {
      name,
      bio,
      hourlyRate,
      subjects
    }).subscribe({
      next: () => {
        this.tutor.update(curr => curr ? {
          ...curr,
          name,
          bio,
          hourlyRate,
          subjects
        } : null);
        this.isSavingProfile.set(false);
        this.isEditProfileOpen.set(false);
        this.successMessage.set('Profile and subjects updated successfully.');
      },
      error: (err: { error?: { detail?: string; title?: string } }) => {
        this.isSavingProfile.set(false);
        const detail = err?.error?.detail || err?.error?.title || 'Failed to update profile.';
        this.editErrorMessage.set(detail);
      }
    });
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(part => part.charAt(0).toUpperCase())
      .slice(0, 2)
      .join('');
  }
}
