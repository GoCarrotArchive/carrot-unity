# Carrot -- Copyright (C) 2012 Carrot Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
import sys, getopt

# https://bitbucket.org/darktable/mod-pbxproj/
from mod_pbxproj import XcodeProject

def main(argv):
    inputfile = None
    outputfile = None
    try:
        opts, args = getopt.getopt(argv,"hi:o:",["input=","output="])
    except getopt.GetoptError:
        print 'CarrotXcodeFrameworks.py -i <inputfile> -o <outputfile>'
        sys.exit(2)
    for opt, arg in opts:
        if opt == '-h':
            print 'CarrotXcodeFrameworks.py <inputfile>'
            sys.exit()
        elif opt in ("-i", "--input"):
            inputfile = arg
        elif opt in ("-o", "--output"):
            outputfile = arg

    project = XcodeProject.Load(inputfile)

    frameworks = ['SystemConfiguration', 'Accounts', 'Social', 'AdSupport']
    for framework in frameworks:
        framework_path = 'System/Library/Frameworks/{0}.framework'.format(framework)
        if not project.get_files_by_os_path(framework_path, tree='SDKROOT'):
            print 'Added \'{0}\''.format(framework_path)
            project.add_file(framework_path, tree='SDKROOT')

    usr_libs = ['libsqlite3']
    for usr_lib in usr_libs:
        usr_lib_path = 'usr/lib/{0}.dylib'.format(usr_lib)
        if not project.get_files_by_os_path(usr_lib_path, tree='SDKROOT'):
            print 'Added \'{0}\''.format(usr_lib_path)
            project.add_file(usr_lib_path, tree='SDKROOT')

    if project.modified:
        if outputfile == None:
            project.backup()
        project.save(outputfile)

if __name__ == "__main__":
    main(sys.argv[1:])
