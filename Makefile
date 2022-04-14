FRAMEWORK:=netcoreapp3.1
CONFIGURATION:=Release
ARCH:=$(subst aarch64,arm64,$(subst x86_64,x64,$(shell uname -m)))
RUNTIME:=linux-$(ARCH)

all: publish

check:
	dotnet test -f $(FRAMEWORK) -c Release --verbosity detailed Turkey.Tests

run-samples:
	rm -rf ~/.nuget.orig && mv ~/.nuget ~/.nuget.orig && mkdir -p ~/.nuget
	cd Samples && test -f ../turkey/Turkey.dll && (dotnet ../turkey/Turkey.dll || true)
	rm -rf ~/.nuget && mv ~/.nuget.orig ~/.nuget

publish:
	git rev-parse --short HEAD > GIT_COMMIT_ID
	cat GIT_COMMIT_ID
	git describe --abbrev=0 | sed -e 's/^v//' > GIT_TAG_VERSION
	cat GIT_TAG_VERSION
	(cd Turkey; \
	 dotnet publish \
	 -f $(FRAMEWORK) \
	 -c $(CONFIGURATION) \
	 -p:VersionPrefix=$$(cat ../GIT_TAG_VERSION) \
	 -p:VersionSuffix=$$(cat ../GIT_COMMIT_ID) \
	 -o $$(readlink -f $$(pwd)/../turkey))
	tar czf turkey.tar.gz turkey/

clean:
	rm -f GIT_COMMIT_ID GIT_TAG_VERSION
	rm -rf Turkey/bin Turkey/obj
	rm -rf Turkey.Tests/bin Turkey.Tests/obj
	rm -rf bin
	rm -rf turkey turkey.tar.gz
	find -iname '*.log' -delete

fix-line-endings:
	find -iname '*.cs' -exec dos2unix {} \;
	find -iname '*.csproj' -exec dos2unix {} \;
	find -iname 'nuget.config' -exec dos2unix {} \;

list-todos:
	grep -r -E 'TODO|FIXME' *
