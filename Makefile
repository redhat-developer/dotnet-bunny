
all: publish

check:
	dotnet test -c Release Turkey.Tests

run-samples:
	rm -rf ~/.nuget.orig && mv ~/.nuget ~/.nuget.orig && mkdir -p ~/.nuget
	cd Samples && test -f ../turkey && (bash ../turkey || true)
	rm -rf ~/.nuget && mv ~/.nuget.orig ~/.nuget

publish:
	dotnet publish -c Release

clean:
	rm -rf Turkey/bin Turkey/obj
	rm -rf Turkey.Tests/bin Turkey.Tests/obj
	find -iname '*.log' -delete

fix-line-endings:
	find -iname '*.cs' -exec dos2unix {} \;
	find -iname '*.csproj' -exec dos2unix {} \;

list-todos:
	grep -r -E 'TODO|FIXME' *
