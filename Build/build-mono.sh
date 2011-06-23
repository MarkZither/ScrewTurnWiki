#!/bin/bash
xbuild /property:Configuration=Debug ScrewTurnWiki.msbuild
pushd Artifacts/WebApplication-SqlServer/Themes/MonoClearlyModern
ln -sf themes/default-fiber.css color-theme.css
ln -sf images/favicon.gif Icon_Xamarin.gif
popd
