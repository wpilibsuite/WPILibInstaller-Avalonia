#!/usr/bin/env python3

from __future__ import print_function
import os
import platform
import subprocess
import sys

script_name = os.path.abspath(sys.argv[0])
tools_folder = os.path.dirname(script_name)
year_folder = os.path.dirname(tools_folder)

if platform.system() == "Linux":
    cmd = [year_folder + "/advantagescope/AdvantageScope (WPILib).AppImage"]
elif platform.system() == "Darwin":
    cmd = ["open", year_folder + "/advantagescope/AdvantageScope (WPILib).app"]
elif platform.system() == "Windows":
    cmd = [year_folder + "\\advantagescope\\AdvantageScope (WPILib).exe"]

env = os.environ.copy()
del env["ELECTRON_RUN_AS_NODE"]

try:
    subprocess.Popen(cmd, env=env)
except Exception as e:
    print("Error launching tool:")
    print(e)
    exit(1)
