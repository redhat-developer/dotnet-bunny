# Turkey

This is a test runner for running integration/regression tests for
.NET Core.

It uses the same format for identifying, selecting and running tests
as [dotnet-bunny](https://github.com/redhat-developer/dotnet-bunny/).

# Some Notes on tests

- All tests are run with the current working directory set to a
  directory where all the test files are present. This may not be the
  original directory of the tests, but a copy instead.

# TODO

- Do not modify original source files for xunit tests
- Implement timeouts for tests
- Use provided NuGet config when running each bash/xunit test
