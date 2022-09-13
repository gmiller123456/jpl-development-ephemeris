#!/usr/bin/perl

#Compute the X positon of Mercury using DE405 file.  Greg Miller (gmiller@gregmiller.net) 2019.
#Released as public domain

use strict;

open(my $f,"ascp2020.405") || die "Can't open ascp2020.405 $!";
my @coeff=();

my $l=<$f>;
$l=<$f>;
while(substr($l,2,1) ne " "){

	$l=~s/D/E/gis;
	push(@coeff,substr($l,0,26)-0);
	push(@coeff,substr($l,26,26)-0);
	push(@coeff,substr($l,52,26)-0);

	$l=<$f>;
}

close($f);

my $jd=2458832.5;

my $subintervals=4;
my $start=$coeff[0];
my $end=$coeff[1];
my $length=$end-$start;
my $subintervalLength=$length/$subintervals;
my $thisSubinterval=1;
my $numCoeff=14;

print "Start: $start\tEnd: $end\tLen: $length\r\n";


my $x = 2*($jd - (($thisSubinterval - 1)*$subintervalLength + $start))/$subintervalLength - 1;
print "time:$x sub: $thisSubinterval dur: $subintervalLength start:$start\r\n";


#Equation 14.20 from Explanetory Supplement
my @t=();
push(@t,1);
push(@t,$x);

for(my $n=2;$n<$numCoeff; $n++){
	my $tn=2*$x*$t[$n-1]-$t[$n-2];
	push(@t,$tn);
}

print join(",",@t)."\r\n";

#Equation 14.19 from Explanetory Supplement
my $pos=0;
for(my $i=0;$i<$numCoeff;$i++){
	$pos+=$coeff[$i+2]*$t[$i];
print "$pos\r\n";
}

