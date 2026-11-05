import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SlotPickerComponent, buildAvailableSlots } from './slot-picker.component';

const NOW = new Date('2026-11-05T12:00:00Z');
const WINDOW = { start: '2026-11-06T10:00:00Z', end: '2026-11-06T12:00:00Z' };
const FIRST_SLOT = { start: '2026-11-06T10:00:00.000Z', end: '2026-11-06T11:00:00.000Z' };
const SECOND_SLOT = { start: '2026-11-06T11:00:00.000Z', end: '2026-11-06T12:00:00.000Z' };

describe('buildAvailableSlots', () => {
  it('Given_TwoHourWindow_When_Building_Then_ReturnsTwoHourlySlots', () => {
    expect(buildAvailableSlots([WINDOW], [], NOW)).toEqual([FIRST_SLOT, SECOND_SLOT]);
  });

  it('Given_BookedSession_When_Building_Then_ExcludesOverlappingSlotOnly', () => {
    const booked = [{ startTime: FIRST_SLOT.start, endTime: FIRST_SLOT.end }];

    expect(buildAvailableSlots([WINDOW], booked, NOW)).toEqual([SECOND_SLOT]);
  });

  it('Given_BookingAcrossSlotBoundary_When_Building_Then_ExcludesBothSlots', () => {
    const booked = [{ startTime: '2026-11-06T10:30:00Z', endTime: '2026-11-06T11:30:00Z' }];

    expect(buildAvailableSlots([WINDOW], booked, NOW)).toEqual([]);
  });

  it('Given_WindowPartlyInPast_When_Building_Then_OmitsPastSlots', () => {
    const now = new Date('2026-11-06T10:30:00Z');

    expect(buildAvailableSlots([WINDOW], [], now)).toEqual([SECOND_SLOT]);
  });

  it('Given_WindowNotDivisibleByAnHour_When_Building_Then_KeepsTheRemainder', () => {
    const window = { start: '2026-11-06T10:00:00Z', end: '2026-11-06T11:30:00Z' };

    expect(buildAvailableSlots([window], [], NOW)[1]).toEqual({
      start: '2026-11-06T11:00:00.000Z',
      end: '2026-11-06T11:30:00.000Z'
    });
  });

  it('Given_ValuesWithoutTimezoneSuffix_When_Building_Then_TreatsThemAsUtc', () => {
    const window = { start: '2026-11-06T10:00:00', end: '2026-11-06T11:00:00' };

    expect(buildAvailableSlots([window], [], NOW)).toEqual([FIRST_SLOT]);
  });
});

describe('SlotPickerComponent', () => {
  let fixture: ComponentFixture<SlotPickerComponent>;

  const buttons = () =>
    Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('.slot-button'));

  beforeEach(() => {
    fixture = TestBed.createComponent(SlotPickerComponent);
    // Far-future window: the component reads the real clock.
    fixture.componentRef.setInput('availability', [
      { start: '2099-11-06T10:00:00Z', end: '2099-11-06T12:00:00Z' }
    ]);
    fixture.detectChanges();
  });

  it('Given_AvailableSlots_When_ClickingOne_Then_SelectsAndMarksIt', () => {
    buttons()[1].click();
    fixture.detectChanges();

    expect(fixture.componentInstance.selected()).toEqual({
      start: '2099-11-06T11:00:00.000Z',
      end: '2099-11-06T12:00:00.000Z'
    });
    expect(buttons()[1].getAttribute('aria-pressed')).toBe('true');
    expect(buttons()[0].getAttribute('aria-pressed')).toBe('false');
  });

  it('Given_NoAvailability_When_Rendering_Then_ShowsEmptyState', () => {
    fixture.componentRef.setInput('availability', []);
    fixture.detectChanges();

    expect(buttons().length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('No available slots');
  });
});
