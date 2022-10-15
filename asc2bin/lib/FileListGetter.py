import glob
import os
import re

class FileListGetter:
    def __init__(self,denum,path,useAll,startJD,endJD):
        self.files=[]
        self._loadFileList(denum,path,useAll,startJD,endJD)

    def _loadFileList(self,denum,path,useAll,startJD,endJD):
        if useAll:
            self.files=glob.glob(os.path.join(path,"asc*."+denum))
            self.files.sort()
        else:
            self._getFilesForJDRange(denum,path,startJD,endJD)

        firstJD=self._getStartJDForFile(self.files[0])
        lastJD=self._getEndJDForFile(self.files[len(self.files)-1])
        self.startJD=firstJD
        self.endJD=lastJD
        if(useAll==False):
            if(firstJD>startJD):
                self.startJD=firstJD
            if(lastJD<endJD):
                self.endJD=lastJD

    def _getFilesForJDRange(self,denum,path,startJD,endJD):
        self.files=[]
        allFiles=glob.glob(os.path.join(path,"asc*."+denum))
        allFiles.sort()

        for f in allFiles:
            s=self._getStartJDForFile(f)
            e=self._getEndJDForFile(f)
            if(startJD>=s and endJD<=e):
                self.files.append(f)

    def _getEndJDForFile(self,filename):
        f=open(filename,"rb")
        if os.path.getsize(filename) > 100000:
            f.seek(-100000,2)

        l=f.readline()
        l=f.readline()
        while len(l)>0:
            while len(l)>40 and l[40]!=32:
                l=f.readline()

            l=f.readline().decode("ascii").strip()
            if len(l)>0:
                t=re.split("\s+",l)
                lastJD=float(t[1].replace("D","E"))

        f.close()
        return lastJD

    def _getStartJDForFile(self,filename):
        f=open(filename,"r")
        l=f.readline()

        l=f.readline().strip()
        t=re.split("\s+",l)
        startJD=float(t[0].replace("D","E"))

        f.close()

        return startJD
