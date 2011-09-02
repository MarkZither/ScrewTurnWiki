
In order to build ScrewTurn Wiki, you need Microsoft Visual Studio 2010 (any edition, including Express) 
installed on the machine.

ScrewTurnWiki.msbuild contains the build script, written for MSBuild 3.5.

To build the application more easily, use the Build.bat batch file, which compiles the application
and the plugins.

BuildAndTest.bat compiles the application and runs all the unit tests. SQL Server 2005/2008 is required
on the machine and it must allow the necessary access rights to the current Windows user.

BuildAndPackage.bat creates a ZIP archive containing the compiled application and the plugins,
but it requires 7-zip [1] installed on the system.

[1] http://www.7-zip.org
