### .NET Bunny is a simple script that hops through the folders and runs .NET Core tests based on json configuration.

This test suite is looking for a `test.json` and expects a `*.csproj` in the subdirectories, where * is the `name` property in the json configuration.

_The test assumes that the project file contains a `TargetFramework` property with `netcoreappx.y`, while it's version is irrelevant and the json-configured version will be used._

The json config has these values:

* `name` - Name of the test, equal to the directory it's in, and the `name.csproj`
* `version` - Minimum dotnet version this test should be run with, e.g. with `1.1` this test will not run with dotnet 1.0 - it will run with dotnet 1.1, 2.0, ... (use either `major.minor` or `major.minor.feature`)
* `versionSpecific` - Set to true for the above version to be exact match - for this test to not run with higher versions of dotnet. (You can use x as minor version, e.g. 1.x will run with 1.0 and 1.1 but not with anything higher.)
* `type` - Type of the test. This can be either `xunit` or `bash`. The `xunit` type will call `dotnet test` while the `bash` type will look for `test.sh` and call it (`test.sh` will receive the current dotnet version as ``$1`)
* `cleanup` - Clean up before running the test.
* `platform-blacklist` - A list of blacklisted platforms where the test will not run.

Example config file: 
```
{
  "name": "CVE-2018-0875",
  "enabled": true,
  "version": "2.0",
  "versionSpecific": false,
  "type": "xunit",
  "cleanup": true,
  "platform-blacklist": [
    "fedora"
    ]
}
```

### TODO:

* Export better and visual results.
* Cache nuget manually for every test to start clean.
