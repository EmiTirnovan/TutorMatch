export interface AuthUser {
  id: string;
  email: string;
  fullName: string;
  role: 'Student' | 'Tutor' | string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  fullName: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  role: string;
}

export interface GoogleAuthRequest {
  idToken?: string;
  credential?: string;
  role?: string;
}
