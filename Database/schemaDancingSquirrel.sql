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
