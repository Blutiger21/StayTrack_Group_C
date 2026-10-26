# StayTrack Mobile App

The student-facing mobile application for the StayTrack student accommodation management system, developed with **Flutter** and **Supabase**.

## Features

- **Authentication**: OTP-based student email login via Supabase Auth.
- **Home Dashboard**: Quick overview of student tenancy, lease details, assigned room, and latest announcements.
- **Payments (`feature/student-payments`)**:
  - View rent payment history with real-time status chips (`Pending`, `Verified`, `Rejected`).
  - Outstanding balance calculation based on tenancy rental amount and unpaid periods.
  - Proof of payment upload with document picker (PDF, PNG, JPG).
  - Secure storage upload to Supabase `proof-of-payment` bucket.
- **Maintenance**: Submit repair requests with photo attachments and track progress.
- **Announcements**: Residence-wide broadcasts from accommodation management.

## Directory Structure

```
lib/
├── core/
│   ├── constants/       # App-wide constants (Supabase credentials, bucket names)
│   ├── models/          # Data models (Payment, Tenant, Room, MaintenanceRequest, etc.)
│   ├── router/          # GoRouter configuration & route guards
│   ├── services/        # Supabase client service layer
│   └── theme/           # Design system palette, typography, and component themes
├── features/
│   ├── announcements/   # Announcements feed & providers
│   ├── auth/            # Sign-in and OTP verification screens
│   ├── home/            # Main shell, dashboard, and navigation bar
│   ├── maintenance/     # Maintenance ticket submission and tracking
│   └── payments/        # Payments overview, history list, and proof upload screen
└── main.dart            # Application entrypoint with Riverpod ProviderScope
```

## Getting Started

### Prerequisites

- [Flutter SDK](https://docs.flutter.dev/get-started/install) (Dart 3.x)
- Android Studio / Xcode for device simulation

### Installation

```bash
# Get dependencies
flutter pub get

# Run on an emulator or connected device
flutter run
```
