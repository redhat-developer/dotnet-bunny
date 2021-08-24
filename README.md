# Turkey

This is a test runner for running integration/regression tests for
.NET Core.

It uses the same format for identifying, selecting and running tests
as [dotnet-bunny](https://github.com/redhat-developer/dotnet-bunny/).

It produces results in various forms, including a junit-compatible xml file.

# Building

Use the following command to build the `turkey` program and place it in the
`bin/` directory.

    make

# Running Tests

If you have a directory containing tests, you can run them via
invoking the `turkey` shell script. For example:

    ./bin/turkey Samples
    BashTestSpecificToDotNet2x                                  [PASS]
    BashTestSpecificToDotNet50                                  [SKIP]
    DisabledBashTest                                            [SKIP]
    ...

See `turkey --help` for more information on how to select and run
tests and how to show the test output.

To get output compatible with `dotnet-bunny`, use `turkey --compatible`

A real example of a test-suite to use with this framework is:
https://github.com/redhat-developer/dotnet-regular-tests/

# Writing Tests

Two different types of tests are supported: xunit-style tests that are
executed with `dotnet test` and bash scripts that are executed directly.

Each test must be stored in a unique directory. The test must contain
a `test.json` file. An example of this file:

    {
      "name": "CVE-2018-0875",
      "enabled": true,
      "requiresSdk": true,
      "version": "2.0",
      "versionSpecific": false,
      "type": "xunit",
      "cleanup": true,
      "ignoredRIDs": [
        "fedora"
        "fedora29"
        "rhel7"
       ]
    }

The `type` specifies how the test is executed.

## Test Configuration Syntax

`test.json` needs to be a `json` file containing a json object with
the following keys:

- `name`

  The name of the test. It must be the same as the name of the
  directory containing the test.

- `enabled`

  Indicates whether a test is enabled. Useful for disabling specific
  tests that are causing issues.

- `requiresSdk`

  Indicates whether a test requires SDK to be installed. If false 
  the test will run even when there is no SDK present.

- `version`

  The version of .NET Core runtime that this test is valid for. It can
  be a complete major/minor version like `2.1` or a wildcard like
  `1.x`. Unless `versionSpecific` is also set, this test will be
  executed on all versions equal to or greater than the specified
  version. For example, setting `version` to `2.0` will result in
  tests being executed under .NET Core versions 2.0, 2.1, 3.0, and 5.0
  but not under 1.1.

- `versionSpecific`

  If set, this test will only be executed if the .NET Core version
  matches the version specified in `version`.

  For example, if `version` is `2.0` and `versionSpecific` is `true`,
  the test will only be executed for .NET Core 2.0, not for 2.1, 1.1
  or 3.0.

  It is often useful to have a wildcard version with this. For
  example, `version` of `1.x` and `versionSpecific` of `true`, means
  that the tests will only be executed on .NET Core 1.0 and 1.1, and
  on no other versions.

- `type`

  Tests can be one of two `type`s: `xunit` or `bash`. `xunit` tests
  are executed by running `dotnet test`. `bash` tests are executed by
  executing a `test.sh` file directly.

- `cleanup`

  Specifies whether directories like `obj` and `bin` should be deleted
  before running the test.

- `ignoredRIDs`

  This is a list of runtime-ids or platform names (optionally followed
  by the version) where this test is invalid. The test will be skipped
  on those platforms.

  Examples:

  - `["linux-arm64"]`: skip this test on `arm64` (aka `aarch64`)
  - `["fedora"]`: skip this test on all Fedora platforms
  - `["rhel.7"]` or `["rhel7"]`: skip this test on RHEL 7, but not on RHEL 8, or another RHEL version
  - `["rhel.8-arm64"]`: skip this test on RHEL 8 on arm64

## Notes on Writing Tests

Some notes for writing tests:

- The first argument passed to a `test.sh` is the version number of
  .NET Core that's being tested.

- All tests are run with the current working directory set to a
  directory where all the test files are present. This may not be the
  original directory of the tests, but a copy instead.

- Tests should try and complete as quickly as possible. Long running
  tests may hit a timeout and be marked as failed.

# Project Conventions

- All warnings are displayed as:

    WARNING: foo bar baz

# Releasing

1. Tag the release

       $ git tag -a v# --sign

   Replease `#` with the real release version. Generally, use the previous
   release version + 1. For example, if the last tag was `v99`, use `v100`.

   This produces a signed and annotated tag. Feel free to add details about the
   release to the annotation.

   Signing requires a gpg key. If you don't have one, you can omit `--sign`.

2. Push the tags to GitHub

       $ git push --tags remote-name

       OR, better:

       $ git push remote-name tag-name

3. GitHub Actions will create a draft release corresponding to the tag.

   It will also attach the `turkey` and `turkey-$arch` binaries to the release.

   Many tools use `wget
   https://github.com/redhat-developer/dotnet-bunny/releases/latest/download/turkey`
   to get the latest release. This keeps them working.

4. Publish the release in GitHub

   1. Select the tag created in step 1

   2. Write release notes

   3. Publish the release

# TODO

- Do not modify original source files for xunit tests

# License

Copyright (C) 2019 Red Hat, Inc

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
