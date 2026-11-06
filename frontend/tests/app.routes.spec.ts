import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { routes } from '../src/app/app.routes';
import { SearchTutorsComponent } from '../src/app/features/tutors/components/search-tutors/search-tutors.component';
import { TutorProfileComponent } from '../src/app/features/tutors/components/tutor-profile/tutor-profile.component';
import { TutorRegisterComponent } from '../src/app/features/tutors/components/tutor-register/tutor-register.component';

describe('app routes', () => {
  const indexOfPath = (path: string): number => routes.findIndex((route) => route.path === path);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()]
    });
  });

  it('Given_SearchPath_When_ReadingRoutes_Then_MapsToSearchTutorsComponent', () => {
    const route = routes.find((candidate) => candidate.path === 'tutors/search');

    expect(route?.component).toBe(SearchTutorsComponent);
  });

  it('Given_SearchPath_When_ComparingWithProfileRoute_Then_IsDeclaredBeforeIdParameter', () => {
    expect(indexOfPath('tutors/search')).toBeGreaterThan(-1);
    expect(indexOfPath('tutors/search')).toBeLessThan(indexOfPath('tutors/:id'));
  });

  it('Given_SearchUrl_When_Navigating_Then_ActivatesSearchPageInsteadOfTutorProfile', async () => {
    const harness = await RouterTestingHarness.create();

    const component = await harness.navigateByUrl('/tutors/search', SearchTutorsComponent);

    expect(component).toBeInstanceOf(SearchTutorsComponent);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('Discover tutors');
  });

  it('Given_TutorIdUrl_When_Navigating_Then_ActivatesTutorProfile', async () => {
    const harness = await RouterTestingHarness.create();

    const component = await harness.navigateByUrl('/tutors/123e4567-e89b-12d3-a456-426614174000', TutorProfileComponent);

    expect(component).toBeInstanceOf(TutorProfileComponent);
  });

  it('Given_UnknownUrl_When_Navigating_Then_RedirectsToRegisterPage', async () => {
    const harness = await RouterTestingHarness.create();

    const component = await harness.navigateByUrl('/does/not/exist', TutorRegisterComponent);

    expect(component).toBeInstanceOf(TutorRegisterComponent);
  });
});
