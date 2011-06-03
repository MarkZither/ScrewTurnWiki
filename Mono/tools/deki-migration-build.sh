#!/bin/bash
REFERENCES="-r:../../References/Lib/MySQL/MySql.Data.dll -r:System.Data"
SOURCES="deki-migration.cs WikiPage.cs WikiUser.cs WikiImage.cs MigrationExtensions.cs DekiNamespace.cs"
TARGET="deki-migration.exe"

dmcs -debug:full $REFERENCES -out:$TARGET $SOURCES
