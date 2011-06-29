-- MySQL dump 10.13  Distrib 5.1.54, for debian-linux-gnu (x86_64)
--
-- Host: localhost    Database: monoproject
-- ------------------------------------------------------
-- Server version	5.1.54-1ubuntu4-log

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
-- Table structure for table `archive`
--

DROP TABLE IF EXISTS `archive`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `archive` (
  `ar_namespace` tinyint(2) unsigned NOT NULL DEFAULT '0',
  `ar_title` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `ar_text` mediumtext NOT NULL,
  `ar_comment` tinyblob NOT NULL,
  `ar_user` int(5) unsigned NOT NULL DEFAULT '0',
  `ar_user_text` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `ar_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `ar_minor_edit` tinyint(1) NOT NULL DEFAULT '0',
  `ar_flags` tinyblob NOT NULL,
  KEY `name_title_timestamp` (`ar_namespace`,`ar_title`,`ar_timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `blobs`
--

DROP TABLE IF EXISTS `blobs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `blobs` (
  `blob_index` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `blob_data` longblob NOT NULL,
  UNIQUE KEY `blob_index` (`blob_index`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `brokenlinks`
--

DROP TABLE IF EXISTS `brokenlinks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `brokenlinks` (
  `bl_from` int(8) unsigned NOT NULL DEFAULT '0',
  `bl_to` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  UNIQUE KEY `bl_from` (`bl_from`,`bl_to`),
  KEY `bl_to` (`bl_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `categorylinks`
--

DROP TABLE IF EXISTS `categorylinks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `categorylinks` (
  `cl_from` int(8) unsigned NOT NULL DEFAULT '0',
  `cl_to` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `cl_sortkey` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `cl_timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY `cl_from` (`cl_from`,`cl_to`),
  KEY `cl_sortkey` (`cl_to`,`cl_sortkey`(128)),
  KEY `cl_timestamp` (`cl_to`,`cl_timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cur`
--

DROP TABLE IF EXISTS `cur`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cur` (
  `cur_id` int(8) unsigned NOT NULL AUTO_INCREMENT,
  `cur_namespace` tinyint(2) unsigned NOT NULL DEFAULT '0',
  `cur_title` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `cur_text` mediumtext NOT NULL,
  `cur_comment` tinyblob NOT NULL,
  `cur_user` int(5) unsigned NOT NULL DEFAULT '0',
  `cur_user_text` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `cur_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `cur_restrictions` tinyblob NOT NULL,
  `cur_counter` bigint(20) unsigned NOT NULL DEFAULT '0',
  `cur_is_redirect` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `cur_minor_edit` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `cur_is_new` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `cur_random` double unsigned NOT NULL DEFAULT '0',
  `cur_touched` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `inverse_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  UNIQUE KEY `cur_id` (`cur_id`),
  KEY `cur_namespace` (`cur_namespace`),
  KEY `cur_title` (`cur_title`(20)),
  KEY `cur_timestamp` (`cur_timestamp`),
  KEY `cur_random` (`cur_random`),
  KEY `name_title_timestamp` (`cur_namespace`,`cur_title`,`inverse_timestamp`),
  KEY `user_timestamp` (`cur_user`,`inverse_timestamp`),
  KEY `usertext_timestamp` (`cur_user_text`,`inverse_timestamp`),
  KEY `namespace_redirect_timestamp` (`cur_namespace`,`cur_is_redirect`,`cur_timestamp`),
  KEY `id_title_ns_red` (`cur_id`,`cur_title`,`cur_namespace`,`cur_is_redirect`)
) ENGINE=InnoDB AUTO_INCREMENT=2577 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `hitcounter`
--

DROP TABLE IF EXISTS `hitcounter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `hitcounter` (
  `hc_id` int(10) unsigned NOT NULL DEFAULT '0'
) ENGINE=MEMORY DEFAULT CHARSET=utf8 MAX_ROWS=25000;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `image`
--

DROP TABLE IF EXISTS `image`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `image` (
  `img_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `img_size` int(8) unsigned NOT NULL DEFAULT '0',
  `img_description` tinyblob NOT NULL,
  `img_user` int(5) unsigned NOT NULL DEFAULT '0',
  `img_user_text` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `img_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  PRIMARY KEY (`img_name`),
  KEY `img_size` (`img_size`),
  KEY `img_timestamp` (`img_timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `imagelinks`
--

DROP TABLE IF EXISTS `imagelinks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `imagelinks` (
  `il_from` int(8) unsigned NOT NULL DEFAULT '0',
  `il_to` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  UNIQUE KEY `il_from` (`il_from`,`il_to`),
  KEY `il_to` (`il_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `interwiki`
--

DROP TABLE IF EXISTS `interwiki`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `interwiki` (
  `iw_prefix` char(32) NOT NULL DEFAULT '',
  `iw_url` char(127) NOT NULL DEFAULT '',
  `iw_local` tinyint(1) NOT NULL DEFAULT '0',
  UNIQUE KEY `iw_prefix` (`iw_prefix`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ipblocks`
--

DROP TABLE IF EXISTS `ipblocks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ipblocks` (
  `ipb_id` int(8) NOT NULL AUTO_INCREMENT,
  `ipb_address` varchar(40) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `ipb_user` int(8) unsigned NOT NULL DEFAULT '0',
  `ipb_by` int(8) unsigned NOT NULL DEFAULT '0',
  `ipb_reason` tinyblob NOT NULL,
  `ipb_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `ipb_auto` tinyint(1) NOT NULL DEFAULT '0',
  `ipb_expiry` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  UNIQUE KEY `ipb_id` (`ipb_id`),
  KEY `ipb_address` (`ipb_address`),
  KEY `ipb_user` (`ipb_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `links`
--

DROP TABLE IF EXISTS `links`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `links` (
  `l_from` int(8) unsigned NOT NULL DEFAULT '0',
  `l_to` int(8) unsigned NOT NULL DEFAULT '0',
  UNIQUE KEY `l_from` (`l_from`,`l_to`),
  KEY `l_to` (`l_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `linkscc`
--

DROP TABLE IF EXISTS `linkscc`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `linkscc` (
  `lcc_pageid` int(10) unsigned NOT NULL DEFAULT '0',
  `lcc_cacheobj` mediumblob NOT NULL,
  UNIQUE KEY `lcc_pageid` (`lcc_pageid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `logging`
--

DROP TABLE IF EXISTS `logging`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `logging` (
  `log_type` varchar(10) NOT NULL DEFAULT '',
  `log_action` varchar(10) NOT NULL DEFAULT '',
  `log_timestamp` varchar(14) NOT NULL DEFAULT '19700101000000',
  `log_user` int(10) unsigned NOT NULL DEFAULT '0',
  `log_namespace` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `log_title` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `log_comment` varchar(255) NOT NULL DEFAULT '',
  `log_params` blob NOT NULL,
  KEY `type_time` (`log_type`,`log_timestamp`),
  KEY `user_time` (`log_user`,`log_timestamp`),
  KEY `page_time` (`log_namespace`,`log_title`,`log_timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `math`
--

DROP TABLE IF EXISTS `math`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `math` (
  `math_inputhash` varchar(16) NOT NULL DEFAULT '',
  `math_outputhash` varchar(16) NOT NULL DEFAULT '',
  `math_html_conservativeness` tinyint(1) NOT NULL DEFAULT '0',
  `math_html` text,
  `math_mathml` text,
  UNIQUE KEY `math_inputhash` (`math_inputhash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `objectcache`
--

DROP TABLE IF EXISTS `objectcache`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `objectcache` (
  `keyname` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `value` mediumblob,
  `exptime` datetime DEFAULT NULL,
  UNIQUE KEY `keyname` (`keyname`),
  KEY `exptime` (`exptime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `old`
--

DROP TABLE IF EXISTS `old`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `old` (
  `old_id` int(8) unsigned NOT NULL AUTO_INCREMENT,
  `old_namespace` tinyint(2) unsigned NOT NULL DEFAULT '0',
  `old_title` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `old_text` mediumtext NOT NULL,
  `old_comment` tinyblob NOT NULL,
  `old_user` int(5) unsigned NOT NULL DEFAULT '0',
  `old_user_text` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `old_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `old_minor_edit` tinyint(1) NOT NULL DEFAULT '0',
  `old_flags` tinyblob NOT NULL,
  `inverse_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  UNIQUE KEY `old_id` (`old_id`),
  KEY `old_namespace` (`old_namespace`,`old_title`(20)),
  KEY `old_timestamp` (`old_timestamp`),
  KEY `name_title_timestamp` (`old_namespace`,`old_title`,`inverse_timestamp`),
  KEY `user_timestamp` (`old_user`,`inverse_timestamp`),
  KEY `usertext_timestamp` (`old_user_text`,`inverse_timestamp`)
) ENGINE=InnoDB AUTO_INCREMENT=14382 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `oldimage`
--

DROP TABLE IF EXISTS `oldimage`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `oldimage` (
  `oi_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `oi_archive_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `oi_size` int(8) unsigned NOT NULL DEFAULT '0',
  `oi_description` tinyblob NOT NULL,
  `oi_user` int(5) unsigned NOT NULL DEFAULT '0',
  `oi_user_text` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `oi_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  KEY `oi_name` (`oi_name`(10))
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `querycache`
--

DROP TABLE IF EXISTS `querycache`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `querycache` (
  `qc_type` char(32) NOT NULL DEFAULT '',
  `qc_value` int(5) unsigned NOT NULL DEFAULT '0',
  `qc_namespace` tinyint(2) unsigned NOT NULL DEFAULT '0',
  `qc_title` char(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  KEY `qc_type` (`qc_type`,`qc_value`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `recentchanges`
--

DROP TABLE IF EXISTS `recentchanges`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `recentchanges` (
  `rc_timestamp` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `rc_cur_time` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `rc_user` int(10) unsigned NOT NULL DEFAULT '0',
  `rc_user_text` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `rc_namespace` tinyint(3) NOT NULL DEFAULT '0',
  `rc_title` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `rc_comment` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `rc_minor` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `rc_bot` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `rc_new` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `rc_cur_id` int(10) unsigned NOT NULL DEFAULT '0',
  `rc_this_oldid` int(10) unsigned NOT NULL DEFAULT '0',
  `rc_last_oldid` int(10) unsigned NOT NULL DEFAULT '0',
  `rc_type` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `rc_moved_to_ns` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `rc_moved_to_title` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `rc_ip` varchar(15) NOT NULL DEFAULT '',
  `rc_id` int(8) NOT NULL AUTO_INCREMENT,
  `rc_patrolled` tinyint(3) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`rc_id`),
  KEY `rc_timestamp` (`rc_timestamp`),
  KEY `rc_namespace_title` (`rc_namespace`,`rc_title`),
  KEY `rc_cur_id` (`rc_cur_id`),
  KEY `new_name_timestamp` (`rc_new`,`rc_namespace`,`rc_timestamp`),
  KEY `rc_ip` (`rc_ip`)
) ENGINE=InnoDB AUTO_INCREMENT=15665 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `searchindex`
--

DROP TABLE IF EXISTS `searchindex`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `searchindex` (
  `si_page` int(8) unsigned NOT NULL DEFAULT '0',
  `si_title` varchar(255) NOT NULL DEFAULT '',
  `si_text` mediumtext NOT NULL,
  UNIQUE KEY `si_page` (`si_page`),
  FULLTEXT KEY `si_title` (`si_title`),
  FULLTEXT KEY `si_text` (`si_text`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 PACK_KEYS=1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `site_stats`
--

DROP TABLE IF EXISTS `site_stats`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `site_stats` (
  `ss_row_id` int(8) unsigned NOT NULL DEFAULT '0',
  `ss_total_views` bigint(20) unsigned DEFAULT '0',
  `ss_total_edits` bigint(20) unsigned DEFAULT '0',
  `ss_good_articles` bigint(20) unsigned DEFAULT '0',
  `ss_total_pages` bigint(20) DEFAULT '-1',
  `ss_users` bigint(20) DEFAULT '-1',
  `ss_admins` int(10) DEFAULT '-1',
  UNIQUE KEY `ss_row_id` (`ss_row_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user`
--

DROP TABLE IF EXISTS `user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user` (
  `user_id` int(5) unsigned NOT NULL AUTO_INCREMENT,
  `user_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `user_real_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `user_password` tinyblob NOT NULL,
  `user_newpassword` tinyblob NOT NULL,
  `user_email` tinytext NOT NULL,
  `user_options` blob NOT NULL,
  `user_touched` varchar(14) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  `user_token` varchar(32) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  UNIQUE KEY `user_id` (`user_id`),
  KEY `user_name` (`user_name`(10))
) ENGINE=InnoDB AUTO_INCREMENT=300 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user_newtalk`
--

DROP TABLE IF EXISTS `user_newtalk`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user_newtalk` (
  `user_id` int(5) NOT NULL DEFAULT '0',
  `user_ip` varchar(40) NOT NULL DEFAULT '',
  KEY `user_id` (`user_id`),
  KEY `user_ip` (`user_ip`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user_rights`
--

DROP TABLE IF EXISTS `user_rights`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user_rights` (
  `ur_user` int(5) unsigned NOT NULL DEFAULT '0',
  `ur_rights` tinyblob NOT NULL,
  UNIQUE KEY `ur_user` (`ur_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `watchlist`
--

DROP TABLE IF EXISTS `watchlist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `watchlist` (
  `wl_user` int(5) unsigned NOT NULL DEFAULT '0',
  `wl_namespace` tinyint(2) unsigned NOT NULL DEFAULT '0',
  `wl_title` varchar(255) CHARACTER SET utf8 COLLATE utf8_bin NOT NULL DEFAULT '',
  UNIQUE KEY `wl_user` (`wl_user`,`wl_namespace`,`wl_title`),
  KEY `namespace_title` (`wl_namespace`,`wl_title`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2011-06-30  1:25:52
