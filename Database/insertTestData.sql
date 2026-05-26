INSERT INTO DanceType (Name) VALUES
    ('Waltz'),
    ('Tango'),
    ('Ballet'),
    ('Hip Hop'),
    ('Salsa'),
    ('Breakdancing'),
    ('Tap'),
    ('Foxtrot'),
    ('Jive'),
    ('Contemporary');

INSERT INTO Teacher (FirstName, LastName) VALUES
    ('Maria', 'Gonzalez'),
    ('James', 'Chen'),
    ('Priya', 'Sharma'),
    ('Derek', 'Okafor'),
    ('Sofia', 'Petrov'),
    ('Liam', 'Nakamura'),
    ('Amara', 'Diallo'),
    ('Ethan', 'Kowalski'),
    ('Yasmin', 'Hassan'),
    ('Carlos', 'Rivera');

INSERT INTO TrainingRequest (CaretakerType, SquirrelName, OrganizationName, OwnerLastName, OwnerFirstName, Email, Phone, SquirrelId, OnboardUsername, DescriptionOfNeeds, OnboardingDateTimeUnix) VALUES
    (1, 'Fuzzy', NULL, 'Washington', 'George', 'washington@example.com', '4145553892', 6, 'bkrug2', 'Fuzzy currently knows ballroom dance. We need him to learn something more marketable, but still an incremental change from that dance style.', 1778057364),
    (2, 'King Fluff', 'Fluff Inc.', NULL, NULL, 'contact@fluff.com', '4145552789', 3, 'bkrug2', 'Has musical training, but not dance training.', 1778634799),
    (1, 'Tooth', NULL, 'Jefferson', 'Thomas', 'jefferson@example.com', '4145552983', 1, 'bkrug2', 'Would like the squirrel to make singing-like noises while dancing.', 1778477248),
    (1, 'Mickey Mouse, but a squirrel', NULL, 'Adams', 'John', 'adams@example.com', '4145551829', 2, 'bkrug2', 'When my squirrel is dancing, he keeps getting distracted by cheese. Can you help?', 1778543448),
    (1, 'Wiski', NULL, 'Madison', 'James', 'madison@example.com', '12125559821', 4, 'bkrug2', 'Trying to evaluate whether or not ballet is the best future for this squirrel.', 1778700562),
    (1, 'Doctor Squirrel', NULL, 'Monroe', 'James', 'monroe@example.com', NULL, 5, 'bkrug2', 'squirrel keeps trying to take your vitals when it is supposed to be tap dancing. Please Help.', 1778701027),
    (1, 'Quilty', NULL, 'Adams', 'John Q', 'qadams@example.com', '2625557892', 6, 'bkrug2', NULL, 1778701393),
    (1, 'Supreme Squirrel', NULL, 'Jackson', 'Andrew', 'jackson@example.com', '6785559281', 7, 'bkrug2', 'He dances with stylish feet of justice.', 1778702473),
    (1, 'McFlurry', NULL, 'Van Buren', 'Martin', 'vanburen@example.com', NULL, 8, 'bkrug2', 'Needs to learn Tap Dance. Needs multiple lessons per week.', 1778702698),
    (1, 'Just Bob', NULL, 'Harrison', 'William', 'harrison@example.com', '4145558392', NULL, NULL, 'Interested in traditional dance forms', NULL),
    (1, 'Soulmate', NULL, 'Tyler', 'John', 'tyler@example.com', '13135552891', NULL, NULL, 'Needs help with balance', NULL);

INSERT INTO DanceTypeTeacher (DanceTypeId, TeacherId)
SELECT dt.DanceTypeId, t.TeacherId
FROM DanceType dt
JOIN Teacher t ON 1=1
WHERE (dt.Name = 'Tango'        AND t.FirstName = 'Maria'  AND t.LastName = 'Gonzalez')
   OR (dt.Name = 'Ballet'       AND t.FirstName = 'Maria'  AND t.LastName = 'Gonzalez')
   OR (dt.Name = 'Hip Hop'      AND t.FirstName = 'James'  AND t.LastName = 'Chen')
   OR (dt.Name = 'Breakdancing' AND t.FirstName = 'James'  AND t.LastName = 'Chen')
   OR (dt.Name = 'Jive'         AND t.FirstName = 'James'  AND t.LastName = 'Chen')
   OR (dt.Name = 'Salsa'        AND t.FirstName = 'Priya'  AND t.LastName = 'Sharma')
   OR (dt.Name = 'Tap'          AND t.FirstName = 'Priya'  AND t.LastName = 'Sharma')
   OR (dt.Name = 'Contemporary' AND t.FirstName = 'Priya'  AND t.LastName = 'Sharma')
   OR (dt.Name = 'Waltz'        AND t.FirstName = 'Derek'  AND t.LastName = 'Okafor')
   OR (dt.Name = 'Ballet'       AND t.FirstName = 'Derek'  AND t.LastName = 'Okafor')
   OR (dt.Name = 'Foxtrot'      AND t.FirstName = 'Derek'  AND t.LastName = 'Okafor')
   OR (dt.Name = 'Hip Hop'      AND t.FirstName = 'Sofia'  AND t.LastName = 'Petrov')
   OR (dt.Name = 'Breakdancing' AND t.FirstName = 'Sofia'  AND t.LastName = 'Petrov')
   OR (dt.Name = 'Contemporary' AND t.FirstName = 'Sofia'  AND t.LastName = 'Petrov')
   OR (dt.Name = 'Ballet'       AND t.FirstName = 'Liam'   AND t.LastName = 'Nakamura')
   OR (dt.Name = 'Salsa'        AND t.FirstName = 'Liam'   AND t.LastName = 'Nakamura')
   OR (dt.Name = 'Breakdancing' AND t.FirstName = 'Amara'  AND t.LastName = 'Diallo')
   OR (dt.Name = 'Tap'          AND t.FirstName = 'Amara'  AND t.LastName = 'Diallo')
   OR (dt.Name = 'Jive'         AND t.FirstName = 'Amara'  AND t.LastName = 'Diallo')
   OR (dt.Name = 'Waltz'        AND t.FirstName = 'Ethan'  AND t.LastName = 'Kowalski')
   OR (dt.Name = 'Tango'        AND t.FirstName = 'Ethan'  AND t.LastName = 'Kowalski')
   OR (dt.Name = 'Tap'          AND t.FirstName = 'Yasmin' AND t.LastName = 'Hassan')
   OR (dt.Name = 'Foxtrot'      AND t.FirstName = 'Yasmin' AND t.LastName = 'Hassan')
   OR (dt.Name = 'Waltz'        AND t.FirstName = 'Carlos' AND t.LastName = 'Rivera')
   OR (dt.Name = 'Tango'        AND t.FirstName = 'Carlos' AND t.LastName = 'Rivera')
   OR (dt.Name = 'Ballet'       AND t.FirstName = 'Carlos' AND t.LastName = 'Rivera')
   OR (dt.Name = 'Salsa'        AND t.FirstName = 'Carlos' AND t.LastName = 'Rivera')
   OR (dt.Name = 'Tap'          AND t.FirstName = 'Carlos' AND t.LastName = 'Rivera');
