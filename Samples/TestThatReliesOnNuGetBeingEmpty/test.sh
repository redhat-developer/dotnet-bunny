#!/bin/bash

if [[ -d ~/.nuget/packages ]]; then
    exit 1
else
    exit 0
fi
