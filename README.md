# Mono DekiWiki to ScrewTurnWiki conversion notes

## Prerequisites

In order to perform the MonoProject wiki conversion you need to have access to the DekiWiki database *and*
website sources. The DekiWiki website must be unpacked and placed anywhere on your disk (by default the conversion
program looks for it in _/tmp/public_html_).

Next, you must create database to keep the original DekiWiki data. By default the conversion program attempts to access
database named *monoproject* on localhost, with the *monoproject* user name and the *monoproject* password.

The database must be created with the default character set configured to *utf8*. Because of MySQL limitations, the *InnoDB*
storage engine must be used. To achieve that, you need to edit the original DekiWiki data dump file and replace all instances
of *TYPE=MyISAM* in *CREATE TABLE* statements with *ENGINE=InnoDB*. Additionally, all instances of *PACK_KEYS=1* must be removed from
the table creation statements as well. After that, data can be imported to the newly created database.

ScrewTurnWiki requires Mono 2.10 from the 2.10 branch.

## Things to keep in mind

- If you add anything to the web application (found in the *WebApplication/* directory) make sure to edit the *WebApplication.csproj*
  file and add appropriate `<Content Include="..."/>` lines or otherwise the new files will not be copied to the website output directory
  on build

## Conversion process

### Run the conversion utility

The conversion utility can be found in the *Mono/tools/* directory and can be compiled using the `deki-migration-build.sh` script in
the same directory. After the utility is built, run the `deki-migration` script in the same directory. The conversion utility can take
up to three arguments:

    deki-migration [DEKI_WEBSITE_DIRECTORY [DEKI_MYSQL_CONNECTION_STRING [OUTPUT_FILE]]]

The default values for those can be found in the `deki-migration.cs` file.

When ran, the deki-migration script will output some diagnostics. If data is converted successfully, you will find the generated SQL
statements in the _OUTPUT_FILE_ specified above (*mono-project-stw-full.sql* by default) and you're ready for the next stage.

### Populate the ScrewTurnWiki database

The *Mono/tools* directory contains the *stw-clean-withplugins.sql* file which is used to create the initial, empty, database ready
to receive data generated in the previous step. ScrewTurnWiki stores plugins in the database and the above file includes two of them -
the syntax highlighter and the Mono-specific plugin which overrides some ScrewTurnWiki behaviors. Every time you convert DekiWiki data
into STW format, you *must* use @stw-clean-withplugins.sql* first.

After the database is initialized, import the file generated in the previous step.

### Build the website

Once the database is populated, the next step is to build the website. To do that, go to the *Build* directory below the top-level 
directory and run the `build-mono.sh` script. If all goes well, the script will generate the website and tell you the location in
which it can be found.

### Configure the website

After successful compilation, go to the location indicated by the build script and edit the *Web.config* file in order to put correct
database access credentials in the _SettingsStorageProviderConfig_ appSettings key.

You should be all set now!

## Running ScrewTurnWiki

It is recommended that the site is configured to run with Mono's SGEN garbage collector.


