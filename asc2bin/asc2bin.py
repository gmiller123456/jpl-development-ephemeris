#!/user/bin/python
from lib import BinWriter
from lib import FileListGetter
from lib import CommandLineParser
import sys

cl=CommandLineParser.CommandLineParser(sys.argv)

lg=FileListGetter.FileListGetter(cl.denum,cl.path,cl.processAllFiles,cl.startJD,cl.endJD)

h=BinWriter.FileWriter(cl.denum,cl.headerFile,lg.files,cl.outputFile,lg.startJD,lg.endJD)