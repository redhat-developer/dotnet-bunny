#!/usr/bin/env python

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
            self.enabled = config["enabled"]
            self.type = config["type"]
            self.anyMinor = config["version"].split('.')[1] == "x"
            if self.anyMinor:
                self.version = int(config["version"].split('.')[0])
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

        def copyProjectJson(self, path):
            if debug:
                print("Test.copyProjectJson( " + str(path) + " )")

            shutil.copy(os.path.join(rootPath, "project" + str(major) + str(minor) + (
                "xunit" if self.type == "xunit" else "") + ".json"), os.path.join(path, "project.json"))

            if debug:
                print("Test.copyProjectJson() DONE")

        # Returns the exit code of the test.
        def run(self, path):
            if debug:
                print("Test.run( " + str(path) + " )")

            print("Running " + self.name)
            logfile.writelines(self.name + ": Running test...\n")
            logfile.flush()

            if version >= 20000:
                self.setFrameworkVersion(os.path.join(path, self.name + ".csproj"))
            else:
                self.copyProjectJson(path)

            testlogFilename = logfilename + "-" + self.name + ".log"
            testlog = ""
            errorCode = 1

            nuGetConfigLocation = os.path.join(path, "nuget.config")
            if nuGetConfig:
                if os.path.exists(nuGetConfigLocation):
                    print("error: nugetconfig at %s already exists " % (nuGetConfigLocation,))
                    exit(2)
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
                logfile.writelines(self.name + ":  " + testlog.replace("\n", prefix) + "\n")

            result = "Result: " + (("FAIL - Code: " + str(errorCode)) if errorCode > 0 else "PASS")
            logfile.writelines(self.name + ":  " + result + "\n\n")
            print(result)

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
                shutil.rmtree(os.path.join(path, "project.lock.json"), True)

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
                msg, traceback = getExceptionTrace()
                print("Failed to create the test {0} with " + msg)
                logfile.writelines(path + ".Create " + msg)
                sys.stdout.flush()
                traceback.print_tb(traceback)
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
                exc_type, exc_obj, exc_tb = sys.exc_info()
                fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
                print("Failed to cleanup before the test {0} with Exception: {1}\n{2}\n{3} @ {4}".format(subdir, exc_type, str(e), fname, exc_tb.tb_lineno))
                logfile.writelines(test.name + ".Cleanup Exception: {0}\n{1}\n{2} @ {3}".format(exc_type, str(e), fname, exc_tb.tb_lineno))
                sys.stdout.flush()
                traceback.print_tb(exc_tb)

            self.total += 1
            try:
                result = test.run(subdir)
            except Exception as e:
                exc_type, exc_obj, exc_tb = sys.exc_info()
                fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
                print("Failed to run the test {0} with Exception: {1}\n{2}\n{3} @ {4}".format(subdir, exc_type, str(e), fname, exc_tb.tb_lineno))
                logfile.writelines(test.name + ".Run Exception: {0}\n{1}\n{2} @ {3}".format(exc_type, str(e), fname, exc_tb.tb_lineno))
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
    "Return a tuple of (message, traceback))"
    exc_type, exc_obj, exc_tb = sys.exc_info()
    fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
    return ("Exception: {0}\n{1}\n{2} @ {3}".format(exc_type, str(e), fname, exc_tb.tb_lineno), exec_tb)

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
    return response.read().strip()

def generateNuGetConfigContentsForFeed(url):
    return """<?xml version="1.0" encoding="utf-8"?>
<configuration>
 <packageSources>
    <add key="prodcon" value="%s" />
 </packageSources>
</configuration>

""" % (url,)

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


print("\n(\\_/)\n(^_^)\n@(\")(\")\n")

helpString = "Usage: run-tests.py x.y [options]\n" \
       "        x.y - major and minor version of the dotnet package in use\n" \
       "        options:\n" \
       "          -p=rhelX|fedora|fedoraXY - platform\n" \
       "          -e  - exit on the first failed test\n" \
       "          -v  - verbose logfile.log output\n" \
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

versionString = sys.argv[1]
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
    branchName = "release/" + majorMinorString
    prodConUrl = getProdConFeedUrl(branchName)
    message = "Packages for runtime version %s are not live on nuget.org, using prodcon nuget repository %s\n" \
        % (latestDotNetRuntimeVersion, prodConUrl)
    print(message)
    logfile.writelines(message)
    nuGetConfig = generateNuGetConfigContentsForFeed(prodConUrl)
    if debug:
        print(nuGetConfig)

rootPath = os.path.abspath(os.path.curdir)
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
