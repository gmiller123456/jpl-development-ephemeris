The original fortran programs for reading ascii versions of the
JPL planetary ephemeris files had a fixed maximum number of 400
for the number of ephemeris constants used in the integration
and listed in the header.xxx file for the ephemeris.

DE431 uses 572 ephemeris constants, which exceeds the prior
maximum number allowed.

In this directory, there are two header files included.

Use header.431_229 if using an old program with 400 maximum
ephemeris constants. The header.431_229 file includes a list
of only 229 ephemeris constants, omitting the mass parameters
for 343 individual asteroids modeled in the de431 integration.
Using this header will only omit the asteroid mass parameters
from the binary ephemeris file; position and velocity information
will be unaffected.

Use header.431_572 if using a program that allows more than 400
ephemeris constants.

***********************************************************

For converting the ascii files to a binary format,
the supplied program asc2eph in the directory /pub/planets/fortran 
was formerly configured to ignore Julian dates less than zero.
To access dates before that with asc2eph please make sure to
use a version updated 15 August 2013 or later.

***********************************************************

Also, this set of files does not include information on TT-TDB
which was integrated as part of the de431 development.
The TT-TDB information will be available in ascii format in the
directory de431t.

***********************************************************

A binary file covering the full ascii time span,
-13000 to +17000, in little-endian format, suitable for Intel
processors is available in the directory Linux directory.

***********************************************************

The original ephemeris was integrated overs a slightly longer time span,
from year -13200 to +17191, to ensure that data existed before and after 
each 1000 year ascii data block. Those few extra years are available
in the SPICE binary SPK file, de431.bsp.
