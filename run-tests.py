# .NET Bunny is a simple script that hops through the folders and runs dotnet tests based on json configuration.
# Radka Janek | rjanekov@redhat.com | https://github.com/redhat-developer/dotnet-bunny

import os
import sys
import subprocess
import shutil
import json
import re


class Test(object):

    def __init__(self, configPath, files):
        config = json.load(open(configPath))

        self.name = config["name"]
        self.type = config["type"]
        self.anyMinor = config["version"].split('.')[1] == "x"
        if self.anyMinor:
            self.version = int(config["version"].split('.')[0])
        else:
            self.version = int(config["version"].replace('.', ""))
        self.versionSpecific = config["versionSpecific"]
        self.shouldCleanup = config["cleanup"]
        self.files = files

    def setFrameworkVersion(self, path):
        if not os.path.exists(path):
            return

        with open(path, 'r') as i:
            content = i.read()
            content = frameworkExpression.sub("<TargetFramework>netcoreapp" + versionString + "</TargetFramework>", content)
        with open(path, 'w') as o:
            o.write(content)

    def copyProjectJson(self, path):
        shutil.copy(os.path.join(rootPath, "project" + version.__str__() + ("xunit" if self.type == "xunit" else "") + ".json"), os.path.join(path, "project.json"))

    # Returns the exit code of the test.
    def run(self, path):
        print "Running " + self.name
        logfile.writelines(self.name + ": Running test...\n")
        logfile.flush()

        if version >= 20:
            self.setFrameworkVersion(os.path.join(path, self.name + ".csproj"))
        else:
            self.copyProjectJson(path)

        #  import cStringIO
        #testlog = cStringIO.StringIO()
        # Apparently python does *not* have memory stream equivalent from C# so I have to write it all into *actual file.*
        # Rip performance. (This stringIO shit won't work cause the call expects file descriptor `fileno()`)

        testlogFilename = logfilename + "-" + self.name + ".log"
        testlog = self.name + "\n\n"
        errorCode = 1

        if self.type == "xunit":
            process = subprocess.Popen(["dotnet", "restore"], cwd=self.name, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            testlog = testlog + process.communicate()[0]
            errorCode = process.wait()
            if errorCode == 0:
                process = subprocess.Popen(["dotnet", "test"], cwd=self.name, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
                testlog = testlog + process.communicate()[0]
                errorCode = process.wait()
        elif self.type == "bash":
            process = subprocess.Popen(os.path.join(path, "test.sh"), cwd=self.name, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            testlog = testlog + process.communicate()[0]
            errorCode = process.wait()
        else:
            logfile.writelines(self.name + ": Unknown test type " + self.type + "\n")

        if errorCode > 0:
            with open(testlogFilename, 'w') as testlogFile:
                testlogFile.write(testlog)

        result = "Result: " + (("FAIL - Code: " + str(errorCode)) if errorCode > 0 else "PASS")
        logfile.writelines(self.name + ": " + result + "\n")
        print result
        return errorCode

    def cleanup(self, path):
        if self.shouldCleanup:
            logfile.writelines(self.name + ": Cleanup...\n")
            shutil.rmtree(os.path.join(path, "bin"), True)
            shutil.rmtree(os.path.join(path, "obj"), True)
            shutil.rmtree(os.path.join(path, "project.lock.json"), True)
            pass
        pass


class DotnetBunny(object):

    total = 0
    passed = 0
    failed = 0

    def __init__(self, rootPath):
        self.rootPath = rootPath

    def runTests(self):
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
                test = Test(path, files)
            except Exception as e:
                print "Failed to create the test " + subdir + " with Exception:\n" + e.__str__()
                logfile.writelines(test.name + ".Create Exception: {0}\n".format(e.__str__()))
                self.failed += 1
                continue

            if not ((test.versionSpecific and test.version == version) or
                    (test.versionSpecific and test.version == major and test.anyMinor) or
                (not test.versionSpecific and test.version <= version)):
                continue

            try:
                test.cleanup(subdir)
            except Exception as e:
                print "Failed to cleanup before the test " + subdir + " with Exception:\n" + e.__str__()
                logfile.writelines(test.name + ".Cleanup Exception: {0}\n".format(e.__str__()))

            self.total += 1
            try:
                result = test.run(subdir)
            except Exception as e:
                print "Failed to run the test " + subdir + " with Exception:\n" + e.__str__()
                logfile.writelines(test.name + ".Run Exception: {0}\n".format(e.__str__()))
                self.failed += 1
                continue

            if result > 0:
                self.failed += 1
                if exitOnFail:
                    break
            else:
                self.passed += 1

    def getResults(self):
        results = "Total: {0} Passed: {1} Failed: {2}".format(self.total, self.passed, self.failed)
        logfile.writelines("\n.NET Bunny: Results:\n")
        logfile.writelines(results + "\n")
        return "\n" + results

    def createResultsFile(self):
        with open("results.properties", "w") as resultsFile:
            resultsFile.write("tests.total={0}\ntests.passed={1}\ntests.failed={2}\n".format(self.total, self.passed, self.failed))

    def cleanup(self):
        logfile.writelines(".NET Bunny: Cleaning up...\n")
        shutil.rmtree("~/.nuget", True)
        shutil.rmtree("~/.local/share/NuGet", True)
        shutil.rmtree("~/.dotnet", True)
        shutil.rmtree("~/.templateengine", True)
        pass


print "\n(\\_/)\n(^_^)\n@(\")(\")\n"

helpString = "Usage: run-tests.py x.y [options]\n" \
       "        x.y - major and minor version of the dotnet package in use\n" \
       "        options:\n" \
       "          -e  - exit on the first failed test\n" \
       "          -r  - create results.properties file for jenkins\n" \
       "          -h  - display this help"

if len(sys.argv) < 2:
    print helpString
    sys.exit(1)

exitOnFail = False
createResultsFile = False

for arg in sys.argv:
    if arg == "-e":
        exitOnFail = True
        continue

    if arg == "-r":
        createResultsFile = True
        continue

    if arg == "-h" or arg == "--help":
        print helpString
        sys.exit(0)

logfilename = "logfile"
logfile = open(logfilename + ".log", "w")
logfile.writelines("\n\n(\\_/)\n(^_^)\n@(\")(\")\n\n")

versionString = sys.argv[1]
version = int(versionString.replace('.', ""))
major = int(versionString.split('.')[0])

frameworkExpression = re.compile("<TargetFramework>netcoreapp\d\.\d</TargetFramework>", re.M)

rootPath = os.path.abspath(os.path.curdir)
dotnetBunny = DotnetBunny(rootPath)

dotnetBunny.cleanup()
dotnetBunny.runTests()
print dotnetBunny.getResults()
if createResultsFile:
    dotnetBunny.createResultsFile()

dotnetBunny.cleanup()
logfile.flush()
logfile.close()
exit(dotnetBunny.failed)
