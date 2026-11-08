import { TestBed } from '@angular/core/testing';
import { TimeProvider } from '../../../src/app/core/services/time-provider.service';

describe('TimeProvider', () => {
  let service: TimeProvider;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TimeProvider);
  });

  const startTimeStr = '2026-11-10T12:00:00.000Z';
  const startTime = new Date(startTimeStr);

  it('Given_MoreThan12HoursBeforeStart_When_CheckingFreeCancellation_Then_ReturnsTrue', () => {
    // 24 hours before start
    const now = new Date(startTime.getTime() - 24 * 60 * 60 * 1000);

    expect(service.isFreeCancellation(startTimeStr, now)).toBe(true);
    expect(service.isLateCancellation(startTimeStr, now)).toBe(false);
  });

  it('Given_Exactly12HoursBeforeStart_When_CheckingFreeCancellation_Then_ReturnsTrue', () => {
    // Exactly 12 hours before start (boundary: utcNow <= deadline)
    const now = new Date(startTime.getTime() - 12 * 60 * 60 * 1000);

    expect(service.isFreeCancellation(startTimeStr, now)).toBe(true);
    expect(service.isLateCancellation(startTimeStr, now)).toBe(false);
  });

  it('Given_12HoursAndOneSecondBeforeStart_When_CheckingFreeCancellation_Then_ReturnsTrue', () => {
    const now = new Date(startTime.getTime() - 12 * 60 * 60 * 1000 - 1000);

    expect(service.isFreeCancellation(startTimeStr, now)).toBe(true);
    expect(service.isLateCancellation(startTimeStr, now)).toBe(false);
  });

  it('Given_11Hours59MinutesBeforeStart_When_CheckingCancellation_Then_IdentifiesAsLate', () => {
    // 11 hours 59 minutes before start (1 second after deadline)
    const now = new Date(startTime.getTime() - (12 * 60 * 60 * 1000 - 1000));

    expect(service.isFreeCancellation(startTimeStr, now)).toBe(false);
    expect(service.isLateCancellation(startTimeStr, now)).toBe(true);
  });

  it('Given_TwoHoursBeforeStart_When_CheckingCancellation_Then_IdentifiesAsLate', () => {
    const now = new Date(startTime.getTime() - 2 * 60 * 60 * 1000);

    expect(service.isFreeCancellation(startTimeStr, now)).toBe(false);
    expect(service.isLateCancellation(startTimeStr, now)).toBe(true);
  });

  it('Given_TimeAfterStart_When_CheckingCancellation_Then_IdentifiesAsLate', () => {
    const now = new Date(startTime.getTime() + 10 * 60 * 1000);

    expect(service.isFreeCancellation(startTimeStr, now)).toBe(false);
    expect(service.isLateCancellation(startTimeStr, now)).toBe(true);
  });

  it('Given_TimeBeforeStart_When_CheckingEligibleToComplete_Then_ReturnsFalse', () => {
    const now = new Date(startTime.getTime() - 5 * 60 * 1000);

    expect(service.isEligibleToComplete(startTimeStr, now)).toBe(false);
  });

  it('Given_TimeAtOrAfterStart_When_CheckingEligibleToComplete_Then_ReturnsTrue', () => {
    const exactStart = new Date(startTime.getTime());
    const afterStart = new Date(startTime.getTime() + 60 * 60 * 1000);

    expect(service.isEligibleToComplete(startTimeStr, exactStart)).toBe(true);
    expect(service.isEligibleToComplete(startTimeStr, afterStart)).toBe(true);
  });

  it('Given_StartTimeAndCurrentTime_When_CalculatingHoursUntilStart_Then_ComputesCorrectDifference', () => {
    const now = new Date(startTime.getTime() - 6.5 * 60 * 60 * 1000);

    expect(service.hoursUntilStart(startTimeStr, now)).toBeCloseTo(6.5);
  });
});

