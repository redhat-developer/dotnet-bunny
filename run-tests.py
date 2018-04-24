# .NET Bunny is a simple script that hops through the folders and runs dotnet tests based on json configuration.
# Radka Janek | rjanekov@redhat.com | https://github.com/redhat-developer/dotnet-bunny

import os
import sys
import subprocess
import shutil
import json
import re
import cStringIO


class Test(object):

    def __init__(self, configPath, files):
        config = json.load(open(configPath))

        self.name = config["name"]
        self.type = config["type"]
        self.version = int(config["version"].replace('.', ""))
        self.versionSpecific = config["versionSpecific"]
        self.shouldCleanup = config["cleanup"]
        self.files = files

    def setFrameworkVersion(self, path):
        with open(path, 'r') as i:
            content = i.read()
            content = frameworkExpression.sub("<TargetFramework>netcoreapp" + versionString + "</TargetFramework>", content)
            i.close()
        with open(path, 'w') as o:
            o.write(content)
            o.close()

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

        errorCode = 1

        if self.type == "xunit":
            errorCode = subprocess.call(["dotnet", "restore"], cwd=self.name, stdout=logfile, stderr=logfile)
            if errorCode == 0:
                errorCode = subprocess.call(["dotnet", "test"], cwd=self.name, stdout=logfile, stderr=logfile)
        elif self.type == "bash":
            errorCode = subprocess.call([os.path.join(path, "test.sh")], cwd=self.name, stdout=logfile, stderr=logfile)
        else:
            logfile.writelines(self.name + ": Unknown test type " + self.type + "\n")

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
                logfile.writelines(".NET Bunny: {0}\n".format(e.__str__()))
                self.failed += 1
                continue

            if ((test.versionSpecific and test.version == version) or
            (not test.versionSpecific and test.version > version)):
                continue

            try:
                test.cleanup(subdir)
            except Exception as e:
                print "Failed to cleanup before the test " + subdir + " with Exception:\n" + e.__str__()
                logfile.writelines(".NET Bunny: " + e.__str__() + "\n")

            try:
                result = test.run(subdir)
            except Exception as e:
                print "Failed to run the test " + subdir + " with Exception:\n" + e.__str__()
                logfile.writelines(".NET Bunny: " + e.__str__() + "\n")
                self.failed += 1
                continue

            self.total += 1
            if result == 1:
                self.failed += 1
                if exitOnFail:
                    break
            else:
                self.passed += 1

            try:
                test.cleanup(subdir)
            except Exception as e:
                print "Failed to cleanup after the test " + subdir + " with Exception:\n" + e.__str__()
                logfile.writelines(".NET Bunny: " + e.__str__() + "\n")

    def getResults(self):
        results = "Total: " + str(self.total) + " Passed: " + str(self.passed) + " Failed: " + str(self.failed)
        logfile.writelines("\n.NET Bunny: Results:\n")
        logfile.writelines(results + "\n")
        return "\n" + results

    def cleanup(self):
        logfile.writelines(".NET Bunny: Cleaning up...\n")
        shutil.rmtree("~/.nuget", True)
        shutil.rmtree("~/.local/share/NuGet", True)
        shutil.rmtree("~/.dotnet", True)
        shutil.rmtree("~/.templateengine", True)
        pass


print "\n(\\_/)\n(^_^)\n@(\")(\")\n"

if len(sys.argv) < 3:
    print "Usage: run-tests.py x.y t/f\n" \
          "        x.y - major and minor version of the dotnet package in use\n" \
          "        t/f - true or false, whether to exit on failed test, or not"
    sys.exit(1)

logfile = open("logfile", "w")

logfile.writelines("\n\n(\\_/)\n(^_^)\n@(\")(\")\n\n")
versionString = sys.argv[1]
version = int(versionString.replace('.', ""))
exitOnFail = True if sys.argv[2] == "true" else False
frameworkExpression = re.compile("<TargetFramework>netcoreapp\d\.\d</TargetFramework>", re.M)

rootPath = os.path.abspath(os.path.curdir)
dotnetBunny = DotnetBunny(rootPath)

dotnetBunny.cleanup()
dotnetBunny.runTests()
print dotnetBunny.getResults()
dotnetBunny.cleanup()

logfile.flush()
logfile.close()
exit(dotnetBunny.failed)
