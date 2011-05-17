
DROP PROCEDURE IF EXISTS `testFilesDatabase`;
CREATE PROCEDURE `testFilesDatabase` ()
BEGIN
  DECLARE i INT DEFAULT -1;
  START TRANSACTION;
	CREATE TABLE `Directory` (
		`FullPath` VARCHAR(250) NOT NULL,
		`Parent` VARCHAR(250),
		CONSTRAINT `PK_Directory` PRIMARY KEY (`FullPath`)
	);

	CREATE TABLE `File` (
		`Name` VARCHAR(200) NOT NULL,
		`Directory` VARCHAR(250) NOT NULL,
		`Size` BIGINT NOT NULL,
		`Downloads` INT NOT NULL,
		`LastModified` DATETIME NOT NULL,
		`Data` LONGBLOB NOT NULL,
		CONSTRAINT `PK_File` PRIMARY KEY (`Name`, `Directory`)
	);

	CREATE TABLE `Attachment` (
		`Name` VARCHAR(200) NOT NULL,
		`Page` VARCHAR(200) NOT NULL,
		`Size` BIGINT NOT NULL,
		`Downloads` INT NOT NULL,
		`LastModified` DATETIME NOT NULL,
		`Data` LONGBLOB NOT NULL,
		CONSTRAINT `PK_Attachment` PRIMARY KEY (`Name`, `Page`)
	);
  COMMIT;
  START TRANSACTION;
	ALTER TABLE `File` ADD CONSTRAINT `FK_File_Directory` FOREIGN KEY (`Directory`)
		REFERENCES `Directory`(`FullPath`) ON DELETE CASCADE ON UPDATE CASCADE;
  COMMIT;
  SELECT COUNT(*) INTO i FROM INFORMATION_SCHEMA.TABLES WHERE table_name like 'Version';
  IF i = 0 THEN
  CREATE TABLE `Version` (
    `Component` VARCHAR(100) NOT NULL,
    `Version` INT NOT NULL,
    CONSTRAINT `PK_Version` PRIMARY KEY (`Component`)
  );
  END IF;
  SELECT COUNT(`Version`) INTO i FROM `Version` WHERE `Component` like 'Files';
  IF i = 0 THEN
	INSERT INTO `Version` (`Component`, `Version`) VALUES ('Files', 3000);
  END IF;
  SELECT COUNT(`FullPath`) INTO i FROM `Directory` WHERE `FullPath` like '/';
  IF i = 0 THEN
	INSERT INTO `Directory` (`FullPath`, `Parent`) VALUES ('/', NULL);
  END IF;
END;
CALL `testFilesDatabase`();
DROP PROCEDURE IF EXISTS `testFilesDatabase`;
