import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { Mock } from 'vitest';
import { ConcurrencyConflictDialogComponent } from '../../../../../src/app/features/bookings/components/concurrency-conflict-dialog/concurrency-conflict-dialog.component';

describe('ConcurrencyConflictDialogComponent', () => {
  let fixture: ComponentFixture<ConcurrencyConflictDialogComponent>;
  let refresh: Mock<() => void>;

  const dialog = (): HTMLElement => fixture.nativeElement.querySelector('[role="alertdialog"]');

  beforeEach(() => {
    fixture = TestBed.createComponent(ConcurrencyConflictDialogComponent);
    document.body.appendChild(fixture.nativeElement);
    refresh = vi.fn<() => void>();
    fixture.componentInstance.refresh.subscribe(refresh);
    fixture.detectChanges();
  });

  afterEach(() => fixture.nativeElement.remove());

  it('Given_OpenDialog_When_Rendered_Then_FocusesAlertDialog', () => {
    expect(document.activeElement).toBe(dialog());
    expect(fixture.nativeElement.textContent).toContain('You have not been booked');
  });

  it('Given_OpenDialog_When_ClickingRefresh_Then_EmitsRefresh', () => {
    fixture.nativeElement.querySelector('button').click();

    expect(refresh).toHaveBeenCalledOnce();
  });

  it('Given_OpenDialog_When_PressingEsc_Then_EmitsRefresh', () => {
    dialog().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(refresh).toHaveBeenCalledOnce();
  });

  it('Given_SingleButton_When_PressingTab_Then_KeepsFocusOnIt', () => {
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    button.focus();

    const tab = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    dialog().dispatchEvent(tab);

    expect(tab.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(button);
  });
});
