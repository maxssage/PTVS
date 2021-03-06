 # ############################################################################
 #
 # Copyright (c) Microsoft Corporation. 
 #
 # This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 # copy of the license can be found in the License.html file at the root of this distribution. If 
 # you cannot locate the Apache License, Version 2.0, please send an email to 
 # vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 # by the terms of the Apache License, Version 2.0.
 #
 # You must not remove this notice, or any other, from this software.
 #
 # ###########################################################################

# Compares two different completion databases, dumping the results.  
# Used for generating fixers for later versions of Python.
# Usage:
#  First run:
#       python.exe PythonScraper.py OutDir1
#       pythonv2.exe PythonScraper.py OutDir2
#       python.exe VersionDiff.py OutDir1 OutDir2
#
# This will then dump a list of removed and new members which can be added to PythonScraper.py.
# A person needs to manually merge these in and make sure the version fields are all correct.
# In general this should be low overhead once every couple of years when a new version of Python
# ships.  
#
# In theory this could be completely automated but we'd need to automatically run against all Python
# versions to figure out the minimum version.

import os, pprint, sys
try:
    import cPickle
except:
    import _pickle as cPickle

dir1 = sys.argv[1]
dir2 = sys.argv[2]

files3 = set(os.listdir(dir1))
files2 = set(os.listdir(dir2))

for filename in files3:
    if filename.endswith('.idb') and filename in files2:
        b2 = cPickle.load(file(dir2 + '\\' + filename, 'rb'))
        b3 = cPickle.load(file(dir1 + '\\' + filename, 'rb'))
        
        removed_three = set(b2['members']) - set(b3['members'])
        added_three = set(b3['members']) - set(b2['members'])
        
        if removed_three or added_three:
            print(filename[:-4])
            if removed_three:
                print('Removed in ' + dir1, removed_three)
            #if added_three:
            #    print('New in ' + dir1, added_three)

            #for added in added_three:
            #    sys.stdout.write("mod['members']['%s'] = " % added)
            #    b3['members'][added]['value']['version'] = '>=3.2'
            #    pprint.pprint(b3['members'][added])
            #

