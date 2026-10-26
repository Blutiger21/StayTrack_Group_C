-- =============================================================================
-- STAYTRACK DATABASE SCHEMA
-- PostgreSQL / Supabase
-- Covers: Roles, Profiles, Property, Room, Tenant, Technician, Payment,
--         Maintenance Requests, Announcements, Storage Buckets, and RLS Policies.
-- =============================================================================

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- 1. ROLES TABLE
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.role (
    role_id SERIAL PRIMARY KEY,
    role_name TEXT NOT NULL UNIQUE
);

-- Seed system roles
INSERT INTO public.role (role_id, role_name) VALUES
    (1, 'management'),
    (2, 'technician'),
    (3, 'student')
ON CONFLICT (role_id) DO UPDATE SET role_name = EXCLUDED.role_name;

-- -----------------------------------------------------------------------------
-- 2. USER PROFILES TABLE
-- Linked 1-to-1 with Supabase Auth (auth.users)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.profiles (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    role_id INT NOT NULL REFERENCES public.role(role_id) DEFAULT 3,
    full_name TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL UNIQUE,
    phone TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- -----------------------------------------------------------------------------
-- 3. PROPERTY TABLE
-- Student accommodation residences/buildings
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.property (
    property_id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    address TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- -----------------------------------------------------------------------------
-- 4. ROOM TABLE
-- Rooms within a property (Single, Sharing, etc.)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.room (
    room_id SERIAL PRIMARY KEY,
    property_id INT NOT NULL REFERENCES public.property(property_id) ON DELETE CASCADE,
    room_number TEXT NOT NULL,
    room_type TEXT NOT NULL DEFAULT 'Single',
    status TEXT NOT NULL DEFAULT 'vacant', -- 'vacant', 'occupied'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_property_room UNIQUE (property_id, room_number)
);

-- -----------------------------------------------------------------------------
-- 5. TENANT TABLE
-- Active or past student tenancy lease record
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.tenant (
    tenant_id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    room_id INT NOT NULL REFERENCES public.room(room_id) ON DELETE RESTRICT,
    student_number TEXT NOT NULL,
    payment_reference TEXT NOT NULL UNIQUE,
    rental_amount NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    lease_start DATE NOT NULL,
    lease_end DATE NOT NULL,
    date_registered TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    status TEXT NOT NULL DEFAULT 'active' -- 'active', 'inactive'
);

-- -----------------------------------------------------------------------------
-- 6. TECHNICIAN TABLE
-- Links maintenance technicians to user profiles
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.technician (
    technician_id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL UNIQUE REFERENCES public.profiles(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- -----------------------------------------------------------------------------
-- 7. PAYMENT TABLE
-- Rent payment records and student proof of payment uploads
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.payment (
    payment_id SERIAL PRIMARY KEY,
    tenant_id INT NOT NULL REFERENCES public.tenant(tenant_id) ON DELETE CASCADE,
    verified_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
    amount NUMERIC(10, 2) NOT NULL,
    payment_date DATE NOT NULL DEFAULT CURRENT_DATE,
    period_month DATE NOT NULL, -- First day of month e.g. '2026-10-01'
    reference TEXT NOT NULL,
    proof_url TEXT,             -- Relative path: 'proof-of-payment/file.pdf'
    status TEXT NOT NULL DEFAULT 'pending', -- 'pending', 'approved', 'rejected'
    verified_at TIMESTAMPTZ
);

-- -----------------------------------------------------------------------------
-- 8. MAINTENANCE REQUEST TABLE
-- Repair tickets submitted by students and assigned to technicians
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.maintenance_request (
    request_id SERIAL PRIMARY KEY,
    tenant_id INT NOT NULL REFERENCES public.tenant(tenant_id) ON DELETE CASCADE,
    technician_id INT REFERENCES public.technician(technician_id) ON DELETE SET NULL,
    title TEXT NOT NULL,
    description TEXT NOT NULL,
    photo_url TEXT,             -- Relative path: 'maintenance-photos/file.jpg'
    status TEXT NOT NULL DEFAULT 'pending', -- 'pending', 'in_progress', 'completed'
    date_submitted TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    date_completed TIMESTAMPTZ
);

-- -----------------------------------------------------------------------------
-- 9. ANNOUNCEMENT TABLE
-- Broadcast messages posted by management
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.announcement (
    announcement_id SERIAL PRIMARY KEY,
    created_by UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    message TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- INDEXES FOR PERFORMANCE
-- =============================================================================
CREATE INDEX IF NOT EXISTS idx_tenant_user_id ON public.tenant(user_id);
CREATE INDEX IF NOT EXISTS idx_tenant_room_id ON public.tenant(room_id);
CREATE INDEX IF NOT EXISTS idx_tenant_payment_ref ON public.tenant(payment_reference);
CREATE INDEX IF NOT EXISTS idx_payment_tenant_id ON public.payment(tenant_id);
CREATE INDEX IF NOT EXISTS idx_payment_status ON public.payment(status);
CREATE INDEX IF NOT EXISTS idx_maintenance_tenant_id ON public.maintenance_request(tenant_id);
CREATE INDEX IF NOT EXISTS idx_maintenance_tech_id ON public.maintenance_request(technician_id);
CREATE INDEX IF NOT EXISTS idx_maintenance_status ON public.maintenance_request(status);
CREATE INDEX IF NOT EXISTS idx_announcement_created_at ON public.announcement(created_at DESC);

-- =============================================================================
-- STORAGE BUCKETS (Supabase Storage)
-- =============================================================================
INSERT INTO storage.buckets (id, name, public)
VALUES ('proof-of-payment', 'proof-of-payment', false)
ON CONFLICT (id) DO NOTHING;

INSERT INTO storage.buckets (id, name, public)
VALUES ('maintenance-photos', 'maintenance-photos', false)
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- STORAGE ROW LEVEL SECURITY (RLS) POLICIES
-- =============================================================================
CREATE POLICY "Allow authenticated uploads to proof-of-payment"
ON storage.objects FOR INSERT TO authenticated
WITH CHECK (bucket_id = 'proof-of-payment');

CREATE POLICY "Allow authenticated read on proof-of-payment"
ON storage.objects FOR SELECT TO authenticated
USING (bucket_id = 'proof-of-payment');

CREATE POLICY "Allow authenticated uploads to maintenance-photos"
ON storage.objects FOR INSERT TO authenticated
WITH CHECK (bucket_id = 'maintenance-photos');

CREATE POLICY "Allow authenticated read on maintenance-photos"
ON storage.objects FOR SELECT TO authenticated
USING (bucket_id = 'maintenance-photos');

-- =============================================================================
-- TABLE ROW LEVEL SECURITY (RLS) POLICIES
-- =============================================================================
ALTER TABLE public.role ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.property ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.room ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.tenant ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.technician ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.payment ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.maintenance_request ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.announcement ENABLE ROW LEVEL SECURITY;

-- 1. Role policies
CREATE POLICY "Allow authenticated users to read roles"
ON public.role FOR SELECT TO authenticated USING (true);

-- 2. Profiles policies
CREATE POLICY "Allow authenticated users to read profiles"
ON public.profiles FOR SELECT TO authenticated USING (true);

CREATE POLICY "Allow users to update own profile"
ON public.profiles FOR UPDATE TO authenticated USING (auth.uid() = id);

CREATE POLICY "Allow insert profile on auth registration"
ON public.profiles FOR INSERT TO authenticated WITH CHECK (true);

-- 3. Property & Room policies
CREATE POLICY "Allow authenticated users to read properties"
ON public.property FOR SELECT TO authenticated USING (true);

CREATE POLICY "Allow authenticated management to insert/update property"
ON public.property FOR ALL TO authenticated USING (true);

CREATE POLICY "Allow authenticated users to read rooms"
ON public.room FOR SELECT TO authenticated USING (true);

CREATE POLICY "Allow authenticated management to manage rooms"
ON public.room FOR ALL TO authenticated USING (true);

-- 4. Tenant policies
CREATE POLICY "Allow authenticated users to read tenants"
ON public.tenant FOR SELECT TO authenticated USING (true);

CREATE POLICY "Allow management to insert/update tenants"
ON public.tenant FOR ALL TO authenticated USING (true);

-- 5. Technician policies
CREATE POLICY "Allow authenticated users to read technicians"
ON public.technician FOR SELECT TO authenticated USING (true);

CREATE POLICY "Allow management to manage technicians"
ON public.technician FOR ALL TO authenticated USING (true);

-- 6. Payment policies
CREATE POLICY "Allow authenticated users to read payments"
ON public.payment FOR SELECT TO authenticated USING (true);

CREATE POLICY "Allow authenticated students to insert payments"
ON public.payment FOR INSERT TO authenticated WITH CHECK (true);

CREATE POLICY "Allow management to update payments"
ON public.payment FOR UPDATE TO authenticated USING (true);

-- 7. Maintenance Request policies
CREATE POLICY "Allow authenticated users to read maintenance requests"
ON public.maintenance_request FOR SELECT TO authenticated USING (true);

CREATE POLICY "Allow authenticated students to submit maintenance requests"
ON public.maintenance_request FOR INSERT TO authenticated WITH CHECK (true);

CREATE POLICY "Allow management and technicians to update maintenance requests"
ON public.maintenance_request FOR UPDATE TO authenticated USING (true);

-- 8. Announcement policies
CREATE POLICY "Allow authenticated users to read announcements"
ON public.announcement FOR SELECT TO authenticated USING (true);

CREATE POLICY "Allow management to insert announcements"
ON public.announcement FOR INSERT TO authenticated WITH CHECK (true);

-- =============================================================================
-- SAMPLE SEED DATA (For Demo Testing)
-- =============================================================================
INSERT INTO public.property (property_id, name, address) VALUES
    (1, 'Campus Vista Residence', '12 University Road, Student Precinct'),
    (2, 'The Oaks Student Lodge', '45 College Avenue, West Campus')
ON CONFLICT (property_id) DO NOTHING;

INSERT INTO public.room (room_id, property_id, room_number, room_type, status) VALUES
    (1, 1, 'A101', 'Single Ensuite', 'occupied'),
    (2, 1, 'A102', 'Single Standard', 'vacant'),
    (3, 1, 'B201', 'Double Sharing', 'vacant'),
    (4, 2, 'R01', 'Single Studio', 'vacant'),
    (5, 2, 'R02', 'Single Standard', 'vacant')
ON CONFLICT (room_id) DO NOTHING;
