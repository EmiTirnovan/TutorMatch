import { Routes } from '@angular/router';
import { TutorRegisterComponent } from './features/tutors/components/tutor-register/tutor-register.component';
import { TutorProfileComponent } from './features/tutors/components/tutor-profile/tutor-profile.component';
import { AvailabilityEditorComponent } from './features/tutors/components/availability-editor/availability-editor.component';
import { SearchTutorsComponent } from './features/tutors/components/search-tutors/search-tutors.component';
import { MySessionsComponent } from './features/sessions/components/my-sessions/my-sessions.component';

export const routes: Routes = [
  { path: '', redirectTo: 'tutors/register', pathMatch: 'full' },
  { path: 'tutors/register', component: TutorRegisterComponent },
  { path: 'tutors/search', component: SearchTutorsComponent },
  { path: 'tutors/:id', component: TutorProfileComponent },
  { path: 'tutors/:id/availability', component: AvailabilityEditorComponent },
  { path: 'sessions', component: MySessionsComponent },
  { path: 'sessions/my', redirectTo: 'sessions' },
  { path: 'my-sessions', redirectTo: 'sessions' },
  { path: '**', redirectTo: 'tutors/register' }
];
