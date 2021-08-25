#!/bin/bash

env

if [[ -n "${OPENSSL_CONF}" ]]; then
    echo "error: OPENSSL_CONF should never be set"
    exit 1
fi
