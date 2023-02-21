-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               5.6.17 - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Version:             10.1.0.5464
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for iwango
CREATE DATABASE IF NOT EXISTS `iwango` /*!40100 DEFAULT CHARACTER SET sjis */;
USE `iwango`;

-- Dumping structure for table iwango.accounts
CREATE TABLE IF NOT EXISTS `accounts` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `username` varchar(32) NOT NULL,
  `password` varchar(32) NOT NULL DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=sjis;

-- Dumping data for table iwango.accounts: ~2 rows (approximately)
/*!40000 ALTER TABLE `accounts` DISABLE KEYS */;
INSERT INTO `accounts` (`id`, `username`, `password`) VALUES
	(1, 'ioncannon@daytonakey', ''),
	(2, 'someguy@daytonakey', ''),
	(3, 'xiden@daytonakey', '');
/*!40000 ALTER TABLE `accounts` ENABLE KEYS */;

-- Dumping structure for table iwango.handles
CREATE TABLE IF NOT EXISTS `handles` (
  `accountId` bigint(20) unsigned NOT NULL,
  `name` varchar(32) NOT NULL,
  `creationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `name` (`name`),
  KEY `accountId` (`accountId`),
  CONSTRAINT `handle_account_fk` FOREIGN KEY (`accountId`) REFERENCES `accounts` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=sjis;

-- Dumping data for table iwango.handles: ~3 rows (approximately)
/*!40000 ALTER TABLE `handles` DISABLE KEYS */;
INSERT INTO `handles` (`accountId`, `name`, `creationDate`) VALUES
	(1, 'aaa.us', '2023-02-19 18:26:00'),
	(2, 'Ioncannon.us', '2022-12-27 13:17:24'),
	(2, 'Someguy.us', '2022-12-27 10:50:19'),
	(1, 'test.us', '2022-12-27 10:25:40');
/*!40000 ALTER TABLE `handles` ENABLE KEYS */;

-- Dumping structure for table iwango.lobby_servers
CREATE TABLE IF NOT EXISTS `lobby_servers` (
  `name` varchar(32) NOT NULL,
  `ip` varchar(20) NOT NULL,
  `port` smallint(5) unsigned NOT NULL,
  `commodityId` varchar(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=sjis;

-- Dumping data for table iwango.lobby_servers: ~1 rows (approximately)
/*!40000 ALTER TABLE `lobby_servers` DISABLE KEYS */;
INSERT INTO `lobby_servers` (`name`, `ip`, `port`, `commodityId`) VALUES
	('Test_Server', '192.168.0.249', 9501, 'daytona');
/*!40000 ALTER TABLE `lobby_servers` ENABLE KEYS */;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
