import { Routes } from '@angular/router';
import { TutorRegisterComponent } from './features/tutors/components/tutor-register/tutor-register.component';
import { TutorProfileComponent } from './features/tutors/components/tutor-profile/tutor-profile.component';
import { AvailabilityEditorComponent } from './features/tutors/components/availability-editor/availability-editor.component';

export const routes: Routes = [
  { path: '', redirectTo: 'tutors/register', pathMatch: 'full' },
  { path: 'tutors/register', component: TutorRegisterComponent },
  { path: 'tutors/:id', component: TutorProfileComponent },
  { path: 'tutors/:id/availability', component: AvailabilityEditorComponent },
  { path: '**', redirectTo: 'tutors/register' }
];
