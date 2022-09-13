#!/usr/bin/python

from de import *

de=DE405()
print(len(de.coefficients))
print(de.getAllPropertiesForSeries(0,Decimal(2458850.5)))
