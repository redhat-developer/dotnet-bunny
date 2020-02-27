#!/usr/bin/env python3

# Wrapper for compatiblity with older users of this repository. If you are a
# new user, look at using bin/turkey directly.
#
# We download a known-to-work prebuilt binary instead of compiling using the
# sourcecode ourselves.

import json
import os
import re
import sys
import subprocess
from urllib.request import urlopen


def download_turkey(binary):
    os.makedirs(os.path.dirname(binary), 0o755)
    release_url = "https://api.github.com/repos/redhat-developer/dotnet-bunny/releases/latest"
    with urlopen(release_url) as response:
        json_response = json.load(response)
        download_url = json_response['assets'][0]['browser_download_url']
        print('Downloading ' + download_url + ' to ' + binary)
        with urlopen(download_url) as download:
            with open(binary, 'wb') as f:
                f.write(download.read())
            os.chmod(binary, 0o755)

def main(args):
    binary = os.path.realpath(os.path.join('bin', 'turkey'))
    if not os.path.exists(binary):
        download_turkey(binary)

    p = subprocess.run([binary, '--version'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, universal_newlines=True, shell=False)
    print(p.stdout)
    if p.returncode != 0:
        return returncode

    test_args = []
    test_args.append(binary)
    # remove first two args
    # first arg is name of program
    # second arg is the .NET Core version
    test_args.extend(args[2:])
    test_args.append('-c')

    print('Running: ' + str(test_args))

    p = subprocess.run(test_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, universal_newlines=True, shell=False)
    print(p.stdout)
    if p.returncode != 0:
        return p.returncode

if __name__ == '__main__':
    sys.exit(main(sys.argv))
