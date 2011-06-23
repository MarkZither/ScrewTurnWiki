#!/bin/bash
gmcs -debug:full -r:../../../WebApplication/bin/ScrewTurn.Wiki.PluginFramework.dll -target:library -out:Mono.MediaWikiCompat.dll Table.cs MediaWikiCompatPlugin.cs
