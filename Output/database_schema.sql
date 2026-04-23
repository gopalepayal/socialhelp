-- Enum Types
CREATE TYPE user_role AS ENUM ('Admin', 'Donor', 'Organisation');
CREATE TYPE org_status AS ENUM ('Pending', 'Approved', 'Rejected');
CREATE TYPE donation_type AS ENUM ('Money', 'Food', 'Clothes');
CREATE TYPE donation_status AS ENUM ('Pending', 'Approved', 'Completed');

-- 1. Admins Table
CREATE TABLE Admins (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. Donors Table
CREATE TABLE Donors (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Phone VARCHAR(50),
    Address TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 3. Organisations Table
CREATE TABLE Organisations (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Address TEXT,
    Description TEXT,
    Status org_status DEFAULT 'Pending',
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 4. OrganizationVerification Table
CREATE TABLE OrganizationVerification (
    Id SERIAL PRIMARY KEY,
    OrganisationId INT NOT NULL,
    DocumentPath VARCHAR(512) NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_verification_org FOREIGN KEY (OrganisationId) REFERENCES Organisations(Id) ON DELETE CASCADE
);

-- 5. Donations Table
CREATE TABLE Donations (
    Id SERIAL PRIMARY KEY,
    DonorId INT NOT NULL,
    OrganisationId INT NOT NULL,
    DonationType donation_type NOT NULL,
    Amount DECIMAL(12, 2) CHECK (DonationType != 'Money' OR Amount IS NOT NULL),
    Quantity INT CHECK (DonationType = 'Money' OR Quantity IS NOT NULL),
    Description TEXT,
    Status donation_status DEFAULT 'Pending',
    PickupAddress TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_donation_donor FOREIGN KEY (DonorId) REFERENCES Donors(Id) ON DELETE CASCADE,
    CONSTRAINT fk_donation_org FOREIGN KEY (OrganisationId) REFERENCES Organisations(Id) ON DELETE CASCADE
);

-- 6. Ratings Table
CREATE TABLE Ratings (
    Id SERIAL PRIMARY KEY,
    DonorId INT NOT NULL,
    OrganisationId INT NOT NULL,
    Stars INT CHECK (Stars >= 1 AND Stars <= 5),
    Comment TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_rating_donor FOREIGN KEY (DonorId) REFERENCES Donors(Id) ON DELETE CASCADE,
    CONSTRAINT fk_rating_org FOREIGN KEY (OrganisationId) REFERENCES Organisations(Id) ON DELETE CASCADE
);

-- 7. Notifications Table
CREATE TABLE Notifications (
    Id SERIAL PRIMARY KEY,
    TargetId INT NOT NULL,
    Message TEXT NOT NULL,
    IsRead BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 8. ChatMessages Table
CREATE TABLE ChatMessages (
    Id SERIAL PRIMARY KEY,
    SenderId INT NOT NULL,
    ReceiverId INT NOT NULL,
    Message TEXT NOT NULL,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for performance and faster lookups
CREATE INDEX idx_users_email ON Users(Email);
CREATE INDEX idx_donors_userid ON Donors(UserId);
CREATE INDEX idx_organisations_userid ON Organisations(UserId);
CREATE INDEX idx_donations_donorid ON Donations(DonorId);
CREATE INDEX idx_donations_organisationid ON Donations(OrganisationId);
CREATE INDEX idx_organization_verification_org_id ON OrganizationVerification(OrganisationId);
CREATE INDEX idx_ratings_donorid ON Ratings(DonorId);
CREATE INDEX idx_ratings_organisationid ON Ratings(OrganisationId);
CREATE INDEX idx_notifications_userid ON Notifications(UserId);
CREATE INDEX idx_chatmessages_senderid ON ChatMessages(SenderId);
CREATE INDEX idx_chatmessages_receiverid ON ChatMessages(ReceiverId);
