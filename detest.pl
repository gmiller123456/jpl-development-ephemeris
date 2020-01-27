#!/usr/bin/perl
use strict;
use lib ".";

use DE;


my @p=  (  "405",      "ascp1600.405",   20,  1018,   "1599 DEC 09 00:00:00",   "2201 FEB 20 00:00:00",     2305424.50,     2525008.50,  32, 20,    "mercury",      "venus",        "emb",       "mars",    "jupiter",     "saturn",     "uranus",    "neptune",      "pluto",       "moon",        "sun",   "nutation",  "libration",   "mantle V",     "TT-TDB",     "future",     "future",     "future",     "future",     "future",    3,  171,  231,  309,  342,  366,  387,  405,  423,  441,  753,  819,  899,  899,  899,  899,  899,  899,  899,  899,   14,   10,   13,   11,    8,    7,    6,    6,    6,   13,   11,   10,   10,    0,    0,    0,    0,    0,    0,    0,    4,    2,    2,    1,    1,    1,    1,    1,    1,    8,    2,    4,    4,    0,    0,    0,    0,    0,    0,    0,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    3,    2,    3,    3,    1,    0,    0,    0,    0,    0, "end");

my $de=DE->new(\@p);

my @t=$de->getAllPropertiesForSeries(0,2458864.5);
print join(",",@t);