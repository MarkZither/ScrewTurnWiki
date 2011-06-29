-- MySQL dump 10.13  Distrib 5.1.54, for debian-linux-gnu (x86_64)
--
-- Host: localhost    Database: stw
-- ------------------------------------------------------
-- Server version	5.1.54-1ubuntu4

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `AclEntry`
--

DROP TABLE IF EXISTS `AclEntry`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AclEntry` (
  `Resource` varchar(200) NOT NULL,
  `Action` varchar(50) NOT NULL,
  `Subject` varchar(100) NOT NULL,
  `Value` char(1) NOT NULL,
  PRIMARY KEY (`Resource`,`Action`,`Subject`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AclEntry`
--

LOCK TABLES `AclEntry` WRITE;
/*!40000 ALTER TABLE `AclEntry` DISABLE KEYS */;
INSERT INTO `AclEntry` VALUES ('G','*','G.Administrators','G'),('N.','Crt_Pg','G.Users','G'),('N.','Pst_Disc','G.Users','G'),('N.','Down_Attn','G.Users','G'),('D.(ScrewTurn.Wiki.Plugins.SqlServer.SqlServerFilesStorageProvider)/','Down_Files','G.Users','G'),('N.','Rd_Pg','G.Anonymous','G'),('N.','Rd_Disc','G.Anonymous','G'),('N.','Down_Attn','G.Anonymous','G'),('D.(ScrewTurn.Wiki.Plugins.SqlServer.SqlServerFilesStorageProvider)/','Down_Files','G.Anonymous','G');
/*!40000 ALTER TABLE `AclEntry` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Attachment`
--

DROP TABLE IF EXISTS `Attachment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Attachment` (
  `Name` varchar(200) NOT NULL,
  `Page` varchar(200) NOT NULL,
  `Size` bigint(20) NOT NULL,
  `Downloads` int(11) NOT NULL,
  `LastModified` datetime NOT NULL,
  `Data` longblob NOT NULL,
  PRIMARY KEY (`Name`,`Page`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Attachment`
--

LOCK TABLES `Attachment` WRITE;
/*!40000 ALTER TABLE `Attachment` DISABLE KEYS */;
/*!40000 ALTER TABLE `Attachment` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Category`
--

DROP TABLE IF EXISTS `Category`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Category` (
  `Name` varchar(100) NOT NULL,
  `Namespace` varchar(100) NOT NULL,
  PRIMARY KEY (`Name`,`Namespace`),
  KEY `FK_Category_Namespace` (`Namespace`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Category`
--

LOCK TABLES `Category` WRITE;
/*!40000 ALTER TABLE `Category` DISABLE KEYS */;
/*!40000 ALTER TABLE `Category` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `CategoryBinding`
--

DROP TABLE IF EXISTS `CategoryBinding`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `CategoryBinding` (
  `Namespace` varchar(100) NOT NULL,
  `Category` varchar(100) NOT NULL,
  `Page` varchar(200) NOT NULL,
  PRIMARY KEY (`Namespace`,`Page`,`Category`),
  KEY `FK_CategoryBinding_Category` (`Category`,`Namespace`),
  KEY `FK_CategoryBinding_Page` (`Page`,`Namespace`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `CategoryBinding`
--

LOCK TABLES `CategoryBinding` WRITE;
/*!40000 ALTER TABLE `CategoryBinding` DISABLE KEYS */;
/*!40000 ALTER TABLE `CategoryBinding` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ContentTemplate`
--

DROP TABLE IF EXISTS `ContentTemplate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ContentTemplate` (
  `Name` varchar(200) NOT NULL,
  `Content` longtext NOT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ContentTemplate`
--

LOCK TABLES `ContentTemplate` WRITE;
/*!40000 ALTER TABLE `ContentTemplate` DISABLE KEYS */;
/*!40000 ALTER TABLE `ContentTemplate` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Directory`
--

DROP TABLE IF EXISTS `Directory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Directory` (
  `FullPath` varchar(250) NOT NULL,
  `Parent` varchar(250) DEFAULT NULL,
  PRIMARY KEY (`FullPath`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Directory`
--

LOCK TABLES `Directory` WRITE;
/*!40000 ALTER TABLE `Directory` DISABLE KEYS */;
INSERT INTO `Directory` VALUES ('/',NULL);
/*!40000 ALTER TABLE `Directory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `File`
--

DROP TABLE IF EXISTS `File`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `File` (
  `Name` varchar(200) NOT NULL,
  `Directory` varchar(250) NOT NULL,
  `Size` bigint(20) NOT NULL,
  `Downloads` int(11) NOT NULL,
  `LastModified` datetime NOT NULL,
  `Data` longblob NOT NULL,
  PRIMARY KEY (`Name`,`Directory`),
  KEY `FK_File_Directory` (`Directory`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `File`
--

LOCK TABLES `File` WRITE;
/*!40000 ALTER TABLE `File` DISABLE KEYS */;
/*!40000 ALTER TABLE `File` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `IndexDocument`
--

DROP TABLE IF EXISTS `IndexDocument`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `IndexDocument` (
  `Id` int(11) NOT NULL,
  `Name` varchar(200) NOT NULL,
  `Title` varchar(200) NOT NULL,
  `TypeTag` varchar(10) NOT NULL,
  `DateTime` datetime NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_IndexDocument` (`Name`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;


--
-- Table structure for table `IndexWord`
--

DROP TABLE IF EXISTS `IndexWord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `IndexWord` (
  `Id` int(11) NOT NULL,
  `Text` varchar(200) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_IndexWord` (`Text`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `IndexWord`
--

LOCK TABLES `IndexWord` WRITE;
/*!40000 ALTER TABLE `IndexWord` DISABLE KEYS */;
/*!40000 ALTER TABLE `IndexWord` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `IndexWordMapping`
--

DROP TABLE IF EXISTS `IndexWordMapping`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `IndexWordMapping` (
  `Word` int(11) NOT NULL,
  `Document` int(11) NOT NULL,
  `FirstCharIndex` smallint(6) NOT NULL,
  `WordIndex` smallint(6) NOT NULL,
  `Location` tinyint(4) NOT NULL,
  PRIMARY KEY (`Word`,`Document`,`FirstCharIndex`,`WordIndex`,`Location`),
  KEY `FK_IndexWordMapping_IndexDocument` (`Document`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `IndexWordMapping`
--

LOCK TABLES `IndexWordMapping` WRITE;
/*!40000 ALTER TABLE `IndexWordMapping` DISABLE KEYS */;
/*!40000 ALTER TABLE `IndexWordMapping` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Log`
--

DROP TABLE IF EXISTS `Log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Log` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `DateTime` datetime NOT NULL,
  `EntryType` char(1) NOT NULL,
  `User` varchar(100) NOT NULL,
  `Message` varchar(4000) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=MyISAM AUTO_INCREMENT=478 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Log`
--

LOCK TABLES `Log` WRITE;
/*!40000 ALTER TABLE `Log` DISABLE KEYS */;
/*!40000 ALTER TABLE `Log` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Message`
--

DROP TABLE IF EXISTS `Message`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Message` (
  `Page` varchar(200) NOT NULL,
  `Namespace` varchar(100) NOT NULL,
  `Id` smallint(6) NOT NULL,
  `Parent` smallint(6) DEFAULT NULL,
  `Username` varchar(100) NOT NULL,
  `Subject` varchar(200) NOT NULL,
  `DateTime` datetime NOT NULL,
  `Body` longtext NOT NULL,
  PRIMARY KEY (`Page`,`Namespace`,`Id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Message`
--

LOCK TABLES `Message` WRITE;
/*!40000 ALTER TABLE `Message` DISABLE KEYS */;
/*!40000 ALTER TABLE `Message` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `MetaDataItem`
--

DROP TABLE IF EXISTS `MetaDataItem`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `MetaDataItem` (
  `Name` varchar(100) NOT NULL,
  `Tag` varchar(100) NOT NULL,
  `Data` varchar(4000) NOT NULL,
  PRIMARY KEY (`Name`,`Tag`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `MetaDataItem`
--

LOCK TABLES `MetaDataItem` WRITE;
/*!40000 ALTER TABLE `MetaDataItem` DISABLE KEYS */;
INSERT INTO `MetaDataItem` VALUES ('AccountActivationMessage','','Hi ##USERNAME## and welcome to ##WIKITITLE##!\nYou must activate your new ##WIKITITLE## Account within 24 hours, following the link below.\n\n##ACTIVATIONLINK##\n\nIf you have any trouble, please contact us at our Email address, ##EMAILADDRESS## .\n\nThank you.\n\nBest regards,\nThe ##WIKITITLE## Team.'),('EditNotice','','Please \'\'\'do not\'\'\' include contents covered by copyright without the explicit permission of the Author. Always preview the result before saving.{BR}\nIf you are having trouble, please visit the [http://www.screwturn.eu/Help.ashx|Help section] at the [http://www.screwturn.eu|ScrewTurn Wiki Website].'),('Footer','','<p class=\"small\">[http://www.screwturn.eu|ScrewTurn Wiki] version {WIKIVERSION}. Some of the icons created by [http://www.famfamfam.com|FamFamFam].</p>'),('Header','','<div style=\"float: right;\">Welcome {USERNAME}, you are in: {NAMESPACEDROPDOWN} &bull; {LOGINLOGOUT}</div><h1>{WIKITITLE}</h1>'),('PasswordResetProcedureMessage','','Hi ##USERNAME##!\nYour can change your password following the instructions you will see at this link:\n    ##LINK##\n\nIf you have any trouble, please contact us at our Email address, ##EMAILADDRESS## .\n\nThank you.\n\nBest regards,\nThe ##WIKITITLE## Team.'),('Sidebar','','<div style=\"float: right;\">\n<a href=\"RSS.aspx\" title=\"Update notifications for {WIKITITLE} (RSS 2.0)\"><img src=\"{THEMEPATH}Images/RSS.png\" alt=\"RSS\" /></a>\n<a href=\"RSS.aspx?Discuss=1\" title=\"Update notifications for {WIKITITLE} Discussions (RSS 2.0)\"><img src=\"{THEMEPATH}Images/RSS-Discussion.png\" alt=\"RSS\" /></a></div>\n====Navigation====\n* \'\'\'[Main_Page|Main Page]\'\'\'\n\n* [RandPage.aspx|Random Page]\n* [Edit.aspx|Create a new Page]\n* [AllPages.aspx|All Pages]\n* [Category.aspx|Categories]\n* [NavPath.aspx|Navigation Paths]\n\n* [AdminHome.aspx|Administration]\n* [Upload.aspx|File Management]\n\n* [Register.aspx|Create Account]\n\n<small>\'\'\'Search the wiki\'\'\'</small>{BR}\n{SEARCHBOX}\n\n[image|PoweredBy|Images/PoweredBy.png|http://www.screwturn.eu]'),('PageChangeMessage','','The page \"##PAGE##\" was modified by ##USER## on ##DATETIME##.\nAuthor\'s comment: ##COMMENT##.\n\nThe page can be found at the following address:\n##LINK##\n\nThank you.\n\nBest regards,\nThe ##WIKITITLE## Team.'),('DiscussionChangeMessage','','A new message was posted on the page \"##PAGE##\" by ##USER## on ##DATETIME##.\n\nThe subject of the message is \"##SUBJECT##\" and it can be found at the following address:\n##LINK##\n\nThank you.\n\nBest regards,\nThe ##WIKITITLE## Team.'),('ApproveDraftMessage','','A draft for the page \"##PAGE##\" was created or modified by ##USER## on ##DATETIME## and is currently held for **approval**.\nAuthor\'s comment: ##COMMENT##.\n\nThe draft can be found and edited at the following address:\n##LINK##\nYou can directly approve or reject the draft at the following address:\n##LINK2##\n\nPlease note that the draft will not be displayed until it is approved.\n\nThank you.\n\nBest regards,\nThe ##WIKITITLE## Team.');
/*!40000 ALTER TABLE `MetaDataItem` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Namespace`
--

DROP TABLE IF EXISTS `Namespace`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Namespace` (
  `Name` varchar(100) NOT NULL,
  `DefaultPage` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Namespace`
--

LOCK TABLES `Namespace` WRITE;
/*!40000 ALTER TABLE `Namespace` DISABLE KEYS */;
INSERT INTO `Namespace` VALUES ('',NULL);
/*!40000 ALTER TABLE `Namespace` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `NavigationPath`
--

DROP TABLE IF EXISTS `NavigationPath`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `NavigationPath` (
  `Name` varchar(100) NOT NULL,
  `Namespace` varchar(100) NOT NULL,
  `Page` varchar(200) NOT NULL,
  `Number` smallint(6) NOT NULL,
  PRIMARY KEY (`Name`,`Namespace`,`Page`),
  KEY `FK_NavigationPath_Page` (`Page`,`Namespace`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `NavigationPath`
--

LOCK TABLES `NavigationPath` WRITE;
/*!40000 ALTER TABLE `NavigationPath` DISABLE KEYS */;
/*!40000 ALTER TABLE `NavigationPath` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `OutgoingLink`
--

DROP TABLE IF EXISTS `OutgoingLink`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `OutgoingLink` (
  `Source` varchar(100) NOT NULL,
  `Destination` varchar(100) NOT NULL,
  PRIMARY KEY (`Source`,`Destination`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `OutgoingLink`
--

LOCK TABLES `OutgoingLink` WRITE;
/*!40000 ALTER TABLE `OutgoingLink` DISABLE KEYS */;
/*!40000 ALTER TABLE `OutgoingLink` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Page`
--

DROP TABLE IF EXISTS `Page`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Page` (
  `Name` varchar(200) NOT NULL,
  `Namespace` varchar(100) NOT NULL,
  `CreationDateTime` datetime NOT NULL,
  PRIMARY KEY (`Name`,`Namespace`),
  KEY `FK_Page_Namespace` (`Namespace`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PageContent`
--

DROP TABLE IF EXISTS `PageContent`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PageContent` (
  `Page` varchar(200) NOT NULL,
  `Namespace` varchar(100) NOT NULL,
  `Revision` smallint(6) NOT NULL,
  `Title` varchar(200) NOT NULL,
  `User` varchar(100) NOT NULL,
  `LastModified` datetime NOT NULL,
  `Comment` varchar(300) DEFAULT NULL,
  `Content` longtext NOT NULL,
  `Description` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`Page`,`Namespace`,`Revision`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PageKeyword`
--

DROP TABLE IF EXISTS `PageKeyword`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PageKeyword` (
  `Page` varchar(200) NOT NULL,
  `Namespace` varchar(100) NOT NULL,
  `Revision` smallint(6) NOT NULL,
  `Keyword` varchar(50) NOT NULL,
  PRIMARY KEY (`Page`,`Namespace`,`Revision`,`Keyword`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `PageKeyword`
--

LOCK TABLES `PageKeyword` WRITE;
/*!40000 ALTER TABLE `PageKeyword` DISABLE KEYS */;
/*!40000 ALTER TABLE `PageKeyword` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `PluginAssembly`
--

DROP TABLE IF EXISTS `PluginAssembly`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PluginAssembly` (
  `Name` varchar(100) NOT NULL,
  `Assembly` longblob NOT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `PluginAssembly`
--

LOCK TABLES `PluginAssembly` WRITE;
/*!40000 ALTER TABLE `PluginAssembly` DISABLE KEYS */;
/*!40000 ALTER TABLE `PluginAssembly` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `PluginStatus`
--

DROP TABLE IF EXISTS `PluginStatus`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PluginStatus` (
  `Name` varchar(150) NOT NULL,
  `Enabled` tinyint(1) NOT NULL,
  `Configuration` varchar(4000) NOT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `PluginStatus`
--

LOCK TABLES `PluginStatus` WRITE;
/*!40000 ALTER TABLE `PluginStatus` DISABLE KEYS */;
INSERT INTO `PluginStatus` VALUES ('ScrewTurn.Wiki.FilesStorageProvider',1,''),('GreenIcicle.Screwturn3SyntaxHighlighter.SyntaxHighlightFormatProvider',1,'ScriptUrl=/JS/sh;Theme=Default');
/*!40000 ALTER TABLE `PluginStatus` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `RecentChange`
--

DROP TABLE IF EXISTS `RecentChange`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `RecentChange` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Page` varchar(200) NOT NULL,
  `Title` varchar(200) NOT NULL,
  `MessageSubject` varchar(200) DEFAULT NULL,
  `DateTime` datetime NOT NULL,
  `User` varchar(100) NOT NULL,
  `Change` char(1) NOT NULL,
  `Description` varchar(4000) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=MyISAM AUTO_INCREMENT=8 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Setting`
--

DROP TABLE IF EXISTS `Setting`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Setting` (
  `Name` varchar(100) NOT NULL,
  `Value` varchar(4000) NOT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Setting`
--

LOCK TABLES `Setting` WRITE;
/*!40000 ALTER TABLE `Setting` DISABLE KEYS */;
INSERT INTO `Setting` VALUES ('LastPageIndexing','20110518020432'),('WikiTitle','ScrewTurn Wiki'),('MainUrl','http://www.server.com/'),('ContactEmail','info@server.com'),('SenderEmail','no-reply@server.com'),('ErrorsEmails',''),('SmtpServer','smtp.server.com'),('SmtpPort','-1'),('SmtpUsername',''),('SmtpPassword',''),('SmtpSsl','no'),('Theme','Elegant'),('DefaultPage','Main_Page'),('DateTimeFormat','ddd\', \'dd\' \'MMM\' \'yyyy\' \'HH\':\'mm'),('DefaultLanguage','en-US'),('DefaultTimezone','60'),('MaxRecentChangesToDisplay','10'),('RssFeedsMode','Summary'),('EnableDoubleClickEditing','no'),('EnableSectionEditing','yes'),('EnableSectionAnchors','yes'),('EnablePageToolbar','yes'),('EnableViewPageCode','yes'),('EnablePageInfoDiv','yes'),('DisableBreadcrumbsTrail','no'),('AutoGeneratePageNames','yes'),('ProcessSingleLineBreaks','no'),('UseVisualEditorAsDefault','no'),('KeptBackupNumber','-1'),('DisplayGravatars','yes'),('UsersCanRegister','yes'),('UsernameRegex','^\\w[\\w\\ !$@%^\\.\\(\\)\\-_]{3,25}$'),('PasswordRegex','^\\w[\\w~!@#$%^\\(\\)\\[\\]\\{\\}\\.,=\\-_\\ ]{5,25}$'),('AccountActivationMode','EMAIL'),('UsersGroup','Users'),('AdministratorsGroup','Administrators'),('AnonymousGroup','Anonymous'),('DisableCaptchaControl','no'),('DisableConcurrentEditing','no'),('ChangeModerationMode','RequirePageEditingPermissions'),('AllowedFileTypes','jpg|jpeg|gif|png|tif|tiff|bmp|svg|htm|html|zip|rar|pdf|txt|doc|xls|ppt|docx|xlsx|pptx'),('FileDownloadCountFilterMode','CountAll'),('FileDownloadCountFilter',''),('MaxFileSize','10240'),('ScriptTagsAllowed','no'),('LoggingLevel','3'),('MaxLogSize','256'),('IpHostFilter',''),('DisableAutomaticVersionCheck','no'),('DisableCache','no'),('CacheSize','100'),('CacheCutSize','20'),('EnableViewStateCompression','no'),('EnableHttpCompression','no'),('DefaultPagesProvider','ScrewTurn.Wiki.Plugins.SqlServer.SqlServerPagesStorageProvider'),('DefaultUsersProvider','ScrewTurn.Wiki.Plugins.SqlServer.SqlServerUsersStorageProvider'),('DefaultFilesProvider','ScrewTurn.Wiki.FilesStorageProvider'),('DefaultCacheProvider','ScrewTurn.Wiki.CacheProvider');
/*!40000 ALTER TABLE `Setting` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Snippet`
--

DROP TABLE IF EXISTS `Snippet`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Snippet` (
  `Name` varchar(200) NOT NULL,
  `Content` longtext NOT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Snippet`
--

LOCK TABLES `Snippet` WRITE;
/*!40000 ALTER TABLE `Snippet` DISABLE KEYS */;
/*!40000 ALTER TABLE `Snippet` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `User`
--

DROP TABLE IF EXISTS `User`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `User` (
  `Username` varchar(100) NOT NULL,
  `PasswordHash` varchar(100) NOT NULL,
  `DisplayName` varchar(150) DEFAULT NULL,
  `Email` varchar(100) NOT NULL,
  `Active` tinyint(1) NOT NULL,
  `DateTime` datetime NOT NULL,
  PRIMARY KEY (`Username`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `User`
--

LOCK TABLES `User` WRITE;
/*!40000 ALTER TABLE `User` DISABLE KEYS */;
/*!40000 ALTER TABLE `User` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `UserData`
--

DROP TABLE IF EXISTS `UserData`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `UserData` (
  `User` varchar(100) NOT NULL,
  `Key` varchar(100) NOT NULL,
  `Data` varchar(4000) NOT NULL,
  PRIMARY KEY (`User`,`Key`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserData`
--

LOCK TABLES `UserData` WRITE;
/*!40000 ALTER TABLE `UserData` DISABLE KEYS */;
/*!40000 ALTER TABLE `UserData` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `UserGroup`
--

DROP TABLE IF EXISTS `UserGroup`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `UserGroup` (
  `Name` varchar(100) NOT NULL,
  `Description` varchar(150) DEFAULT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserGroup`
--

LOCK TABLES `UserGroup` WRITE;
/*!40000 ALTER TABLE `UserGroup` DISABLE KEYS */;
INSERT INTO `UserGroup` VALUES ('Administrators','Built-in Administrators'),('Users','Built-in Users'),('Anonymous','Built-in Anonymous Users');
/*!40000 ALTER TABLE `UserGroup` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `UserGroupMembership`
--

DROP TABLE IF EXISTS `UserGroupMembership`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `UserGroupMembership` (
  `User` varchar(100) NOT NULL,
  `UserGroup` varchar(100) NOT NULL,
  PRIMARY KEY (`User`,`UserGroup`),
  KEY `FK_UserGroupMembership_UserGroup` (`UserGroup`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserGroupMembership`
--

LOCK TABLES `UserGroupMembership` WRITE;
/*!40000 ALTER TABLE `UserGroupMembership` DISABLE KEYS */;
/*!40000 ALTER TABLE `UserGroupMembership` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Version`
--

DROP TABLE IF EXISTS `Version`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Version` (
  `Component` varchar(100) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Component`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Version`
--

LOCK TABLES `Version` WRITE;
/*!40000 ALTER TABLE `Version` DISABLE KEYS */;
INSERT INTO `Version` VALUES ('Settings',3000),('Users',3000),('Files',3000),('Pages',3001);
/*!40000 ALTER TABLE `Version` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2011-05-24 11:43:11
