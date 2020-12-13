#!/usr/bin/env python

from __future__ import print_function
import os
import subprocess
import sys
import time

script_name = os.path.abspath(sys.argv[0])
jar_name = os.path.splitext(script_name)[0] + ".jar"

jdk_dir = os.path.join(os.path.dirname(jar_name), "jdk", "bin", "java")

try:
    p = subprocess.Popen(
        [jdk_dir, "-jar", jar_name], stdout=subprocess.PIPE, stderr=subprocess.PIPE
    )
except:
    # Start failed. Try JAVA_HOME.
    try:
        jdk_dir = os.path.join(os.environ["JAVA_HOME"], "bin", "java")
    except:
        # No JAVA_HOME. Try just running java from PATH.
        jdk_dir = "java"
    try:
        p = subprocess.Popen(
            [jdk_dir, "-jar", jar_name],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except Exception as e:
        # Really error
        print("Error launching tool:")
        print(e)
        exit(1)

# Wait 3 seconds and print stdout/stderr if the process exits
for i in range(3):
    time.sleep(1)
    if p.poll():
        print(p.stdout.read().decode("utf-8"))
        print(p.stderr.read().decode("utf-8"), file=sys.stderr)
