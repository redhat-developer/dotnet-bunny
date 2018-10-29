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


class DotnetBunny(object):

    class Test(object):

        def __init__(self, configPath, files):
            if debug:
                print("Test.__init__( " + configPath.__str__() + " )")

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
            self.shouldCleanup = config["cleanup"]
            self.files = files

            if debug:
                print("Test.__init__() DONE")

        def setFrameworkVersion(self, path):
            if debug:
                print("Test.setFrameworkVersion( " + path.__str__() + " )")

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
                print("Test.copyProjectJson( " + path.__str__() + " )")

            shutil.copy(os.path.join(rootPath, "project" + major.__str__() + minor.__str__() + (
                "xunit" if self.type == "xunit" else "") + ".json"), os.path.join(path, "project.json"))

            if debug:
                print("Test.copyProjectJson() DONE")

        # Returns the exit code of the test.
        def run(self, path):
            if debug:
                print("Test.run( " + path.__str__() + " )")

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

            if self.type == "xunit":
                try:
                    process = subprocess.Popen(["dotnet", "restore"], cwd=path, stdout=subprocess.PIPE,
                                               stderr=subprocess.STDOUT)
                    testlog = testlog + process.communicate()[0]
                    errorCode = process.wait()
                    if errorCode == 0:
                        process = subprocess.Popen(["dotnet", "test"], cwd=path, stdout=subprocess.PIPE,
                                                   stderr=subprocess.STDOUT)
                        testlog = testlog + process.communicate()[0]
                        errorCode = process.wait()
                except Exception as e:
                    exc_type, exc_obj, exc_tb = sys.exc_info()
                    fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
                    testlog = testlog + "Process Exception: {0}\n{1} @ {2}".format(e.__str__(), fname, exc_tb.tb_lineno)
                    errorCode = 1
            elif self.type == "bash":
                try:
                    mypath = os.path.join(path, "test.sh")
                    process = subprocess.Popen([mypath, versionString], cwd=path, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
                    testlog = testlog + process.communicate()[0]
                    errorCode = process.wait()
                except Exception as e:
                    exc_type, exc_obj, exc_tb = sys.exc_info()
                    fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
                    testlog = testlog + "Process Exception: {0}\n{1} @ {2}".format(e.__str__(), fname, exc_tb.tb_lineno)
                    errorCode = 1
            else:
                logfile.writelines(self.name + ": Unknown test type " + self.type + "\n")

            if errorCode > 0:
                with open(testlogFilename, 'w') as testlogFile:
                    testlogFile.write(self.name + " log:\n\n" + testlog)

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
                print("Test.cleanup( " + path.__str__() + " )")

            if self.shouldCleanup:
                logfile.writelines(self.name + ": Cleanup...\n")
                shutil.rmtree(os.path.join(path, "bin"), True)
                shutil.rmtree(os.path.join(path, "obj"), True)
                shutil.rmtree(os.path.join(path, "project.lock.json"), True)
                pass
            pass

            if debug:
                print("Test.cleanup() DONE")

    def __init__(self, rootPath):
        if debug:
            print("DotnetBunny.__init__( " + rootPath.__str__() + " )")

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
                exc_type, exc_obj, exc_tb = sys.exc_info()
                fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
                print("Failed to create the test {0} with Exception: {1}\n{2}\n{3} @ {4}".format(subdir, exc_type, e.__str__(), fname, exc_tb.tb_lineno))
                logfile.writelines(path + ".Create Exception: {0}\n{1}\n{2} @ {3}".format(exc_type, e.__str__(), fname, exc_tb.tb_lineno))
                sys.stdout.flush()
                traceback.print_tb(exc_tb)
                self.failed += 1
                continue

            if not test.enabled and not executeDisabled:
                continue

            if not ((test.versionSpecific and test.version == version) or
                    (test.versionSpecific and test.version == major and test.anyMinor) or
                (not test.versionSpecific and test.version <= version)):
                continue

            if any(platform in s for s in test.platformBlacklist):
                continue

            try:
                test.cleanup(subdir)
            except Exception as e:
                exc_type, exc_obj, exc_tb = sys.exc_info()
                fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
                print("Failed to cleanup before the test {0} with Exception: {1}\n{2}\n{3} @ {4}".format(subdir, exc_type, e.__str__(), fname, exc_tb.tb_lineno))
                logfile.writelines(test.name + ".Cleanup Exception: {0}\n{1}\n{2} @ {3}".format(exc_type, e.__str__(), fname, exc_tb.tb_lineno))
                sys.stdout.flush()
                traceback.print_tb(exc_tb)

            self.total += 1
            try:
                result = test.run(subdir)
            except Exception as e:
                exc_type, exc_obj, exc_tb = sys.exc_info()
                fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)[1]
                print("Failed to run the test {0} with Exception: {1}\n{2}\n{3} @ {4}".format(subdir, exc_type, e.__str__(), fname, exc_tb.tb_lineno))
                logfile.writelines(test.name + ".Run Exception: {0}\n{1}\n{2} @ {3}".format(exc_type, e.__str__(), fname, exc_tb.tb_lineno))
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

        with open("results.properties", "w") as resultsFile:
            resultsFile.write("tests.total={0}\ntests.passed={1}\ntests.failed={2}\n".format(self.total, self.passed, self.failed))

    def cleanup(self):
        if debug:
            print("DotnetBunny.cleanup()")

        logfile.writelines(".NET Bunny: Cleaning up...\n")
        shutil.rmtree("~/.nuget/packages", True)
        shutil.rmtree("~/.local/share/NuGet", True)
        shutil.rmtree("~/.dotnet", True)
        shutil.rmtree("~/.templateengine", True)
        pass


print("\n(\\_/)\n(^_^)\n@(\")(\")\n")

helpString = "Usage: run-tests.py x.y [options]\n" \
       "        x.y - major and minor version of the dotnet package in use\n" \
       "        options:\n" \
       "          -p=rhel7|rhel8|fedora - platform, defaults to rhel7\n" \
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
platform = "rhel7"
compatiblePlatforms = ["rhel7", "rhel8", "fedora"]

for arg in sys.argv:
    if arg.startswith("-p="):
        platform = arg[3:]
        if any(platform in s for s in compatiblePlatforms):
            continue
        print("Invalid platform!")
        exit(0)

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

    if arg == "-h" or arg == "--help":
        print(helpString)
        sys.exit(0)

reload(sys)
sys.setdefaultencoding('utf8')

logfilename = "logfile"
logfile = open(logfilename + ".log", "w")
logfile.writelines("\n\n(\\_/)\n(^_^)\n@(\")(\")\n\n")

versionString = sys.argv[1]
versionArray = versionString.split('.')
majorMinorString = versionArray[0] + "." + versionArray[1]
major = int(versionArray[0])
minor = int(versionArray[1])
version = int(versionString.replace('.', ""))
if version < 10000:
    version = version * 1000

frameworkExpression = re.compile("<TargetFramework>netcoreapp\d\.\d</TargetFramework>", re.M)

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
