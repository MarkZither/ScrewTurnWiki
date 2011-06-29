#!/bin/bash
WEB_BIN="../../../WebApplication/bin/"
SOURCES="\
    MediaWikiCompatPlugin.cs \
    Table.cs \
    Toc.cs"
REFS="\
    -r:$WEB_BIN/ScrewTurn.Wiki.PluginFramework.dll \
    -r:$WEB_BIN/ScrewTurn.Wiki.Core.dll"
gmcs -debug:full  -target:library $REFS -out:Mono.MediaWikiCompat.dll $SOURCES
