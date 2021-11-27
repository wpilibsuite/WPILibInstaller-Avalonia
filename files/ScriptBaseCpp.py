#!/usr/bin/env python

from __future__ import print_function
import os
import platform
import subprocess
import sys

script_name = os.path.abspath(sys.argv[0])
exe_name = os.path.splitext(script_name)[0]

if platform.system() == "Linux":
    cmd = [exe_name.lower(), *sys.argv[1:]]
elif platform.system() == "Darwin":
    cmd = ["open", exe_name + ".app", *sys.argv[1:]]
elif platform.system() == "Windows":
    cmd = [exe_name + ".exe", *sys.argv[1:]]
else:
    cmd = [exe_name, *sys.argv[1:]]

try:
    subprocess.Popen(cmd)
except Exception as e:
    print("Error launching tool:")
    print(e)
    exit(1)
