# MermaidFlow — Next.js Integration Guide

> **For the Agent:** This guide covers every endpoint, the exact data shapes, and how each page/component should call the API. Follow it literally — no extra abstractions needed.

---

## Base Setup

**Base URL:** `http://localhost:5209`  
**API prefix:** none (e.g. `POST /auth/login`, not `/api/auth/login`)  
**Auth header:** `Authorization: Bearer <token>`

### `lib/api.ts` — single fetch wrapper

```ts
const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5209";

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const token =
    typeof window !== "undefined" ? localStorage.getItem("token") : null;

  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw Object.assign(new Error(err.title ?? "Request failed"), {
      status: res.status,
      detail: err,
    });
  }

  if (res.status === 204) return undefined as T;
  return res.json();
}
```

### Token storage helpers

```ts
// lib/auth.ts
export const saveTokens = (token: string, refreshToken: string) => {
  localStorage.setItem("token", token);
  localStorage.setItem("refreshToken", refreshToken);
};

export const clearTokens = () => {
  localStorage.removeItem("token");
  localStorage.removeItem("refreshToken");
};

export const getRefreshToken = () =>
  localStorage.getItem("refreshToken") ?? "";
```

---

## Types

```ts
// types/api.ts

export interface AuthResponse {
  token: string;
  refreshToken: string;
  userId: string;
  email: string;
  displayName: string;
  expiresAt: string;           // ISO date string
  refreshTokenExpiresAt: string;
}

export interface UserResponse {
  id: string;
  email: string;
  displayName: string;
  createdAt: string;
}

export interface DocumentResponse {
  id: string;
  title: string;
  content: string;
  userId: string;
  createdAt: string;
  updatedAt: string;
  isPublic: boolean;
  tags: string[];
}

export interface MermaidValidationResponse {
  isValid: boolean;
  errorMessage: string | null;
}
```

---

## Auth

### Register — `POST /auth/register`

```ts
// components/RegisterForm.tsx
import { apiFetch } from "@/lib/api";
import { saveTokens } from "@/lib/auth";
import type { AuthResponse } from "@/types/api";

async function register(email: string, password: string, displayName: string) {
  const data = await apiFetch<AuthResponse>("/auth/register", {
    method: "POST",
    body: JSON.stringify({ email, password, displayName }),
  });
  saveTokens(data.token, data.refreshToken);
  return data;
}
```

### Login — `POST /auth/login`

```ts
async function login(email: string, password: string) {
  const data = await apiFetch<AuthResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
  saveTokens(data.token, data.refreshToken);
  return data;
}
```

### Refresh — `POST /auth/refresh`

Call this when any request returns `401`.

```ts
// lib/auth.ts
export async function refreshAccessToken(): Promise<string> {
  const refreshToken = getRefreshToken();
  const data = await apiFetch<AuthResponse>("/auth/refresh", {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
  saveTokens(data.token, data.refreshToken);
  return data.token;
}
```

### Logout — `POST /auth/logout`

```ts
async function logout() {
  const refreshToken = getRefreshToken();
  await apiFetch("/auth/logout", {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
  clearTokens();
}
```

---

## Users

All `/users` endpoints require `Authorization: Bearer <token>`.

### Get user — `GET /users/{id}`

```ts
import { apiFetch } from "@/lib/api";
import type { UserResponse } from "@/types/api";

const user = await apiFetch<UserResponse>(`/users/${userId}`);
```

### Update user — `PUT /users/{id}`

```ts
const updated = await apiFetch<UserResponse>(`/users/${userId}`, {
  method: "PUT",
  body: JSON.stringify({ email, displayName }),
});
```

### Patch email — `PATCH /users/{id}/email`

```ts
await apiFetch<UserResponse>(`/users/${userId}/email`, {
  method: "PATCH",
  body: JSON.stringify({ email }),
});
```

### Patch display name — `PATCH /users/{id}/display-name`

```ts
await apiFetch<UserResponse>(`/users/${userId}/display-name`, {
  method: "PATCH",
  body: JSON.stringify({ displayName }),
});
```

### Delete user — `DELETE /users/{id}`

```ts
await apiFetch(`/users/${userId}`, { method: "DELETE" });
clearTokens();
```

---

## Documents

### Create — `POST /documents`

```ts
import type { DocumentResponse } from "@/types/api";

const doc = await apiFetch<DocumentResponse>("/documents", {
  method: "POST",
  body: JSON.stringify({
    title,
    content,       // raw markdown / mermaid blocks
    isPublic,
    tags,          // string[]
  }),
});
```

### List user's documents — `GET /documents`

```ts
const docs = await apiFetch<DocumentResponse[]>("/documents");
```

### Get single document — `GET /documents/{id}`

```ts
const doc = await apiFetch<DocumentResponse>(`/documents/${id}`);
// Returns 403 if the document is private and not owned by the current user
```

### Update — `PUT /documents/{id}`

```ts
const updated = await apiFetch<DocumentResponse>(`/documents/${id}`, {
  method: "PUT",
  body: JSON.stringify({ title, content, isPublic, tags }),
});
```

### Delete — `DELETE /documents/{id}`

```ts
await apiFetch(`/documents/${id}`, { method: "DELETE" });
```

### Public documents (no auth) — `GET /documents/public`

```ts
// No token needed — call plain fetch or apiFetch (token header is ignored server-side)
const publicDocs = await apiFetch<DocumentResponse[]>("/documents/public");
```

### Export document — `GET /documents/{id}/export?format=html|png|svg`

```ts
// Returns a file download — do NOT use apiFetch (it expects JSON)
const token = localStorage.getItem("token");
const res = await fetch(
  `${BASE_URL}/documents/${id}/export?format=png`,
  { headers: { Authorization: `Bearer ${token}` } }
);
const blob = await res.blob();
const url = URL.createObjectURL(blob);
// trigger download:
const a = document.createElement("a");
a.href = url;
a.download = `diagram.png`;
a.click();
```

---

## Mermaid

### Render to SVG — `POST /mermaid/render`

Response is raw SVG bytes, not JSON.

```ts
// components/MermaidPreview.tsx
async function renderMermaid(code: string, theme = "default") {
  const token = localStorage.getItem("token");
  const res = await fetch(`${BASE_URL}/mermaid/render`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ code, theme }),
  });
  const svg = await res.text();
  // Inject into DOM:
  document.getElementById("preview")!.innerHTML = svg;
}
```

### Validate — `POST /mermaid/validate`

```ts
import type { MermaidValidationResponse } from "@/types/api";

const result = await apiFetch<MermaidValidationResponse>("/mermaid/validate", {
  method: "POST",
  body: JSON.stringify({ code }),
});

if (!result.isValid) {
  console.error(result.errorMessage);
}
```

### Export as file — `POST /mermaid/export`

```ts
// format: "svg" | "png"
async function exportMermaid(code: string, format: "svg" | "png", theme = "default") {
  const token = localStorage.getItem("token");
  const res = await fetch(`${BASE_URL}/mermaid/export`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ code, theme, format }),
  });
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `diagram.${format}`;
  a.click();
}
```

### Get available themes — `GET /mermaid/themes`

```ts
const themes = await apiFetch<string[]>("/mermaid/themes");
// e.g. ["default", "dark", "forest", "neutral"]
```

---

## Error Handling

The API returns RFC 7807 problem details on errors:

```json
{ "title": "Not Found", "status": 404, "detail": "Document not found" }
```

Errors thrown by `apiFetch` include `.status` and `.detail`. Handle them at the call site:

```ts
try {
  await login(email, password);
} catch (err: any) {
  if (err.status === 401) showError("Invalid credentials");
  else if (err.status === 409) showError("Email already in use");
  else showError("Something went wrong");
}
```

For `401` errors on protected calls, attempt a token refresh then retry once:

```ts
// lib/api.ts — enhanced version
export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  try {
    return await _fetch<T>(path, options);
  } catch (err: any) {
    if (err.status === 401) {
      await refreshAccessToken();           // updates localStorage
      return _fetch<T>(path, options);      // retry with new token
    }
    throw err;
  }
}
```

---

## Environment Variable

```env
# .env.local
NEXT_PUBLIC_API_URL=http://localhost:5209
```

---

## Quick Reference

| Action | Method | Path | Auth |
|---|---|---|---|
| Register | POST | `/auth/register` | No |
| Login | POST | `/auth/login` | No |
| Refresh token | POST | `/auth/refresh` | No |
| Logout | POST | `/auth/logout` | No |
| Get user | GET | `/users/{id}` | Yes |
| Update user | PUT | `/users/{id}` | Yes |
| Patch email | PATCH | `/users/{id}/email` | Yes |
| Patch display name | PATCH | `/users/{id}/display-name` | Yes |
| Delete user | DELETE | `/users/{id}` | Yes |
| Create document | POST | `/documents` | Yes |
| List documents | GET | `/documents` | Yes |
| Get document | GET | `/documents/{id}` | Yes |
| Update document | PUT | `/documents/{id}` | Yes |
| Delete document | DELETE | `/documents/{id}` | Yes |
| Public documents | GET | `/documents/public` | No |
| Export document | GET | `/documents/{id}/export?format=` | Yes |
| Render mermaid | POST | `/mermaid/render` | Yes |
| Validate mermaid | POST | `/mermaid/validate` | Yes |
| Export mermaid | POST | `/mermaid/export` | Yes |
| List themes | GET | `/mermaid/themes` | Yes |
