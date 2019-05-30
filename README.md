# Turkey

This is a test runner for running integration/regression tests for
.NET Core.

It uses the same format for identifying, selecting and running tests
as [dotnet-bunny](https://github.com/redhat-developer/dotnet-bunny/).

# Running Tests

If you have a directory containing tests, you can run them via
invoking the `turkey` shell script. For example:

    cd Samples
    ../turkey

See `turkey --help` for more information on how to select and run
tests and how to show the test output.

To get output compatible with `dotnet-bunny`, use `turkey --compatible`

# Writing Tests

Two different types of tests are supported: xunit-style tests that are
executed with `dotnet test` and bash scripts that are executed directly.

Each test must be stored in a unique directory. The test must contain
a `test.json` file. An example of this file:

    {
      "name": "CVE-2018-0875",
      "enabled": true,
      "version": "2.0",
      "versionSpecific": false,
      "type": "xunit",
      "cleanup": true,
      "platformBlacklist": [
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

- `platformBlacklist`

  This is a list of platform names, optionally followed by the
  version, where this test is invalid . For example, `["fedora"]`
  means this test should be skipped on all Fedora platforms.
  `["rhel7"]` means this test should be skipped on RHEL 7, but not on
  RHEL 8, or another RHEL version.


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

# TODO

- Do not modify original source files for xunit tests
- Use provided NuGet config when running each bash/xunit test
