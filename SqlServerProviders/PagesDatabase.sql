
DROP PROCEDURE IF EXISTS `testPagesDatabase` ;
CREATE PROCEDURE `testPagesDatabase` ()
BEGIN
  DECLARE i INT DEFAULT -1;
  START TRANSACTION;
	CREATE TABLE `Namespace` (
		`Name` VARCHAR(100) NOT NULL,
		`DefaultPage` VARCHAR(200),
		CONSTRAINT `PK_Namespace` PRIMARY KEY (`Name`)
	);

	CREATE TABLE `Category`(
		`Name` VARCHAR(100) NOT NULL,
		`Namespace` VARCHAR(100) NOT NULL,
		CONSTRAINT `PK_Category` PRIMARY KEY (`Name`, `Namespace`)
	);

	CREATE TABLE `Page` (
		`Name` VARCHAR(200) NOT NULL,
		`Namespace` VARCHAR(100) NOT NULL,
		`CreationDateTime` DATETIME NOT NULL,
		CONSTRAINT `PK_Page` PRIMARY KEY (`Name`, `Namespace`)
	);

	-- Deleting/Renaming/Moving a page requires manually updating the binding
	CREATE TABLE `CategoryBinding` (
		`Namespace` VARCHAR(100) NOT NULL,
		`Category` VARCHAR(100) NOT NULL,
		`Page` VARCHAR(200) NOT NULL,
		CONSTRAINT `PK_CategoryBinding` PRIMARY KEY (`Namespace`, `Page`, `Category`)
	);

	CREATE TABLE `PageContent` (
		`Page` VARCHAR(200) NOT NULL,
		`Namespace` VARCHAR(100) NOT NULL,
		`Revision` SMALLINT NOT NULL,
		`Title` VARCHAR(200) NOT NULL,
		`User` VARCHAR(100) NOT NULL,
		`LastModified` DATETIME NOT NULL,
		`Comment` VARCHAR(300),
		`Content` LONGTEXT NOT NULL,
		`Description` VARCHAR(200),
		CONSTRAINT `PK_PageContent` PRIMARY KEY (`Page`, `Namespace`, `Revision`)
	);

	CREATE TABLE `PageKeyword` (
		`Page` VARCHAR(200) NOT NULL,
		`Namespace` VARCHAR(100) NOT NULL,
		`Revision` SMALLINT NOT NULL,
		`Keyword` VARCHAR(50) NOT NULL,
		CONSTRAINT `PK_PageKeyword` PRIMARY KEY (`Page`, `Namespace`, `Revision`, `Keyword`)
	);

	CREATE TABLE `Message` (
		`Page` VARCHAR(200) NOT NULL,
		`Namespace` VARCHAR(100) NOT NULL,
		`Id` SMALLINT NOT NULL,
		`Parent` SMALLINT,
		`Username` VARCHAR(100) NOT NULL,
		`Subject` VARCHAR(200) NOT NULL,
		`DateTime` DATETIME NOT NULL,
		`Body` LONGTEXT NOT NULL,
		CONSTRAINT `PK_Message` PRIMARY KEY (`Page`, `Namespace`, `Id`)
	);

	CREATE TABLE `NavigationPath` (
		`Name` VARCHAR(100) NOT NULL,
		`Namespace` VARCHAR(100) NOT NULL,
		`Page` VARCHAR(200) NOT NULL,
		`Number` SMALLINT NOT NULL,
		CONSTRAINT `PK_NavigationPath` PRIMARY KEY (`Name`, `Namespace`, `Page`)
	);

	CREATE TABLE `Snippet` (
		`Name` VARCHAR(200) NOT NULL,
		`Content` LONGTEXT NOT NULL,
		CONSTRAINT `PK_Snippet` PRIMARY KEY (`Name`)
	);

	CREATE TABLE `ContentTemplate` (
		`Name` VARCHAR(200) NOT NULL,
		`Content` LONGTEXT NOT NULL,
		CONSTRAINT `PK_ContentTemplate` PRIMARY KEY (`Name`)
	);

	CREATE TABLE `IndexDocument` (
		`Id` INT NOT NULL,
		`Name` VARCHAR(200) NOT NULL,
		`Title` VARCHAR(200) NOT NULL,
		`TypeTag` VARCHAR(10) NOT NULL,
		`DateTime` DATETIME NOT NULL,
		CONSTRAINT `UQ_IndexDocument` UNIQUE INDEX (`Name`),
		CONSTRAINT `PK_IndexDocument` PRIMARY KEY (`Id`)
	);

	CREATE TABLE `IndexWord` (
		`Id` INT NOT NULL,
		`Text` VARCHAR(200) NOT NULL,
		CONSTRAINT `UQ_IndexWord` UNIQUE INDEX (`Text`),
		CONSTRAINT `PK_IndexWord` PRIMARY KEY (`Id`)
	);

	CREATE TABLE `IndexWordMapping` (
		`Word` INT NOT NULL,
		`Document` INT NOT NULL,
		`FirstCharIndex` SMALLINT NOT NULL,
		`WordIndex` SMALLINT NOT NULL,
		`Location` TINYINT NOT NULL,
		CONSTRAINT `PK_IndexWordMapping` PRIMARY KEY (`Word`, `Document`, `FirstCharIndex`, `WordIndex`, `Location`)
	);
  COMMIT;
  START TRANSACTION;
	ALTER TABLE `Category` ADD CONSTRAINT `FK_Category_Namespace` FOREIGN KEY (`Namespace`) 
		REFERENCES `Namespace`(`Name`) ON DELETE CASCADE ON UPDATE CASCADE;
		
	ALTER TABLE `Page` ADD CONSTRAINT `FK_Page_Namespace` FOREIGN KEY (`Namespace`) 
		REFERENCES `Namespace`(`Name`) ON DELETE CASCADE ON UPDATE CASCADE;
		
	ALTER TABLE `CategoryBinding` ADD CONSTRAINT `FK_CategoryBinding_Namespace` FOREIGN KEY (`Namespace`) 
		REFERENCES `Namespace`(`Name`);
	ALTER TABLE `CategoryBinding` ADD CONSTRAINT `FK_CategoryBinding_Category` FOREIGN KEY (`Category`, `Namespace`) 
		REFERENCES `Category`(`Name`, `Namespace`) ON DELETE CASCADE ON UPDATE CASCADE;
	ALTER TABLE `CategoryBinding` ADD CONSTRAINT `FK_CategoryBinding_Page` FOREIGN KEY (`Page`, `Namespace`) 
		REFERENCES `Page`(`Name`, `Namespace`) ON DELETE NO ACTION ON UPDATE NO ACTION;
		
	ALTER TABLE `PageContent` ADD CONSTRAINT `FK_PageContent_Page` FOREIGN KEY (`Page`, `Namespace`) 
		REFERENCES `Page`(`Name`, `Namespace`) ON DELETE CASCADE ON UPDATE CASCADE;

	ALTER TABLE `PageKeyword` ADD CONSTRAINT `FK_PageKeyword_PageContent` FOREIGN KEY (`Page`, `Namespace`, `Revision`) 
		REFERENCES `PageContent`(`Page`, `Namespace`, `Revision`) ON DELETE CASCADE ON UPDATE CASCADE;

	ALTER TABLE `Message` ADD CONSTRAINT `FK_Message_Page` FOREIGN KEY (`Page`, `Namespace`) 
		REFERENCES `Page`(`Name`, `Namespace`) ON DELETE CASCADE ON UPDATE CASCADE;

	ALTER TABLE `NavigationPath` ADD CONSTRAINT `FK_NavigationPath_Page` FOREIGN KEY (`Page`, `Namespace`) 
		REFERENCES `Page`(`Name`, `Namespace`) ON DELETE CASCADE ON UPDATE CASCADE;

	ALTER TABLE `IndexWordMapping` ADD CONSTRAINT `FK_IndexWordMapping_IndexWord` FOREIGN KEY (`Word`) 
		REFERENCES `IndexWord`(`Id`) ON DELETE CASCADE ON UPDATE CASCADE;
	ALTER TABLE `IndexWordMapping` ADD CONSTRAINT `FK_IndexWordMapping_IndexDocument` FOREIGN KEY (`Document`) 
		REFERENCES `IndexDocument`(`Id`) ON DELETE CASCADE ON UPDATE CASCADE;
  COMMIT;
  SELECT COUNT(*) INTO i FROM INFORMATION_SCHEMA.TABLES WHERE table_name like 'Version';
  IF i = 0 THEN
  CREATE TABLE `Version` (
    `Component` VARCHAR(100) NOT NULL,
    `Version` INT NOT NULL,
    CONSTRAINT `PK_Version` PRIMARY KEY (`Component`)
  );
  END IF;
  SELECT COUNT(`Version`) INTO i FROM `Version` WHERE `Component` like 'Pages';
  IF i = 0 THEN
	INSERT INTO `Version` (`Component`, `Version`) VALUES ('Pages', 3001);
  END IF;
  SELECT COUNT(`Name`) INTO i FROM `Namespace` WHERE `Name` like '';
  IF i = 0 THEN
	INSERT INTO `Namespace` (`Name`, `DefaultPage`) VALUES ('', NULL);
  END IF;
END ;
CALL `testPagesDatabase`();
DROP PROCEDURE IF EXISTS `testPagesDatabase`;
