import { TestBed } from '@angular/core/testing';
import { ConcurrencyConflictDialogComponent } from './concurrency-conflict-dialog.component';

describe('ConcurrencyConflictDialogComponent', () => {
  it('Given_OpenDialog_When_ClickingRefresh_Then_EmitsRefresh', () => {
    // jsdom does not implement the native dialog API.
    HTMLDialogElement.prototype.showModal = vi.fn();
    const fixture = TestBed.createComponent(ConcurrencyConflictDialogComponent);
    const refresh = vi.fn();
    fixture.componentInstance.refresh.subscribe(refresh);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('button').click();

    expect(HTMLDialogElement.prototype.showModal).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('You have not been booked');
    expect(refresh).toHaveBeenCalledOnce();
  });
});
