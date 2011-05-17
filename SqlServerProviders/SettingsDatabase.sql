
CREATE TABLE `Setting` (
	`Name` VARCHAR(100) NOT NULL,
	`Value` VARCHAR(4000) NOT NULL,
	CONSTRAINT `PK_Setting` PRIMARY KEY (`Name`)
);

CREATE TABLE `Log` (
	`Id` INT NOT NULL AUTO_INCREMENT,
	`DateTime` DATETIME NOT NULL,
	`EntryType` CHAR NOT NULL,
	`User` VARCHAR(100) NOT NULL,
	`Message` VARCHAR(4000) NOT NULL,
	CONSTRAINT `PK_Log` PRIMARY KEY (`Id`)
);

CREATE TABLE `MetaDataItem` (
	`Name` VARCHAR(100) NOT NULL,
	`Tag` VARCHAR(100) NOT NULL,
	`Data` VARCHAR(4000) NOT NULL,
	CONSTRAINT `PK_MetaDataItem` PRIMARY KEY (`Name`, `Tag`)
);

CREATE TABLE `RecentChange` (
	`Id` int NOT NULL AUTO_INCREMENT,
	`Page` VARCHAR(200) NOT NULL,
	`Title` VARCHAR(200) NOT NULL,
	`MessageSubject` VARCHAR(200),
	`DateTime` DATETIME NOT NULL,
	`User` VARCHAR(100) NOT NULL,
	`Change` CHAR NOT NULL,
	`Description` VARCHAR(4000),
	CONSTRAINT `PK_RecentChange` PRIMARY KEY (`Id`)
);

CREATE TABLE `PluginAssembly` (
	`Name` VARCHAR(100) NOT NULL,
	`Assembly` LONGBLOB NOT NULL,
	CONSTRAINT `PK_PluginAssembly` PRIMARY KEY (`Name`)
);

CREATE TABLE `PluginStatus` (
	`Name` VARCHAR(150) NOT NULL,
	`Enabled` BOOL NOT NULL,
	`Configuration` VARCHAR(4000) NOT NULL,
	CONSTRAINT `PK_PluginStatus` PRIMARY KEY (`Name`)
);

CREATE TABLE `OutgoingLink` (
	`Source` VARCHAR(100) NOT NULL,
	`Destination` VARCHAR(100) NOT NULL,
	CONSTRAINT `PK_OutgoingLink` PRIMARY KEY (`Source`, `Destination`)
);

CREATE TABLE `AclEntry` (
	`Resource` VARCHAR(200) NOT NULL,
	`Action` VARCHAR(50) NOT NULL,
	`Subject` VARCHAR(100) NOT NULL,
	`Value` CHAR NOT NULL,
	CONSTRAINT `PK_AclEntry` PRIMARY KEY (`Resource`, `Action`, `Subject`)
);

DROP PROCEDURE IF EXISTS `testSettingsDatabase` ;
CREATE PROCEDURE `testSettingsDatabase` ()
BEGIN
  DECLARE i INT DEFAULT -1;
  SELECT COUNT(*) INTO i FROM INFORMATION_SCHEMA.TABLES WHERE table_name like 'Version';
  IF i = 0 THEN
  CREATE TABLE `Version` (
    `Component` VARCHAR(100) NOT NULL,
    `Version` INT NOT NULL,
    CONSTRAINT `PK_Version` PRIMARY KEY (`Component`)
  );
  END IF;
  SELECT COUNT(`Version`) INTO i FROM `Version` WHERE `Component` like 'Settings';
  IF i = 0 THEN
	INSERT INTO `Version` (`Component`, `Version`) VALUES ('Settings', 3000);
  END IF;
END ;
CALL `testSettingsDatabase`();
DROP PROCEDURE IF EXISTS `testSettingsDatabase`;
