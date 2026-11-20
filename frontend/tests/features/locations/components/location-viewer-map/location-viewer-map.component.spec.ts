import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { LocationViewerMapComponent } from '../../../../../src/app/features/locations/components/location-viewer-map/location-viewer-map.component';
import { LocationMapComponent } from '../../../../../src/app/features/locations/components/location-map/location-map.component';

describe('LocationViewerMapComponent', () => {
  let fixture: ComponentFixture<LocationViewerMapComponent>;

  const setup = (address: string | null): void => {
    fixture = TestBed.createComponent(LocationViewerMapComponent);
    fixture.componentRef.setInput('latitude', 47.17);
    fixture.componentRef.setInput('longitude', 27.57);
    fixture.componentRef.setInput('address', address);
    fixture.detectChanges();
  };

  it('Given_Location_When_Rendering_Then_ShowsReadOnlyPinAndAddress', () => {
    setup('Corp C, Bulevardul Carol I 11, Iasi - sala C309');

    const map = fixture.debugElement.query(By.directive(LocationMapComponent)).componentInstance as LocationMapComponent;
    expect(map.latitude()).toBe(47.17);
    expect(map.longitude()).toBe(27.57);
    expect(map.editable()).toBe(false);
    expect(fixture.nativeElement.querySelector('[data-testid="viewer-address"]')?.textContent).toBe(
      'Corp C, Bulevardul Carol I 11, Iasi - sala C309'
    );
  });

  it('Given_NoAddress_When_Rendering_Then_ShowsOnlyThePin', () => {
    setup(null);

    expect(fixture.nativeElement.querySelector('app-location-map')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="viewer-address"]')).toBeNull();
  });
});
