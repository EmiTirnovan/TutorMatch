import { Component, computed, input, model } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DateRangeDto } from '../../../../core/models/date-range.dto';
import { SessionResponse } from '../../../../core/models/session.models';

const SLOT_MINUTES = 60;

interface SlotDay {
  date: string;
  slots: DateRangeDto[];
}

// The API stores UTC but SQLite drops the "Z" on read, so a suffix-less value must not be parsed as local time.
function parseUtc(value: string): number {
  return Date.parse(/(Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`);
}

export function buildAvailableSlots(
  availability: DateRangeDto[],
  bookedSessions: Pick<SessionResponse, 'startTime' | 'endTime'>[],
  now: Date
): DateRangeDto[] {
  const booked = bookedSessions.map(s => ({ start: parseUtc(s.startTime), end: parseUtc(s.endTime) }));
  const slots: { start: number; end: number }[] = [];

  for (const window of availability) {
    const windowEnd = parseUtc(window.end);
    let start = parseUtc(window.start);
    while (start < windowEnd) {
      const end = Math.min(start + SLOT_MINUTES * 60_000, windowEnd);
      const isTaken = booked.some(b => start < b.end && end > b.start);
      if (!isTaken && start > now.getTime()) {
        slots.push({ start, end });
      }
      start = end;
    }
  }

  return slots
    .sort((a, b) => a.start - b.start)
    .map(s => ({ start: new Date(s.start).toISOString(), end: new Date(s.end).toISOString() }));
}

@Component({
  selector: 'app-slot-picker',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './slot-picker.component.html',
  styleUrl: './slot-picker.component.css'
})
export class SlotPickerComponent {
  readonly availability = input.required<DateRangeDto[]>();
  readonly bookedSessions = input<SessionResponse[]>([]);
  readonly selected = model<DateRangeDto | null>(null);

  readonly days = computed<SlotDay[]>(() => {
    const days = new Map<string, DateRangeDto[]>();
    for (const slot of buildAvailableSlots(this.availability(), this.bookedSessions(), new Date())) {
      const key = new Date(slot.start).toDateString();
      days.set(key, [...(days.get(key) ?? []), slot]);
    }
    return [...days].map(([date, slots]) => ({ date, slots }));
  });

  isSelected(slot: DateRangeDto): boolean {
    return this.selected()?.start === slot.start;
  }
}
