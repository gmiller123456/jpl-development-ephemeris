import os

class CommandLineParser:

    def __init__(self,arr):
        self.denum=None
        self.outputFile=None
        self.headerFile=None
        self.path=""
        self.startJD=""
        self.endJD=""
        self.processAllFiles=True

        self.parse(arr)
        if(self.denum==None):
            print("Error: denum not provided.")
            self.useage()
            exit()

        if(self.outputFile==None):
            self.outputFile="jpleph."+self.denum
        if(self.headerFile==None):
            self.headerFile=os.path.join(self.path,"header."+self.denum)

    def parse(self,arr):
        self.index=1
        while self.index<len(arr):
            self.parseArg(arr)

    def parseArg(self,arr):

        if(self.index>=len(arr)):
            self.usage()
            exit()

        if(arr[self.index][0]=="-"):
            option=arr[self.index][1]
            if(option=="p"):
                self.path=self.getopt(arr)
                self.index+=1
            elif(option=="h"):
                self.headerFile=self.getopt(arr)
                self.index+=1
            elif(option=="o"):
                self.outputFile=self.getopt(arr)
                self.index+=1
            elif(option=="r"):
                self.startJD=float(self.getopt(arr))
                self.endJD=float(self.getopt(arr))
                self.processAllFiles=False
                self.index+=1
            else:
                print("Illegal option: "+option)
                self.useage()
                exit()
        else:
            self.denum=arr[self.index]
            self.index+=1
        
    def getopt(self,arr):
        self.index+=1
        if(self.index>=len(arr)):
            self.useage()
            exit()

        return arr[self.index]

    def useage(self):
        print("asc2bin.py -- Converts ASCII NASA JPL DE files to binary format.")
        print()
        print("Useage:")
        print("asc2bin.py [denum]")
        print("   [denum] - required - the version of the DE to look for (e.g. 405, 431t, etc)")
        print("   -p ASCII file path [Default is current directory]")
        print("   -r Start JD End JD [Default is all matching files]")
        print("   -h ASCII header file [Default 'header.[denum]' e.g. 'header.405']")
        print("   -o Output binary file [Default 'jpleph.[denum]]")
        print()
        print("Examples:")
        print("   asc2bin.py 102")
        print("   asc2bin.py 431t -p \"c:\\asciifiles\\\" -o \"c:\\binfiles\\jpleph.431t\"")
        print("   asc2bin.py 405 -r 2451536.5 2458864.5")
        print()
        print("ASCII file names are assumed to match asc*.[denum] and are in the proper")
        print("order when sorted alphabetically (e.g. the original names from JPL).")
