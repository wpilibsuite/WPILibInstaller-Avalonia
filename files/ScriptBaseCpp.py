#!/usr/bin/env python3

from __future__ import print_function
import os
import platform
import subprocess
import sys

script_name = os.path.abspath(sys.argv[0])
exe_name = os.path.splitext(script_name)[0]

if platform.system() == "Linux":
    exe_path, exe_name = os.path.split(exe_name)
    cmd = [os.path.join(exe_path, exe_name.lower())]
elif platform.system() == "Darwin":
    cmd = ["open", exe_name + ".app", "--args"]
elif platform.system() == "Windows":
    cmd = [exe_name + ".exe"]
else:
    cmd = [exe_name]

cmd.extend(sys.argv[1:])

try:
    subprocess.Popen(cmd)
except Exception as e:
    print("Error launching tool:")
    print(e)
    exit(1)
