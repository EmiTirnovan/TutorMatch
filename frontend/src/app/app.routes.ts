import { Routes } from '@angular/router';
import { TutorRegisterComponent } from './features/tutors/components/tutor-register/tutor-register.component';
import { TutorProfileComponent } from './features/tutors/components/tutor-profile/tutor-profile.component';
import { AvailabilityEditorComponent } from './features/tutors/components/availability-editor/availability-editor.component';
import { SearchTutorsComponent } from './features/tutors/components/search-tutors/search-tutors.component';
import { MySessionsComponent } from './features/sessions/components/my-sessions/my-sessions.component';
import { LoginComponent } from './features/auth/components/login/login.component';
import { RegisterComponent } from './features/auth/components/register/register.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { EditProfileComponent } from './features/tutors/components/edit-profile/edit-profile.component';

export const routes: Routes = [
  { path: '', redirectTo: 'tutors/search', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'tutors/search', component: SearchTutorsComponent },
  { path: 'profile', component: EditProfileComponent, canActivate: [authGuard] },
  { path: 'profile/edit', component: EditProfileComponent, canActivate: [authGuard] },
  {
    path: 'availability',
    component: AvailabilityEditorComponent,
    canActivate: [authGuard, roleGuard(['tutor'])],
  },
  { path: 'tutors/register', component: TutorRegisterComponent, canActivate: [authGuard] },
  { path: 'tutors/:id', component: TutorProfileComponent },
  {
    path: 'tutors/:id/availability',
    component: AvailabilityEditorComponent,
    canActivate: [authGuard, roleGuard(['tutor'])],
  },
  { path: 'sessions', component: MySessionsComponent, canActivate: [authGuard] },
  { path: 'sessions/my', redirectTo: 'sessions' },
  { path: 'my-sessions', redirectTo: 'sessions' },
  { path: '**', redirectTo: 'tutors/register' },
];
