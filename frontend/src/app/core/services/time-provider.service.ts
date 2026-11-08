import { Injectable } from '@angular/core';

export const TWELVE_HOURS_MS = 12 * 60 * 60 * 1000;

@Injectable({
  providedIn: 'root'
})
export class TimeProvider {
  /**
   * Returns current time as a Date. Can be overridden in unit tests.
   */
  now(): Date {
    return new Date();
  }

  /**
   * Returns true if cancellation is free (>= 12h before start), matching backend TimeProvider rules:
   * deadline = startTime - 12 hours. utcNow <= deadline.
   */
  isFreeCancellation(startTime: string | Date, now: Date = this.now()): boolean {
    const startMs = new Date(startTime).getTime();
    const nowMs = now.getTime();
    const deadlineMs = startMs - TWELVE_HOURS_MS;
    return nowMs <= deadlineMs;
  }

  /**
   * Returns true if cancellation is late (< 12h before start).
   */
  isLateCancellation(startTime: string | Date, now: Date = this.now()): boolean {
    return !this.isFreeCancellation(startTime, now);
  }

  /**
   * Returns true if session can be completed by tutor (utcNow >= startTime).
   */
  isEligibleToComplete(startTime: string | Date, now: Date = this.now()): boolean {
    const startMs = new Date(startTime).getTime();
    const nowMs = now.getTime();
    return nowMs >= startMs;
  }

  /**
   * Returns hours remaining until start time.
   */
  hoursUntilStart(startTime: string | Date, now: Date = this.now()): number {
    const startMs = new Date(startTime).getTime();
    const nowMs = now.getTime();
    return (startMs - nowMs) / (60 * 60 * 1000);
  }
}

