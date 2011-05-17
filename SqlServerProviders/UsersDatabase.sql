
DROP PROCEDURE IF EXISTS `testUsersDatabase` ;
CREATE PROCEDURE `testUsersDatabase` ()
BEGIN
  DECLARE i INT DEFAULT -1;
  START TRANSACTION;
	  CREATE TABLE `User` (
		`Username` VARCHAR(100) NOT NULL,
		`PasswordHash` VARCHAR(100) NOT NULL,
		`DisplayName` VARCHAR(150),
		`Email` VARCHAR(100) NOT NULL,
		`Active` BOOL NOT NULL,
		`DateTime` DATETIME NOT NULL,
		CONSTRAINT `PK_User` PRIMARY KEY (`Username`)
	);

	CREATE TABLE `UserGroup` (
		`Name` VARCHAR(100) NOT NULL,
		`Description` VARCHAR(150),
		CONSTRAINT `PK_UserGroup` PRIMARY KEY (`Name`)
	);

	CREATE TABLE `UserGroupMembership` (
		`User` VARCHAR(100) NOT NULL,
		`UserGroup` VARCHAR(100) NOT NULL,
		CONSTRAINT `PK_UserGroupMembership` PRIMARY KEY (`User`, `UserGroup`)
	);

	CREATE TABLE `UserData` (
		`User` VARCHAR(100) NOT NULL,
		`Key` VARCHAR(100) NOT NULL,
		`Data` VARCHAR(4000) NOT NULL,	
		CONSTRAINT `PK_UserData` PRIMARY KEY (`User`, `Key`)
	);
  COMMIT;
  START TRANSACTION;
	ALTER TABLE `UserGroupMembership` ADD CONSTRAINT `FK_UserGroupMembership_User` FOREIGN KEY (`User`) 
		REFERENCES `User`(`Username`) ON DELETE CASCADE ON UPDATE CASCADE;
	ALTER TABLE `UserGroupMembership` ADD CONSTRAINT `FK_UserGroupMembership_UserGroup` FOREIGN KEY (`UserGroup`) 
		REFERENCES `UserGroup`(`Name`) ON DELETE CASCADE ON UPDATE CASCADE;
		
	ALTER TABLE `UserData` ADD CONSTRAINT `FK_UserData_User` FOREIGN KEY (`User`) 
		REFERENCES `User`(`Username`) ON DELETE CASCADE ON UPDATE CASCADE;

  COMMIT;
  SELECT COUNT(*) INTO i FROM INFORMATION_SCHEMA.TABLES WHERE table_name like 'Version';
  IF i = 0 THEN
  CREATE TABLE `Version` (
    `Component` VARCHAR(100) NOT NULL,
    `Version` INT NOT NULL,
    CONSTRAINT `PK_Version` PRIMARY KEY (`Component`)
  );
  END IF;
  SELECT COUNT(`Version`) INTO i FROM `Version` WHERE `Component` like 'Users';
  IF i = 0 THEN
	INSERT INTO `Version` (`Component`, `Version`) VALUES ('Users', 3000);
  END IF;
END ;
CALL `testUsersDatabase`();
DROP PROCEDURE IF EXISTS `testUsersDatabase`;
