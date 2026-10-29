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
| Mobile application | Flutter (Riverpod, GoRouter, Supabase Flutter) |
| Web application | ASP.NET Core Razor Pages |
| Shared backend & database | Supabase (Auth, PostgreSQL Database, Storage) |

## Repository Structure

```
/staytrack_mobile     → Flutter mobile application for students
  ├── lib/core        → Constants, theme, router, models, and Supabase services
  └── lib/features    → Feature-first modules (auth, home, payments, maintenance, announcements)
/StayTrack_webApp     → ASP.NET Core web application for management & technicians
/database             → Database schema (PostgreSQL) and Supabase storage configurations
/docs                 → Project documentation (SRS, architecture, progress tracker, diagrams)
README.md
```

## Student Payments Feature (`feature/student-payments`)

The student payments module provides an end-to-end payment workflow within the Flutter mobile application:

* **Data Model (`lib/core/models/payment.dart`)**:
  Represents rent payment records, mapping to the Supabase `payment` table (`payment_id`, `tenant_id`, `amount`, `payment_date`, `period_month`, `reference`, `status`, `proof_url`, `verified_at`).
* **State Management (`lib/features/payments/presentation/providers/payments_provider.dart`)**:
  Manages payment history queries via `paymentsProvider` and handles proof-of-payment file uploads through `UploadNotifier` with comprehensive error and status handling.
* **Payment Overview & History (`lib/features/payments/presentation/screens/payments_screen.dart`)**:
  * Displays an **Outstanding Balance** overview card showing monthly rental and pending payment summaries.
  * Shows a sorted **Payment History** list with payment reference, formatted amounts in ZAR, and status badges (`Pending`, `Verified`, `Rejected`).
  * Pull-to-refresh support and skeleton loading animations.
* **Proof of Payment Upload (`lib/features/payments/presentation/screens/upload_proof_screen.dart`)**:
  * Integrated mobile file picker accepting **PDF, PNG, and JPG/JPEG** documents.
  * Form inputs for payment amount, payment month, and automated student payment reference.
  * Uploads documents directly to the Supabase **`proof-of-payment`** storage bucket and records the entry in the `payment` table.

## Mobile Application Setup

To run the Flutter mobile app locally:

1. Navigate to the mobile app directory:
   ```bash
   cd staytrack_mobile
   ```
2. Install dependencies:
   ```bash
   flutter pub get
   ```
3. Run the app on your preferred emulator or device:
   ```bash
   flutter run
   ```

## Group Members

| Name | Student Number |
|---|---|
| Amatebelle FB | 223040545 |
| Mohapi MA | 222070281 |
| Gongxeka IQ | 223026371 |
| Seleke TM | 223044798 |
| Leeuw GA | 221026798 |
| Tshitangano TD | 223022577 |
| Molelekeng KP | 224020157 |

## Current Project Status

**Phase 2 — System Architecture and Design** (Unit 3)

- [x] Phase 1: Planning, Requirements and Feasibility — submitted
- [x] Phase 2: System Architecture, UML, ERD, Interface Design — completed / in review
- [ ] Phase 3: Development, Integration and Testing — in progress
  - [x] Student Payments feature (history & proof upload)
  - [ ] Maintenance ticketing integration
  - [ ] Announcement feeds

See `/docs` for the current Software Requirements Specification, feasibility study, risk register and progress tracker.

## Important Notes

- No confidential stakeholder information, passwords, API keys or service-role keys are committed to this repository.
- The prototype uses test data rather than real tenant financial or personal information.
