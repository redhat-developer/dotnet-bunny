FRAMEWORK:=netcoreapp3.1
CONFIGURATION:=Release
RUNTIME:=linux-$(subst aarch64,arm64,$(subst x86_64,x64,$(shell uname -m)))

all: publish

check:
	dotnet test -c Release --verbosity detailed Turkey.Tests

run-samples:
	rm -rf ~/.nuget.orig && mv ~/.nuget ~/.nuget.orig && mkdir -p ~/.nuget
	cd Samples && test -f ../bin/turkey && (../bin/turkey || true)
	rm -rf ~/.nuget && mv ~/.nuget.orig ~/.nuget

publish:
	git rev-parse --short HEAD > GIT_COMMIT_ID
	cat GIT_COMMIT_ID
	git describe --abbrev=0 | sed -e 's/^v//' > GIT_TAG_VERSION
	cat GIT_TAG_VERSION
	(cd Turkey; \
	 dotnet publish \
	 -c $(CONFIGURATION) \
	 -r $(RUNTIME) \
	 -p:VersionPrefix=$$(cat ../GIT_TAG_VERSION) \
	 -p:VersionSuffix=$$(cat ../GIT_COMMIT_ID) \
	 -p:PublishSingleFile=true \
	 -p:PublishTrimmed=true)
	mkdir -p bin
	cp -a ./Turkey/bin/$(CONFIGURATION)/$(FRAMEWORK)/$(RUNTIME)/publish/Turkey bin/turkey

clean:
	rm -rf Turkey/bin Turkey/obj
	rm -rf Turkey.Tests/bin Turkey.Tests/obj
	rm -rf bin
	find -iname '*.log' -delete

fix-line-endings:
	find -iname '*.cs' -exec dos2unix {} \;
	find -iname '*.csproj' -exec dos2unix {} \;
	find -iname 'nuget.config' -exec dos2unix {} \;

list-todos:
	grep -r -E 'TODO|FIXME' *
