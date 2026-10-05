# StayTrack

An integrated prototype for managing student accommodation payments, tenants and maintenance requests — developed for **ITC327W: Work-Integrated Learning** at the Central University of Technology, Free State.

## Project Description

Small student-accommodation providers often manage rent payments, tenant information, maintenance requests and communication manually through WhatsApp, spreadsheets and paper records. StayTrack replaces this with a shared system:

- **Students** use the Flutter mobile app to view their payment reference and history, upload proof of payment, submit and track maintenance requests (with photos), and view announcements.
- **Management** uses the ASP.NET web app to manage tenant records, verify payments, generate monthly payment reports, assign maintenance requests, and post announcements.
- **Maintenance Technician** uses a restricted view within the ASP.NET web app to view assigned requests and mark jobs as complete.
- **Supabase** provides the shared backend: Authentication (OTP-based login for all roles), Database (users, rooms, payments, maintenance requests) and Storage (proof-of-payment images, maintenance photos).

Stakeholder: a real student-accommodation landlord managing 3 properties.

## Tech Stack

| Component | Technology |
|---|---|
| Mobile application | Flutter |
| Web application | ASP.NET |
| Shared backend & database | Supabase (Auth, Database, Storage) |

## Repository Structure

```
/flutter_app          → Flutter mobile prototype
/aspnet_app            → ASP.NET web prototype
/docs                  → SRS, feasibility study, risk register, progress tracker
/docs/diagrams         → Architecture, UML, ERD, wireframes (exported images)
/docs/design-links.md  → Links to original editable design files
README.md
```

## Group Members

| Name 
|---|---|
| Amatebelle FB - 223040545 
| Mohapi MA - 222070281
| Gongxeka IQ - 223026371
| Seleke TM - 223044798
| Leeuw GA - 221026798
| Tshitangano TD - 223022577
| Molelekeng KP - 224020157

## Current Project Status

**Phase 2 — System Architecture and Design** (Unit 3)

- [x] Phase 1: Planning, Requirements and Feasibility — submitted
- [ ] Phase 2: System Architecture, UML, ERD, Interface Design — in progress
- [ ] Phase 3: Development, Integration and Testing — not started

See `/docs` for the current Software Requirements Specification, feasibility study, risk register and progress tracker.

## Important Notes

- No confidential stakeholder information, passwords, API keys or service-role keys are committed to this repository.
- The prototype uses test data rather than real tenant financial or personal information.
