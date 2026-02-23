# Data Flow: Authentication (Login & Refresh)

This guide explains how Valora handles user authentication, specifically the **Login** and **Token Refresh** flows.

## High-Level Sequence: Login

The following diagram illustrates the lifecycle of a `POST /api/auth/login` request.

```mermaid
sequenceDiagram
    participant Client as Flutter App
    participant API as Valora.Api (AuthEndpoints)
    participant Service as AuthService (Application)
    participant Identity as IdentityService (Infrastructure)
    participant UserMgr as UserManager<ApplicationUser>
    participant Token as TokenGenerator
    participant DB as PostgreSQL

    Client->>API: POST /api/auth/login { email, password }
    API->>Service: LoginAsync(LoginDto)

    Service->>Identity: VerifyPasswordAsync(email, password)
    Identity->>UserMgr: FindByEmailAsync(email)
    UserMgr->>DB: SELECT * FROM "AspNetUsers" WHERE Email = ...
    DB-->>UserMgr: User Entity
    UserMgr->>UserMgr: CheckPasswordAsync(user, password)

    alt Invalid Credentials
        UserMgr-->>Identity: False
        Identity-->>Service: Null
        Service-->>API: Null
        API-->>Client: 401 Unauthorized
    else Valid Credentials
        UserMgr-->>Identity: True
        Identity-->>Service: User Details

        Service->>Service: GenerateAuthResponseAsync(user)
        Service->>Token: GenerateAccessToken(user)
        Token-->>Service: JWT (15 min)

        Service->>Token: GenerateRefreshToken()
        Token-->>Service: Refresh Token (7 days)

        Service->>Identity: SaveRefreshTokenAsync(userId, token)
        Identity->>DB: INSERT INTO "RefreshTokens" (...)

        Service-->>API: AuthResponseDto
        API-->>Client: 200 OK { accessToken, refreshToken }
    end
```

## Detailed Steps

### 1. Request Handling (`Valora.Api`)
- The endpoint `POST /api/auth/login` receives the credentials.
- **Rate Limiting:** This endpoint is strictly rate-limited ("strict" policy) to prevent brute-force attacks.
- It delegates to `IAuthService.LoginAsync`.

### 2. Verification (`Valora.Infrastructure`)
- `IdentityService` retrieves the user by email.
- It uses ASP.NET Core Identity's password hasher to verify the provided password against the stored hash.

### 3. Token Generation (`Valora.Application`)
- Upon successful verification, the system generates two tokens:
  - **Access Token (JWT):** Short-lived (e.g., 15-60 minutes). Contains claims (UserId, Email, Roles) and is signed with the `JWT_SECRET`. Used for authorizing API requests.
  - **Refresh Token:** Long-lived (e.g., 7 days). A random secure string stored in the database. Used to obtain a new Access Token when the old one expires.

### 4. Persistence
- The **Refresh Token** is hashed (SHA-256) and stored in the `RefreshTokens` table linked to the user.
- This allows the server to revoke sessions by deleting the refresh token from the database.

---

## High-Level Sequence: Token Refresh

When the Access Token expires (401), the client uses the Refresh Token to get a new pair.

```mermaid
sequenceDiagram
    participant Client as Flutter App
    participant API as Valora.Api
    participant Service as AuthService
    participant Identity as IdentityService
    participant DB as PostgreSQL

    Client->>API: POST /api/auth/refresh { refreshToken }
    API->>Service: RefreshTokenAsync(token)

    Service->>Identity: ValidateRefreshTokenAsync(token)
    Identity->>DB: SELECT * FROM "RefreshTokens" WHERE Token = Hash(token)

    alt Invalid / Expired / Revoked
        DB-->>Identity: Null
        Identity-->>Service: Null
        Service-->>API: Null
        API-->>Client: 401 Unauthorized (Force Logout)
    else Valid
        DB-->>Identity: RefreshToken Entity
        Identity->>Identity: RevokeToken(oldToken)
        Identity->>DB: UPDATE "RefreshTokens" SET Revoked = Now()

        Identity-->>Service: User Details
        Service->>Service: GenerateAuthResponseAsync(user) (New Access + New Refresh)

        Service->>Identity: SaveRefreshTokenAsync(newToken)
        Identity->>DB: INSERT INTO "RefreshTokens" (...)

        Service-->>API: AuthResponseDto
        API-->>Client: 200 OK { newAccess, newRefresh }
    end
```

### Refresh Token Rotation
- **Security Feature:** We use **Refresh Token Rotation**. When a refresh token is used, it is immediately revoked (or deleted), and a *new* refresh token is issued.
- **Why?** If a refresh token is stolen, the thief can only use it once. If the legitimate user then tries to use the same (now old) refresh token, the server detects the reuse and can revoke *all* tokens for that user (Reuse Detection).
