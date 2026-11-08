import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AvailabilityEditorComponent } from '../../../../../src/app/features/tutors/components/availability-editor/availability-editor.component';
import { TutorService } from '../../../../../src/app/core/services/tutor.service';
import { TutorDetailsResponse } from '../../../../../src/app/core/models/tutor.models';

describe('AvailabilityEditorComponent', () => {
  let component: AvailabilityEditorComponent;
  let fixture: ComponentFixture<AvailabilityEditorComponent>;
  let tutorService: TutorService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AvailabilityEditorComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        TutorService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AvailabilityEditorComponent);
    component = fixture.componentInstance;
    tutorService = TestBed.inject(TutorService);
    component.userContext.setRole('tutor');
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('Given_StartEqualsEnd_When_ValidatingNewSlot_Then_ReturnsError', () => {
    const error = component.validateNewSlot('2026-11-04T10:00:00Z', '2026-11-04T10:00:00Z');
    expect(error).toBe('Start time must be strictly before end time.');
  });

  it('Given_StartAfterEnd_When_ValidatingNewSlot_Then_ReturnsError', () => {
    const error = component.validateNewSlot('2026-11-04T12:00:00Z', '2026-11-04T10:00:00Z');
    expect(error).toBe('Start time must be strictly before end time.');
  });

  it('Given_OverlappingInterval_When_ValidatingNewSlot_Then_DetectsCollisionAndRejects', () => {
    component.slots.set([
      { start: '2026-11-04T10:00:00Z', end: '2026-11-04T12:00:00Z' }
    ]);

    // Overlapping: 11:00 to 13:00
    const error = component.validateNewSlot('2026-11-04T11:00:00Z', '2026-11-04T13:00:00Z');
    expect(error).toContain('Slot overlaps with existing interval');
  });

  it('Given_ValidNonOverlappingInterval_When_ValidatingNewSlot_Then_Passes', () => {
    component.slots.set([
      { start: '2026-11-04T10:00:00Z', end: '2026-11-04T12:00:00Z' }
    ]);

    // Non-overlapping: 14:00 to 16:00
    const error = component.validateNewSlot('2026-11-04T14:00:00Z', '2026-11-04T16:00:00Z');
    expect(error).toBeNull();
  });

  it('Given_ValidInputs_When_AddingSlot_Then_SlotIsAddedAndSorted', () => {
    component.slots.set([]);
    component.newStart = '2026-11-05T14:00';
    component.newEnd = '2026-11-05T16:00';

    component.addSlot();

    expect(component.slots().length).toBe(1);
    expect(component.validationError()).toBeNull();
  });

  it('Given_ExistingSlots_When_RemovingSlot_Then_SlotIsDeleted', () => {
    component.slots.set([
      { start: '2026-11-04T10:00:00Z', end: '2026-11-04T12:00:00Z' },
      { start: '2026-11-04T14:00:00Z', end: '2026-11-04T16:00:00Z' }
    ]);

    component.removeSlot(0);

    expect(component.slots().length).toBe(1);
    expect(component.slots()[0].start).toBe('2026-11-04T14:00:00Z');
  });

  it('Given_TutorId_When_SavingAvailability_Then_CallsTutorService', () => {
    component.tutorId = 'tutor-123';
    component.slots.set([
      { start: '2026-11-04T10:00:00Z', end: '2026-11-04T12:00:00Z' }
    ]);

    const updateSpy = vi.spyOn(tutorService, 'updateAvailability').mockReturnValue(of(undefined as unknown as void));

    component.saveAvailability();

    expect(updateSpy).toHaveBeenCalledWith('tutor-123', {
      availability: component.slots()
    });
    expect(component.successMessage()).toContain('updated and saved successfully');
  });

  describe('Student Role Restrictions', () => {
    beforeEach(() => {
      component.userContext.setRole('student');
      component.isEmbedded = false;
      fixture.detectChanges();
    });

    it('Given_StudentRole_When_CheckingCanEdit_Then_ReturnsFalse', () => {
      expect(component.canEdit()).toBe(false);
    });

    it('Given_StudentRole_When_AttemptingToAddSlot_Then_BlocksAndSetsValidationError', () => {
      component.slots.set([]);
      component.newStart = '2026-11-05T14:00';
      component.newEnd = '2026-11-05T16:00';

      component.addSlot();

      expect(component.slots().length).toBe(0);
      expect(component.validationError()).toContain('Students cannot add availability intervals');
    });

    it('Given_StudentRole_When_AttemptingToRemoveSlot_Then_SlotIsPreserved', () => {
      component.slots.set([{ start: '2026-11-04T10:00:00Z', end: '2026-11-04T12:00:00Z' }]);

      component.removeSlot(0);

      expect(component.slots().length).toBe(1);
    });

    it('Given_StudentRole_When_AttemptingToSave_Then_BlocksAndSetsErrorMessage', () => {
      component.tutorId = 'tutor-123';
      const updateSpy = vi.spyOn(tutorService, 'updateAvailability');

      component.saveAvailability();

      expect(updateSpy).not.toHaveBeenCalled();
      expect(component.errorMessage()).toContain('Students cannot update or save tutor availability');
    });

    it('Given_StudentRole_When_Rendering_Then_RendersReadonlyNoticeAndHidesActionButtons', () => {
      component.tutorId = 'tutor-123';
      component.slots.set([{ start: '2026-11-04T10:00:00Z', end: '2026-11-04T12:00:00Z' }]);
      fixture.detectChanges();

      const readonlyNotice = fixture.nativeElement.querySelector('[data-testid="student-readonly-notice"]');
      const readonlyCard = fixture.nativeElement.querySelector('[data-testid="student-readonly-card"]');
      const saveBtn = fixture.nativeElement.querySelector('.btn-save-header');
      const deleteBtn = fixture.nativeElement.querySelector('[data-testid="delete-slot-btn"]');

      expect(readonlyNotice).not.toBeNull();
      expect(readonlyCard).not.toBeNull();
      expect(saveBtn).toBeNull();
      expect(deleteBtn).toBeNull();
    });
  });
});
