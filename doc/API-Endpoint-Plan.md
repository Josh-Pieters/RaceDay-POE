# RaceDay – API Endpoint Plan

This document lists every REST endpoint the RaceDay API will expose in Part 2.
Role values: **None** (public/no auth), **Any** (any logged-in user), **Organiser**, **Participant**.

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or a Participant. | None | `{ "fullName", "email", "password", "role" }` | 201 Created – returns new user id and role. 400 Bad Request – validation failed. 409 Conflict – email already registered. |
| POST | /api/auth/login | Authenticates a user and issues a JWT access token. | None | `{ "email", "password" }` | 200 OK – returns JWT token, user id, role. 401 Unauthorized – invalid credentials. |

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's own account and profile details. | Any | None | 200 OK – user + profile object. 401 Unauthorized – no/invalid token. |
| PUT | /api/users/me | Updates the logged-in user's basic account details (name, phone). | Any | `{ "fullName", "phoneNumber" }` | 200 OK – updated user object. 400 Bad Request – validation failed. |
| PUT | /api/users/me/profile | Creates or updates the logged-in Participant's extended profile (DOB, emergency contact, medical notes). | Participant | `{ "dateOfBirth", "gender", "emergencyContactName", "emergencyContactPhone", "medicalNotes" }` | 200 OK – updated profile object. 400 Bad Request – validation failed. |
| GET | /api/users/{id} | Returns a specific user's public details. Used by an Organiser to view a participant linked to an enrolment. | Organiser | None | 200 OK – user object. 404 Not Found – user does not exist. |

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events, with optional filters (province, date range, keyword). Used by Participants to browse events. | None | None | 200 OK – array of event summaries. |
| GET | /api/events/{id} | Returns full details of a single event, including its categories. | None | None | 200 OK – event object with nested categories. 404 Not Found – event does not exist. |
| POST | /api/events | Creates a new event. The logged-in Organiser becomes the event's OrganiserID. | Organiser | `{ "eventName", "description", "eventDate", "startTime", "location", "province" }` | 201 Created – new event object. 400 Bad Request – validation failed. |
| PUT | /api/events/{id} | Updates an event's details. Only the Organiser who owns the event may edit it. | Organiser | `{ "eventName", "description", "eventDate", "startTime", "location", "province" }` | 200 OK – updated event object. 403 Forbidden – not the owning Organiser. 404 Not Found. |
| DELETE | /api/events/{id} | Deletes an event and its categories. Only the owning Organiser may delete it. | Organiser | None | 204 No Content. 403 Forbidden – not the owning Organiser. 404 Not Found. |
| GET | /api/events/{id}/enrolments | Returns every enrolment across all categories for an event. Used by the Organiser to see who has entered. | Organiser | None | 200 OK – array of enrolments with participant and category info. 403 Forbidden – not the owning Organiser. |

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories (e.g. 5km, 10km, 21.1km) for a given event. | None | None | 200 OK – array of categories. 404 Not Found – event does not exist. |
| POST | /api/events/{eventId}/categories | Adds a new category to an event. Only the owning Organiser may add categories. | Organiser | `{ "categoryName", "distanceKM", "price", "maxParticipants" }` | 201 Created – new category object. 403 Forbidden – not the owning Organiser. 404 Not Found – event does not exist. |
| PUT | /api/categories/{id} | Updates a category's details. Only the owning Organiser may edit it. | Organiser | `{ "categoryName", "distanceKM", "price", "maxParticipants" }` | 200 OK – updated category object. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Deletes a category, provided it has no active enrolments. Only the owning Organiser may delete it. | Organiser | None | 204 No Content. 403 Forbidden. 409 Conflict – category has active enrolments. |

## 5. Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrolments | Enters the logged-in Participant into a category for an event. | Participant | `{ }` (participant id taken from token) | 201 Created – enrolment record with status "Confirmed". 400 Bad Request. 409 Conflict – already enrolled, or category full. |
| GET | /api/enrolments/me | Returns all of the logged-in Participant's own enrolments, past and upcoming. | Participant | None | 200 OK – array of enrolments with event/category info. |
| DELETE | /api/enrolments/{id} | Cancels the logged-in Participant's own enrolment. | Participant | None | 204 No Content. 403 Forbidden – not the owning Participant. 404 Not Found. |

## 6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Captures a finish-line result for a participant's enrolment. Only the Organiser of that event may capture results. | Organiser | `{ "finishTime", "overallPosition", "categoryPosition" }` | 201 Created – new result object. 403 Forbidden – not the owning Organiser. 404 Not Found – enrolment does not exist. |
| PUT | /api/results/{id} | Corrects a previously captured result. Only the capturing Organiser may edit it. | Organiser | `{ "finishTime", "overallPosition", "categoryPosition" }` | 200 OK – updated result object. 403 Forbidden. 404 Not Found. |
| GET | /api/results/me | Returns the logged-in Participant's full personal results history across all events. | Participant | None | 200 OK – array of results with event/category context. |
| GET | /api/events/{id}/results | Returns the full results list for an event (e.g. for a public leaderboard). | None | None | 200 OK – array of results ordered by category and position. 404 Not Found – event does not exist. |

---
