#!/usr/bin/env python3

# .NET Bunny is a simple script that hops through the folders and runs dotnet tests based on json configuration.
# Radka Janek | rjanekov@redhat.com | https://github.com/redhat-developer/dotnet-bunny

from __future__ import print_function
import os
import traceback
import sys
import subprocess
import shutil
import json
import re
try:
    from urllib2 import urlopen
except:
    from urllib.request import urlopen


class DotnetBunny(object):

    class Test(object):

        def __init__(self, configPath, files):
            if debug:
                print("Test.__init__( " + str(configPath) + " )")

            config = json.load(open(configPath))

            self.name = config["name"]
            directory_name = os.path.basename(os.path.dirname(configPath))
            if self.name != directory_name:
                print("error: mismatch in directory name '%s' vs test name '%s' in '%s'" %
                    (directory_name, self.name, configPath))
                exit(3)

            self.enabled = config["enabled"]
            self.type = config["type"]
            self.anyMinor = config["version"].split('.')[1] == "x"
            if self.anyMinor:
                self.version = int(config["version"].split('.')[0] + '0')
            else:
                self.version = int(config["version"].replace('.', ""))
            if self.version < 10000:
                self.version = self.version * 1000

            self.versionSpecific = config["versionSpecific"]
            self.platformBlacklist = config["platformBlacklist"]
            for p in self.platformBlacklist:
                if not p in knownPlatforms:
                    print("Unknown platform %s in test %s" % (p, self.name))

            self.shouldCleanup = config["cleanup"]
            self.files = files

            if debug:
                print("Test.__init__() DONE")

        def setFrameworkVersion(self, path):
            if debug:
                print("Test.setFrameworkVersion( " + str(path) + " )")

            if not os.path.exists(path):
                return

            with open(path, 'r') as i:
                content = i.read()
                content = frameworkExpression.sub("<TargetFramework>netcoreapp" + majorMinorString + "</TargetFramework>",
                                                  content)
            with open(path, 'w') as o:
                o.write(content)

            if debug:
                print("Test.setFrameworkVersion() DONE")

        # Returns the exit code of the test.
        def run(self, path):
            if debug:
                print("Test.run( " + str(path) + " )")

            print("Running " + self.name)
            logfile.writelines(self.name + ": Running test...\n")
            logfile.flush()

            self.setFrameworkVersion(os.path.join(path, self.name + ".csproj"))

            testlogFilename = logfilename + "-" + self.name + ".log"
            testlog = ""
            errorCode = 1

            nuGetConfigLocation = os.path.join(path, "nuget.config")
            if nuGetConfig:
                if os.path.exists(nuGetConfigLocation):
                    print("error: nugetconfig at %s already exists " % (nuGetConfigLocation,))
                    exit(2)
                if debug:
                    print("Test.run(): adding nuget config")
                with open(nuGetConfigLocation, "w") as nugetConfig:
                    nugetConfig.write(nuGetConfig)

            if self.type == "xunit":
                try:
                    process = subprocess.Popen(["dotnet", "restore"], cwd=path, stdout=subprocess.PIPE,
                                               stderr=subprocess.STDOUT, universal_newlines=True)
                    testlog = testlog + process.communicate()[0]
                    errorCode = process.wait()
                    if errorCode == 0:
                        process = subprocess.Popen(["dotnet", "test"], cwd=path, stdout=subprocess.PIPE,
                                                   stderr=subprocess.STDOUT, universal_newlines=True)
                        testlog = testlog + process.communicate()[0]
                        errorCode = process.wait()
                except Exception as e:
                    msg, _ = getExceptionTrace()
                    testlog = testlog + "Process " + msg
                    errorCode = 1
            elif self.type == "bash":
                try:
                    mypath = os.path.join(path, "test.sh")
                    process = subprocess.Popen([mypath, versionString], cwd=path, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, universal_newlines=True)
                    testlog = testlog + process.communicate()[0]
                    errorCode = process.wait()
                except Exception as e:
                    msg, _ = getExceptionTrace()
                    testlog = testlog + "Process " + msg
                    errorCode = 1
            else:
                logfile.writelines(self.name + ": Unknown test type " + self.type + "\n")

            if errorCode > 0:
                with open(os.path.join(logDirectory, testlogFilename), 'w') as testlogFile:
                    testlogFile.write(self.name + " log:\n\n" + testlog)

            if nuGetConfig and os.path.exists(nuGetConfigLocation):
                os.remove(nuGetConfigLocation)

            if verbose:
                prefix = "\n" + self.name + ":  "
                print(self.name + ":  " + testlog.replace("\n", prefix))

            result = "Result: " + (("FAIL - Code: " + str(errorCode)) if errorCode > 0 else "PASS")
            logfile.writelines(self.name + ":  " + result + "\n\n")
            print(result + "\n")

            if debug:
                print("Test.run() DONE")

            return errorCode

        def cleanup(self, path):
            if debug:
                print("Test.cleanup( " + str(path) + " )")

            if self.shouldCleanup:
                logfile.writelines(self.name + ": Cleanup...\n")
                shutil.rmtree(os.path.join(path, "bin"), True)
                shutil.rmtree(os.path.join(path, "obj"), True)

            if debug:
                print("Test.cleanup() DONE")

    def __init__(self, rootPath):
        if debug:
            print("DotnetBunny.__init__( " + str(rootPath) + " )")

        self.rootPath = rootPath
        self.total = 0
        self.passed = 0
        self.failed = 0

    def runTests(self):
        if debug:
            print("DotnetBunny.runTests()")

        logfile.writelines(".NET Bunny: Running tests...\n")

        # TODO: This could be faster, if I knew how to achieve this in python:
        # In cs I'd make this async load of json configs and drop the tasks in a list,
        # then iterate through, awaiting each and running the test.
        # It probably requires 3.x python as well, while this is 2.7 code right now.
        for subdir, dirs, files in os.walk(self.rootPath):
            path = os.path.join(subdir, "test.json")
            if not os.path.exists(path):
                continue

            try:
                test = DotnetBunny.Test(path, files)
            except Exception as e:
                msg, tb = getExceptionTrace()
                print("Failed to create the test {0} with {1}".format(subdir, msg))
                logfile.writelines(path + ".Create " + msg)
                sys.stdout.flush()
                traceback.print_tb(tb)
                self.failed += 1
                continue

            if not test.enabled and not executeDisabled:
                continue

            if not ((test.versionSpecific and test.version == version) or
                    (test.versionSpecific and test.version == major and test.anyMinor) or
                (not test.versionSpecific and test.version <= version)):
                continue

            if any(platform in test.platformBlacklist for platform in platforms):
                continue

            try:
                test.cleanup(subdir)
            except Exception as e:
                msg, exc_tb = getExceptionTrace()
                print("Failed to cleanup before the test {0} with {1}".format(subdir, msg))
                logfile.writelines(test.name + ".Cleanup " + msg)
                sys.stdout.flush()
                traceback.print_tb(exc_tb)

            self.total += 1
            try:
                result = test.run(subdir)
            except Exception as e:
                msg, exc_tb = getExceptionTrace()
                print("Failed to run the test {0} with  {1}".format(subdir, msg))
                logfile.writelines(test.name + ".Run " + msg)
                sys.stdout.flush()
                traceback.print_tb(exc_tb)
                self.failed += 1
                continue

            if result > 0:
                self.failed += 1
                if exitOnFail:
                    break
            else:
                self.passed += 1

    def getResults(self):
        if debug:
            print("DotnetBunny.getResults()")

        results = "Total: {0} Passed: {1} Failed: {2}".format(self.total, self.passed, self.failed)
        logfile.writelines("\n.NET Bunny: Results:\n")
        logfile.writelines(results + "\n")
        return "\n" + results

    def createResultsFile(self):
        if debug:
            print("DotnetBunny.createResultsFile()")

        with open(os.path.join(logDirectory, "results.properties"), "w") as resultsFile:
            resultsFile.write("tests.total={0}\ntests.passed={1}\ntests.failed={2}\n".format(self.total, self.passed, self.failed))

    def cleanup(self):
        if debug:
            print("DotnetBunny.cleanup()")

        logfile.writelines(".NET Bunny: Cleaning up...\n")
        shutil.rmtree("~/.nuget/packages", True)
        shutil.rmtree("~/.local/share/NuGet", True)
        shutil.rmtree("~/.dotnet", True)
        shutil.rmtree("~/.templateengine", True)


def getExceptionTrace():
    "Return a tuple of (message, traceback)"
    exc_type, exc_obj, exc_tb = sys.exc_info()
    fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
    return ("Exception: {0}\n{1}\n{2} @ {3}".format(exc_type, str(exc_obj), fname, exc_tb.tb_lineno), exc_tb)

def getDotNetRuntimeVersion():
    "Guess the latest runtime version for the default dotnet on the command line"
    process = subprocess.Popen(["dotnet", "--list-runtimes"],
                               stdout=subprocess.PIPE,
                               universal_newlines=True)
    errorCode = process.wait()
    if errorCode:
        return None
    output = process.communicate()[0]
    netCoreAppVersions = [line.split(" ")[1]
                          for line in output.split("\n")
                          if line.startswith("Microsoft.NETCore.App")]
    latest = sorted(netCoreAppVersions)[-1]
    return latest

def nugetPackagesAreLive(version):
    "True if the Microsoft.NETCore.App packages are available on nuget.org for the given version."
    # See https://docs.microsoft.com/en-us/nuget/api/search-autocomplete-service-resource
    url = "https://api-v2v3search-0.nuget.org/autocomplete?id=microsoft.netcore.app&prerelease=true"
    response = urlopen(url)
    jsonResponse = json.load(response)
    found = version in jsonResponse["data"]
    return found

def getProdConFeedUrl(branchName):
    "Find the prodcon url for the given release branch of github.com/dotnet/source-build."
    url = "https://raw.githubusercontent.com/dotnet/source-build/" + branchName + "/ProdConFeed.txt"
    response = urlopen(url)
    url = response.read().strip().decode('utf-8')
    try:
        urlopen(url)
        return url
    except:
        print('ProdCon URL %s is invalid, ignoring' % (url,))
        return None

def generateNuGetConfigContentsForFeeds(urls):
    sources = '\n    '.join('<add key="%s" value="%s" />' % (index, url) for index, url in enumerate(urls))
    return """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    %s
  </packageSources>
</configuration>
""" % (sources,)

def getKnownPlatforms():
    platforms = []
    platforms.append("rhel")
    platforms.extend(["rhel" + str(i) for i in range(6,10)])
    platforms.append("fedora")
    platforms.extend(["fedora" + str(i) for i in range(26,40)])
    return platforms


def identifyPlatform():
    """Return a list of platforms that this platform is compatible with

For example, Fedora 28 will return ['fedora', 'fedora28']
"""
    name_id = ""
    version_id = ""
    with open("/etc/os-release") as os_release:
        for line in os_release:
            line = line.strip()
            key = line.split("=")[0]
            value = '='.join(line.split("=")[1:])
            value = unquoteShellValue(value)
            if key == "ID":
                name_id = value
            elif key == "VERSION_ID":
                if "." in value:
                    version_id = value[:value.index(".")]
                else:
                    version_id = value
    return [name_id, name_id + version_id]


def unquoteShellValue(value):
    if value.startswith('"') and value.endswith('"'):
        value = value[1:-1]
    return value


def printUsefulSystemInformation():
    processFree = subprocess.Popen(["free", "-h"], cwd=rootPath, stdout=subprocess.PIPE,
                                   stderr=subprocess.STDOUT, universal_newlines=True)
    print("Current resources:\n" + processFree.communicate()[0])

    # Lets use a filtered list to reduce noise and also to keep any
    # confidential data out.
    environmentVariables = [
        'ASPNETCORE_ENVIRONMENT',
        'CFLAGS',
        'COLORTERM',
        'CPATH',
        'CXXFLAGS',
        'DEBUG',
        'DISPLAY',
        'DOTNET_CLI_TELEMETRY_OPTOUT',
        'DOTNET_MULTILEVEL_LOOKUP',
        'DOTNET_PACKAGES',
        'DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX',
        'DOTNET_ROOT',
        'DOTNET_RUNTIME_ID',
        'DOTNET_SERVICING',
        'DOTNET_SKIP_FIRST_TIME_EXPERIENCE',
        'DOTNET_SYSTEM_BUFFERS_ARRAYPOOL_TRIMSHARED',
        'DOTNET_SYSTEM_GLOBALIZATION_INVARIANT',
        'DOTNET_SYSTEM_RUNTIME_CACHING_TRACING',
        'EDITOR',
        'HOME',
        'HOSTNAME',
        'INFOPATH',
        'LANG',
        'LD_LIBRARY_PATH',
        'LIBRARY_PATH',
        'MANPATH',
        'MODULEPATH',
        'OSTYPE',
        'PAGER',
        'PATH',
        'PKG_CONFIG_PATH',
        'POSIXLY_CORRECT',
        'PWD',
        'PYTHONDEBUG',
        'PYTHONHOME',
        'PYTHONINSPECT',
        'PYTHONOPTIMIZE',
        'PYTHONPATH',
        'PYTHONSTARTUP',
        'PYTHONUNBUFFERED',
        'PYTHONUTF8',
        'PYTHONVERBOSE',
        'PYTHONWARNINGS',
        'SHELL',
        'TERM',
        'TZ',
        'USER',
        'XDG_CURRENT_DESKTOP',
        'XDG_DATA_DIRS',
        'XDG_RUNTIME_DIR',
        'XDG_SESSION_DESKTOP',
        'XDG_SESSION_TYPE',
    ]

    print("Current (filtered) environment:")
    for envVar in environmentVariables:
        if envVar in os.environ:
            print(envVar + "=" + os.environ[envVar])


print("\n(\\_/)\n(^_^)\n@(\")(\")\n")

helpString = "Usage: run-tests.py x.y [options]\n" \
       "        x.y - major and minor version of the dotnet package in use\n" \
       "        options:\n" \
       "          -p=rhelX|fedora|fedoraXY - platform\n" \
       "          -e  - exit on the first failed test\n" \
       "          -s=url - additional nuget source(s)\n" \
       "          -v  - verbose console output\n" \
       "          -r  - create results.properties file for jenkins\n" \
       "          -x  - execute disabled tests as well\n" \
       "          -d  - debug console spam\n" \
       "          -h  - display this help"

if len(sys.argv) < 2:
    print(helpString)
    sys.exit(1)

exitOnFail = False
verbose = False
createResultsFile = False
executeDisabled = False
debug = False
platforms = identifyPlatform()
knownPlatforms = getKnownPlatforms()
logDirectory = os.getcwd()
nuGetUrls = []
nuGetConfig = ""

for arg in sys.argv:
    if arg.startswith("-p="):
        platform_string = arg[3:]
        platforms = platform_string.split(",")
        if any(platform in knownPlatforms for platform in platforms):
            continue
        print("Unknown platforms: " + str(platforms))
        print("Known platforms are: " + str(knownPlatforms))
        exit(1)

    if arg.startswith("-s="):
        url = arg[3:]
        nuGetUrls.append(url)

    if arg == "-e":
        exitOnFail = True
        continue

    if arg == "-v":
        verbose = True
        continue

    if arg == "-r":
        createResultsFile = True
        continue

    if arg == "-x":
        executeDisabled = True
        continue

    if arg == "-d":
        debug = True
        continue

    if arg.startswith("-l="):
        logDirectory = os.path.join(logDirectory, arg[3:])
        if not os.path.isdir(logDirectory):
            print("Log directory %s doesn't exist" % (logDirectory,))
            sys.exit(1)
        continue

    if arg == "-h" or arg == "--help":
        print(helpString)
        sys.exit(0)

if debug:
    print("Known Platforms: " + str(knownPlatforms))
    print("Current Platforms: " + str(platforms))

try:
    reload(sys)
    sys.setdefaultencoding('utf8')
except NameError:
    pass  # python 3 is already utf8

logfilename = "logfile"
logfile = open(os.path.join(logDirectory, logfilename) + ".log", "w")
logfile.writelines("\n\n(\\_/)\n(^_^)\n@(\")(\")\n\n")

match = re.search(r'^\d\.\d\.\d+', sys.argv[1])
#match = re.search("^[0-9.]+", sys.argv[1])
versionString = match.group(0)
versionArray = versionString.split('.')
majorMinorString = versionArray[0] + "." + versionArray[1]
major = int(versionArray[0])
minor = int(versionArray[1])
version = int(versionString.replace('.', ""))
if version < 10000:
    version = version * 1000

frameworkExpression = re.compile(r"<TargetFramework>netcoreapp\d\.\d</TargetFramework>", re.M)

latestDotNetRuntimeVersion = getDotNetRuntimeVersion()
if latestDotNetRuntimeVersion and not nugetPackagesAreLive(latestDotNetRuntimeVersion):
    message = "Packages for runtime version %s are not live on nuget.org" % (latestDotNetRuntimeVersion, )
    print(message)
    logfile.writelines(message)
    branchName = "release/" + majorMinorString
    prodConUrl = getProdConFeedUrl(branchName)
    if prodConUrl:
        message = "Using prodcon nuget repository %s\n" % (prodConUrl, )
        print(message)
        nuGetUrls.append(prodConUrl)
        logfile.writelines(message)

if nuGetUrls:
    nuGetConfig = generateNuGetConfigContentsForFeeds(nuGetUrls)
    if debug:
        print("Using nuget config:")
        print(nuGetConfig)

rootPath = os.path.abspath(os.path.curdir)

printUsefulSystemInformation();

dotnetBunny = DotnetBunny(rootPath)

dotnetBunny.cleanup()
dotnetBunny.runTests()
print(dotnetBunny.getResults())
if createResultsFile:
    dotnetBunny.createResultsFile()

dotnetBunny.cleanup()
logfile.flush()
logfile.close()
exit(dotnetBunny.failed)
