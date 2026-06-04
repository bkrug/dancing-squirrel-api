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

INSERT INTO TrainingRequest (CaretakerType, SquirrelName, OrganizationName, OwnerLastName, OwnerFirstName, Email, Phone, DescriptionOfNeeds) VALUES
    (1, 'Fuzzy', NULL, 'Washington', 'George', 'washington@example.com', '4145553892', 'Fuzzy currently knows ballroom dance. We need him to learn something more marketable, but still an incremental change from that dance style.'),
    (2, 'King Fluff', 'Fluff Inc.', NULL, NULL, 'contact@fluff.com', '4145552789', 'Has musical training, but not dance training.'),
    (1, 'Tooth', NULL, 'Jefferson', 'Thomas', 'jefferson@example.com', '4145552983', 'Would like the squirrel to make singing-like noises while dancing.'),
    (1, 'Mickey Mouse, but a squirrel', NULL, 'Adams', 'John', 'adams@example.com', '4145551829', 'When my squirrel is dancing, he keeps getting distracted by cheese. Can you help?'),
    (1, 'Wiski', NULL, 'Madison', 'James', 'madison@example.com', '12125559821', 'Trying to evaluate whether or not ballet is the best future for this squirrel.'),
    (1, 'Doctor Squirrel', NULL, 'Monroe', 'James', 'monroe@example.com', NULL, 'squirrel keeps trying to take your vitals when it is supposed to be tap dancing. Please Help.'),
    (1, 'Quilty', NULL, 'Adams', 'John Q', 'qadams@example.com', '2625557892', NULL),
    (1, 'Supreme Squirrel', NULL, 'Jackson', 'Andrew', 'jackson@example.com', '6785559281', 'He dances with stylish feet of justice.'),
    (1, 'McFlurry', NULL, 'Van Buren', 'Martin', 'vanburen@example.com', NULL, 'Needs to learn Tap Dance. Needs multiple lessons per week.'),
    (1, 'Just Bob', NULL, 'Harrison', 'William', 'harrison@example.com', '4145558392', 'Interested in traditional dance forms'),
    (1, 'Soulmate', NULL, 'Tyler', 'John', 'tyler@example.com', '13135552891', 'Needs help with balance');

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
