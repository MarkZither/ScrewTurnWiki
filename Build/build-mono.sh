#!/bin/bash -e
xbuild /property:Configuration=Debug ScrewTurnWiki.msbuild
pushd Artifacts/WebApplication-SqlServer/Themes/MonoClearlyModern
ln -sf themes/default-fiber.css color-theme.css
ln -sf images/favicon.gif Icon.Xamarin.gif
popd
pushd ../Mono/Plugins/MediaWikiCompat
./build.sh
popd
if [ -f ../Mono/Web.config ]; then
    cp -ap ../Mono/Web.config Artifacts/WebApplication-SqlServer/
fi
pushd Artifacts/WebApplication-SqlServer/
echo WebSite can be found in `pwd`
echo Make sure to edit Web.config in that directory and update database access credentials
echo
