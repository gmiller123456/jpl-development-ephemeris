#!/usr/bin/perl
use strict;

print "let de405=[\r\n";
while(my $l=<>){
	if(substr($l,2,1) eq " "){
		print "//$l";
	} else {
		$l=~s/D/E/g;
		substr($l,26,1)=",";
		substr($l,52,1)=",";
		substr($l,78,1)=",";
		print "$l";
	}
}
print "];\r\n";