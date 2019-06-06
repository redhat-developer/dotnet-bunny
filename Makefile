
all: publish

check:
	dotnet test -c Release Turkey.Tests

run-samples:
	rm -rf ~/.nuget.orig && mv ~/.nuget ~/.nuget.orig && mkdir -p ~/.nuget
	cd Samples && test -f ../turkey && (bash ../turkey || true)
	rm -rf ~/.nuget && mv ~/.nuget.orig ~/.nuget

publish:
	git rev-parse --short HEAD > GIT_COMMIT_ID
	dotnet publish -c Release -p:VersionSuffix=$$(cat GIT_COMMIT_ID)

clean:
	rm -rf Turkey/bin Turkey/obj
	rm -rf Turkey.Tests/bin Turkey.Tests/obj
	find -iname '*.log' -delete

fix-line-endings:
	find -iname '*.cs' -exec dos2unix {} \;
	find -iname '*.csproj' -exec dos2unix {} \;
	find -iname 'nuget.config' -exec dos2unix {} \;

list-todos:
	grep -r -E 'TODO|FIXME' *
