#!/bin/bash

rm -rf Turkey/bin Turkey/obj
pushd Turkey
dotnet publish -r linux-x64 -c Release --self-contained true
popd
mv Turkey/bin/Release/netcoreapp3.1/linux-x64/publish/Turkey Turkey/bin/Release/netcoreapp3.1/linux-x64/publish/turkey
ls -lah Turkey/bin/Release/netcoreapp3.1/linux-x64/publish/

