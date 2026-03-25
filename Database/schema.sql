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
