/** Shapes of the Identity API (`/api/auth`, `/api/users`). Dates are ISO 8601 strings. */
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface UserResponse {
  id: string;
  email: string;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
}

export interface LogoutOptions {
  /** Where to send the user after they sign in again (used when a session expires). */
  returnUrl?: string;
}
