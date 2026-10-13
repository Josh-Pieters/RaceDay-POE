# RaceDay
 
RaceDay is a full-stack, cloud-aware event management system built for the South African road running, walking and cycling community. It replaces the paper-based registration, spreadsheets and disconnected communication that most community races currently rely on.
 
The platform lets **Event Organisers** create and manage events, categories and participant results, while **Participants** can browse upcoming events, enter a category, track their personal results history, and prepare for race day.
 
This repository is built progressively across three parts:

## Roles
 
| Role | Capabilities |
|---|---|
| **Organiser** | Create, edit and delete events; manage event categories; capture participant results; view all enrolments for their events. |
| **Participant** | Create an account; browse events; enter an event by selecting a category; view their own enrolments; track their personal results. |
 
Role-based access is planned at the API level in Part 2 (`[Authorize(Roles = "...")]`) and will be reflected consistently in the MVC interface in Part 3.
 
## Part 1 Contents (`/docs`)
 
| File | Description |
|---|---|
| `docs/RaceDay ERD.png` | Entity Relationship Diagram — 6 entities (Users, UserProfiles, Events, Categories, Enrolments, Results) with primary keys, foreign keys and cardinality. |
| `docs/API-Endpoint-Plan.md` | Full endpoint plan covering Authentication, User Profile, Events, Categories, Event Enrolments and Results. |
| `docs/RaceDay SQL Database.sql` | SQL Server script that creates the full schema (matching the ERD exactly) and seeds it with sample data: 2 Organisers, 2 Participants, 3 Events, categories per event, and sample enrolments/results. |
 
### Database design notes
- `Enrolments` is the junction table that resolves the many-to-many relationship between Participants and Categories (and, by extension, Events).
- `UserProfiles` holds optional extended participant details (date of birth, emergency contact, medical notes) in a 1-to-1 relationship with `Users`, keeping the core `Users` table lean for both roles.
- `Results` has a 1-to-0..1 relationship with `Enrolments` — a result only exists once an Organiser captures it after race day.
## CI/CD
 
<img width="1334" height="537" alt="image" src="https://github.com/user-attachments/assets/eb312838-6a37-46e3-bdfb-5b47879530f0" />

 
## Video Walkthrough
 
An unlisted YouTube video walking through the planning documents, the ERD decisions, the endpoint plan choices, and a live run of the SQL script in SSMS:
 
` https://youtu.be/bJ63v8W_EZc `
 
## Repository Structure
 
```
RaceDay/
├── .github/
│   └── workflows/
│       └── validate-structure.yml
├── docs/
│   ├── ERD.png
│   ├── API-Endpoint-Plan.md
│   └── RaceDay-Database.sql
└── README.md
```
 
