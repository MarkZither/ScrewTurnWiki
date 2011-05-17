
DROP PROCEDURE IF EXISTS `updatePagesDatabase3000to3001` ;
CREATE PROCEDURE `updatePagesDatabase3000to3001` ()
BEGIN
	START TRANSACTION;
		DROP TABLE `IndexWordMapping`;
		DROP TABLE `IndexWord`;
		DROP TABLE `IndexDocument`;
	COMMIT;
	START TRANSACTION;	
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
		ALTER TABLE `IndexWordMapping` ADD CONSTRAINT `FK_IndexWordMapping_IndexWord` FOREIGN KEY (`Word`) 
			REFERENCES `IndexWord`(`Id`) ON DELETE CASCADE ON UPDATE CASCADE;
		ALTER TABLE `IndexWordMapping` ADD CONSTRAINT `FK_IndexWordMapping_IndexDocument` FOREIGN KEY (`Document`) 
			REFERENCES `IndexDocument`(`Id`) ON DELETE CASCADE ON UPDATE CASCADE;
	COMMIT;
	START TRANSACTION;
		UPDATE `Version` SET `Version` = 3001 WHERE `Component` like 'Pages';
	COMMIT;
END ;
CALL `updatePagesDatabase3000to3001`();
DROP PROCEDURE IF EXISTS `updatePagesDatabase3000to3001`;
