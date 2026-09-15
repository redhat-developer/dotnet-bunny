#!/bin/bash

env

if [[ -n "${ASPNETCORE_URLS}" ]]; then
    echo "error: ASPNETCORE_URLS should never be set."
    exit 1
fi

if [[ -n "${COREHOST_TRACE}" ]]; then
    echo "error: COREHOST_TRACE should never be set."
    exit 1
fi

if [[ -n "${DOTNET_FOO_BAR}" ]]; then
    echo "error: DOTNET_FOO_BAR should never be set."
    exit 1
fi

if [[ -n "${DOTNET_ROLL_FORWARD}" ]]; then
    echo "error: DOTNET_ROLL_FORWARD should never be set."
    exit 1
fi

if [[ -n "${DOTNET_RUNTIME_ID}" ]]; then
    echo "error: DOTNET_RUNTIME_ID should never be set."
    exit 1
fi

if [[ -n "${DOTNET_STARTUP_HOOKS}" ]]; then
    echo "error: DOTNET_STARTUP_HOOKS should never be set."
    exit 1
fi

if [[ -n "${NUGET_PACKAGES}" ]]; then
    echo "error: NUGET_PACKAGES should never be set."
    exit 1
fi

if [[ -n "${OPENSSL_CONF}" ]]; then
    echo "error: OPENSSL_CONF should never be set"
    exit 1
fi
