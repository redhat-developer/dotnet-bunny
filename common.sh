#!/usr/bin/env bash

# Common functions for use by tests
# 
# To use this cript, source it in a test and then call one of the functions

set -euo pipefail

# Configuration

declare keep_logs=0

# Internal variables

declare errors=0
declare log_file
declare has_children=0
declare -a children

# Functions

function initialize() {
    errors=0
    has_children=0
    children=()
    log_file=$(mktemp --tmp "dotnet-test.XXXXXXXXXXX")
    echo "" > $log_file
    echo "[INFO] Running $(get-test-name)"
    echo "[INFO] Log at $log_file"
}

function step() {
    if [[ $errors -eq 0 ]] ; then
        # echo "[STEP] $@"
        set +e
        "$@" >> $log_file 2>&1 
        ret=$?
        set -e
        if [[ $ret -eq 0 ]] ; then
            echo "[OK] $@"
        else
            echo "[ERROR] $@"
            errors=1
        fi
    else
        echo "[SKIP] $@"
    fi
}

function background-step() {
    if [[ $errors -eq 0 ]] ; then
        # echo "[STEP] $@"
        set +e
        "$@" >> $log_file 2>&1 &
        ret=$?
        children+=($!)
        has_children=1
        set -e
        if [[ $ret -eq 0 ]] ; then
            echo "[BACKGROUND] $@"
        else
            echo "[ERROR] $@"
            errors=1
        fi
    else
        echo "[SKIP] $@"
    fi
}

function error() {
    echo "[ERROR] $@"
    errors=1
}

function grep-log() {
    set +e
    grep "$@" $log_file > /dev/null
    if [[ $? -eq 0 ]] ; then
        echo "[OK] grep $@"
    else
        echo "[ERROR] grep $@"
        errors=1
    fi
    set -e
}

function finish() {
    if [[ $errors -eq 0 ]] ; then
        echo "[PASS] $(get-test-name)"
        if [[ $keep_logs -eq 0 ]]; then
            rm "$log_file"
            echo "[INFO] Deleted $log_file"
        fi
    else
        echo "[FAIL] $(get-test-name)"
    fi
    if [[ $has_children -eq 1 ]]; then
        for pid in "${children[@]}"; do 
            for child in $(ps -o pid,ppid -ax | awk "{ if ( \$2 == $pid ) { print \$1 }}"); do
                echo "[INFO] Killing child process $child because ppid = $pid"
                kill $child
            done
        done
    fi
    # make sure all background jobs are done
    wait
}

function get-test-name() {
    local base_path="${BASH_SOURCE[0]}"
    abs2rel $(readlink -f $0) $(readlink -f $(dirname $base_path))
}

