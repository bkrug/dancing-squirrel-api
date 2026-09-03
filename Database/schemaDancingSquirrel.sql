CREATE TABLE Person (
	PersonId INTEGER NOT NULL,
	LastName TEXT NOT NULL,
	FirstName TEXT NOT NULL,
	CONSTRAINT PK_Person PRIMARY KEY (PersonId)
);
CREATE INDEX Person_LastName_IDX ON Person (LastName,FirstName);
CREATE TABLE Organization (
	OrganizationId INTEGER NOT NULL,
	Name TEXT NOT NULL,
	CONSTRAINT PK_Organization PRIMARY KEY (OrganizationId)
);
CREATE TABLE SquirrelOwner (
	SquirrelOwnerId INTEGER NOT NULL,
	PersonId INTEGER,
	OrganizationId INTEGER, PhoneNumber TEXT, Email TEXT,
	CONSTRAINT PK_SquirrelOwner PRIMARY KEY (SquirrelOwnerId),
	CONSTRAINT FK_SquirrelOwner_Person FOREIGN KEY (PersonId) REFERENCES Person(PersonId),
	CONSTRAINT FK_SquirrelOwner_Organization FOREIGN KEY (OrganizationId) REFERENCES Organization(OrganizationId)
	CHECK ((PersonId is null) != (OrganizationId is null))
);
CREATE TABLE Squirrel (
	SquirrelId INTEGER NOT NULL,
	SquirrelOwnerId INTEGER NOT NULL,
	Name TEXT NOT NULL,
	CONSTRAINT PK_Squirrel PRIMARY KEY (SquirrelId),
	CONSTRAINT FK_Squirrel_SquirrelOwner FOREIGN KEY (SquirrelOwnerId) REFERENCES SquirrelOwner(SquirrelOwnerId)
);
CREATE TABLE TrainingRequest (
	TrainingRequestId INTEGER NOT NULL,
	CaretakerType INTEGER NOT NULL,
	SquirrelName TEXT NOT NULL,
	OrganizationName TEXT,
	OwnerLastName TEXT,
	OwnerFirstName TEXT,
	Email TEXT NOT NULL,
	Phone TEXT,
	SquirrelId INTEGER,
	OnboardUsername TEXT,
	DescriptionOfNeeds TEXT,
	OnboardingDateTimeUnix INTEGER,
	CONSTRAINT PK_TrainingRequest PRIMARY KEY (TrainingRequestId),
	CONSTRAINT FK_TrainingRequest_Squirrel FOREIGN KEY (SquirrelId) REFERENCES Squirrel(SquirrelId)
);
CREATE TABLE DanceType (
	DanceTypeId INTEGER NOT NULL,
	Name TEXT NOT NULL,
	CONSTRAINT PK_DanceType PRIMARY KEY (DanceTypeId)
);
CREATE TABLE Teacher (
	TeacherId INTEGER NOT NULL,
	FirstName TEXT NOT NULL,
	LastName TEXT NOT NULL,
	CONSTRAINT PK_Teacher PRIMARY KEY (TeacherId)
);
CREATE INDEX Teacher_LastName_IDX ON Teacher (LastName, FirstName);
CREATE TABLE DanceTypeTeacher (
	DanceTypeId INTEGER NOT NULL,
	TeacherId INTEGER NOT NULL,
	CONSTRAINT PK_DanceTypeTeacher PRIMARY KEY (DanceTypeId, TeacherId),
	CONSTRAINT FK_DanceTypeTeacher_DanceType FOREIGN KEY (DanceTypeId) REFERENCES DanceType(DanceTypeId),
	CONSTRAINT FK_DanceTypeTeacher_Teacher FOREIGN KEY (TeacherId) REFERENCES Teacher(TeacherId)
);
CREATE TABLE SquirrelTeacher (
	SquirrelId INTEGER NOT NULL,
	TeacherId INTEGER NOT NULL,
	CONSTRAINT PK_SquirrelTeacher PRIMARY KEY (SquirrelId, TeacherId),
	CONSTRAINT FK_SquirrelTeacher_Squirrel FOREIGN KEY (SquirrelId) REFERENCES Squirrel(SquirrelId),
	CONSTRAINT FK_SquirrelTeacher_Teacher FOREIGN KEY (TeacherId) REFERENCES Teacher(TeacherId)
);
CREATE TABLE DefaultAvailability (
	DefaultAvailabilityId INTEGER NOT NULL,
	TeacherId INTEGER NOT NULL,
	DayOfWeek INTEGER NOT NULL,
	StartTimeUnix INTEGER NOT NULL,
	EndTimeUnix INTEGER NOT NULL,
	CONSTRAINT PK_DefaultAvailability PRIMARY KEY (DefaultAvailabilityId),
	CONSTRAINT FK_DefaultAvailability_Teacher FOREIGN KEY (TeacherId) REFERENCES Teacher(TeacherId)
);
CREATE INDEX DefaultAvailability_TeacherId_IDX ON DefaultAvailability (TeacherId);
CREATE TABLE RecurringEvent (
	RecurringEventId INTEGER NOT NULL,
	RecurrenceType INTEGER NOT NULL, -- 0 = None (single lesson), 1 = Weekly, etc.
	Description TEXT NOT NULL,
	DaysOfWeek INTEGER, -- NULL when RecurrenceType = None
	StartDateUnix INTEGER NOT NULL, -- Date of event if non-recurring, or first date of a recurring class
	EndDateUnix INTEGER,  -- Last date of a recurring class
	StartTimeUnix INTEGER NOT NULL, -- default start time for a lesson
	EndTimeUnix INTEGER NOT NULL, -- default end time for a lesson
	CONSTRAINT PK_RecurringEvent PRIMARY KEY (RecurringEventId)
);
CREATE TABLE EventInstance (
    EventInstanceId INTEGER NOT NULL,
	RecurringEventId INTEGER NOT NULL,
	Canceled BIT NOT NULL,
	StartDateTimeUnix INTEGER NOT NULL,
	EndDateTimeUnix INTEGER NOT NULL,
	CONSTRAINT PK_EventInstance PRIMARY KEY (EventInstanceId),
	CONSTRAINT FK_EventInstance_RecurringEvent FOREIGN KEY (RecurringEventId) REFERENCES RecurringEvent(RecurringEventId)
);
CREATE INDEX EventInstance_RecurringEventId_IDX ON EventInstance (RecurringEventId);
CREATE TABLE RecurringEventTeacher (
	RecurringEventId INTEGER NOT NULL,
	TeacherId INTEGER NOT NULL,
	CONSTRAINT PK_RecurringEventTeacher PRIMARY KEY (RecurringEventId, TeacherId),
	CONSTRAINT FK_RecurringEventTeacher_RecurringEvent FOREIGN KEY (RecurringEventId) REFERENCES RecurringEvent(RecurringEventId),
	CONSTRAINT FK_RecurringEventTeacher_Teacher FOREIGN KEY (TeacherId) REFERENCES Teacher(TeacherId)
);
CREATE INDEX RecurringEventTeacher_TeacherId_IDX ON RecurringEventTeacher (TeacherId);
CREATE TABLE RecurringEventSquirrel (
	RecurringEventId INTEGER NOT NULL,
	SquirrelId INTEGER NOT NULL,
	CONSTRAINT PK_RecurringEventSquirrel PRIMARY KEY (RecurringEventId, SquirrelId),
	CONSTRAINT FK_RecurringEventSquirrel_RecurringEvent FOREIGN KEY (RecurringEventId) REFERENCES RecurringEvent(RecurringEventId),
	CONSTRAINT FK_RecurringEventSquirrel_Squirrel FOREIGN KEY (SquirrelId) REFERENCES Squirrel(SquirrelId)
);
CREATE INDEX RecurringEventSquirrel_SquirrelId_IDX ON RecurringEventSquirrel (SquirrelId);
