## ADDED Requirements

### Requirement: Registration prompt behavior is empirically verified
`GET /bff/register`'s use of the non-standard `prompt=register` OIDC parameter SHALL be verified
against the real `shoppinessrealm` Keycloak instance before this capability is considered
complete. Verification alone (without a code change) satisfies this requirement if Keycloak's
hosted theme honors the parameter as intended.

#### Scenario: Verification confirms registration form is shown
- **WHEN** `GET /bff/register` is followed through to Keycloak's hosted login/registration UI
  against the `shoppinessrealm` realm
- **THEN** the observed landing page is the registration form, not the login form
- **THEN** this observation is recorded as the outcome of this requirement's verification

### Requirement: A fallback exists if the prompt parameter is not honored
The system SHALL provide a working fallback for `GET /bff/register` if verification shows that
`prompt=register` does not land the user on Keycloak's registration form. The fallback SHALL
redirect the OIDC challenge to Keycloak's dedicated registration endpoint
(the realm's `protocol/openid-connect/registrations` path) instead of the standard authorize
endpoint, for this flow only.

#### Scenario: Fallback is applied only if verification fails
- **WHEN** verification (per the requirement above) shows `prompt=register` does not produce
  the registration form
- **THEN** `GET /bff/register`'s challenge is redirected through the registrations endpoint
  fallback instead
- **THEN** following `GET /bff/register` through to Keycloak lands on the registration form

#### Scenario: No fallback is applied if verification succeeds
- **WHEN** verification confirms `prompt=register` already produces the registration form
- **THEN** `GET /bff/register`'s implementation is left unchanged
