import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TutorService } from '../../../../core/services/tutor.service';
import { DateRangeDto } from '../../../../core/models/date-range.dto';
import { TutorDetailsResponse } from '../../../../core/models/tutor.models';

@Component({
  selector: 'app-availability-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './availability-editor.component.html',
  styleUrl: './availability-editor.component.css'
})
export class AvailabilityEditorComponent implements OnInit {
  private readonly tutorService = inject(TutorService);
  private readonly route = inject(ActivatedRoute);

  @Input() tutorId: string = '';
  @Input() isEmbedded: boolean = false;

  readonly slots = signal<DateRangeDto[]>([]);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly validationError = signal<string | null>(null);

  // New slot form state
  newStart: string = '';
  newEnd: string = '';

  ngOnInit(): void {
    if (!this.tutorId) {
      const routeId = this.route.snapshot.paramMap.get('id');
      if (routeId) {
        this.tutorId = routeId;
      }
    }

    if (this.tutorId && !this.isEmbedded) {
      this.loadTutorAvailability();
    } else {
      this.setDefaultSlotTimes();
    }
  }

  loadTutorAvailability(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.tutorService.getTutorById(this.tutorId).subscribe({
      next: (tutor: TutorDetailsResponse) => {
        this.slots.set(tutor.availability || []);
        this.isLoading.set(false);
        this.setDefaultSlotTimes();
      },
      error: () => {
        this.errorMessage.set('Could not load tutor availability. Please verify the tutor ID.');
        this.isLoading.set(false);
        this.setDefaultSlotTimes();
      }
    });
  }

  setDefaultSlotTimes(): void {
    const now = new Date();
    // Round to tomorrow 10:00
    const tomorrow = new Date(now);
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(10, 0, 0, 0);

    const tomorrowEnd = new Date(tomorrow);
    tomorrowEnd.setHours(12, 0, 0, 0);

    this.newStart = this.formatDateTimeLocal(tomorrow);
    this.newEnd = this.formatDateTimeLocal(tomorrowEnd);
  }

  formatDateTimeLocal(date: Date): string {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  validateNewSlot(startIso: string, endIso: string): string | null {
    if (!startIso || !endIso) {
      return 'Please choose both a start date/time and an end date/time.';
    }

    const startDate = new Date(startIso);
    const endDate = new Date(endIso);

    if (isNaN(startDate.getTime()) || isNaN(endDate.getTime())) {
      return 'Invalid date/time format.';
    }

    if (startDate >= endDate) {
      return 'Start time must be strictly before end time.';
    }

    // Check for overlap with existing slots in list
    const currentSlots = this.slots();
    for (let i = 0; i < currentSlots.length; i++) {
      const existing = currentSlots[i];
      const exStart = new Date(existing.start);
      const exEnd = new Date(existing.end);

      // Overlap condition: start < exEnd && end > exStart
      if (startDate < exEnd && endDate > exStart) {
        return `Slot overlaps with existing interval: ${this.formatReadableRange(existing.start, existing.end)}`;
      }
    }

    return null;
  }

  addSlot(): void {
    this.validationError.set(null);
    this.successMessage.set(null);

    const startIso = new Date(this.newStart).toISOString();
    const endIso = new Date(this.newEnd).toISOString();

    const error = this.validateNewSlot(startIso, endIso);
    if (error) {
      this.validationError.set(error);
      return;
    }

    const updated = [...this.slots(), { start: startIso, end: endIso }];
    // Sort chronologically
    updated.sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime());
    this.slots.set(updated);

    // Prepare next suggestion
    const nextStart = new Date(this.newEnd);
    nextStart.setHours(nextStart.getHours() + 1);
    const nextEnd = new Date(nextStart);
    nextEnd.setHours(nextEnd.getHours() + 2);
    this.newStart = this.formatDateTimeLocal(nextStart);
    this.newEnd = this.formatDateTimeLocal(nextEnd);
  }

  removeSlot(index: number): void {
    const updated = [...this.slots()];
    updated.splice(index, 1);
    this.slots.set(updated);
    this.successMessage.set(null);
  }

  saveAvailability(): void {
    if (!this.tutorId) {
      this.errorMessage.set('Cannot save availability: Tutor ID is missing.');
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.tutorService.updateAvailability(this.tutorId, { availability: this.slots() }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.successMessage.set('Availability intervals updated and saved successfully.');
      },
      error: (err: { error?: { detail?: string; title?: string } }) => {
        this.isSaving.set(false);
        const detail = err?.error?.detail || err?.error?.title || 'Failed to update availability on server.';
        this.errorMessage.set(detail);
      }
    });
  }

  formatReadableRange(startStr: string, endStr: string): string {
    const s = new Date(startStr);
    const e = new Date(endStr);
    const dateFormatted = s.toLocaleDateString(undefined, {
      weekday: 'short',
      year: 'numeric',
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
