
all: publish

check: publish
	dotnet test Turkey.Tests

publish:
	dotnet publish -c Release

clean:
	rm -rf Turkey/bin Turkey/obj
	rm -rf Turkey.Tests/bin Turkey.Tests/obj
