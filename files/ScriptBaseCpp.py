#!/usr/bin/env python

from __future__ import print_function
import sys
import os
import subprocess
import time
import platform

fullScript = os.path.abspath(sys.argv[0])

scriptName = os.path.basename(sys.argv[0])
scriptName = os.path.splitext(scriptName)[0]

exeName = scriptName


toolsFolder = os.path.dirname(fullScript)

fullExeName = os.path.join(toolsFolder, exeName)

if platform.system() == 'Darwin':
  fullExeName = fullExeName + '.app'
  runCommand = 'open'
else:
  runCommand = 'exec'

try:
  subProc = subprocess.Popen([runCommand, fullExeName], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
  # If here, start succeeded
except Exception as e:
  # error
  print('Error Launching Tool: ')
  print(e)
  exit(1)

exit(0)
