#!/usr/bin/perl
use strict;

print "{ \"data\": [\r\n";
parseFile("header.102","ascm0200.102","300");
parseFile("header.200","ascp1600.200","20");
parseFile("header.202","ascp1900.202","50");
parseFile("header.403","ascp1600.403","100");
parseFile("header.405","ascp1600.405","20");
parseFile("header.406","ascm0100.406","100");
parseFile("header.410","ascp1960.410","20");
parseFile("header.413","ascp1900.413","25");
parseFile("header.414","ascp1600.414","100");
parseFile("header.418","ascp1900.418","152");
parseFile("header.421","ascp1900.421","150");
parseFile("header.422","ascm0100.422","100");
parseFile("header.423","ascp1800.423","50");
parseFile("header.424","ascm0100.424","100");
parseFile("header.430t","ascp01550.430t","100");
parseFile("header.430_229","ascp1550.430","100");
parseFile("header.431_229","ascm01000.431","1000");
parseFile("header.432t","ascp01550.432t","50");
parseFile("header_228.432","ascp01550.432","50");
print "]}\r\n";

sub parseFile(){
	my $filename=shift;
	my $firstFile=shift;
	my $yearsPerFile=shift;

	$filename=~m/\.([\dt]+)/gis;
	my $deversion=$1;
	printf("   [%7s,%20s,%5s,","\"".$deversion."\"","\"".$firstFile."\"",$yearsPerFile);

	open(my $f,$filename);

	my $l=<$f>;
	$l=~m/NCOEFF\=\s*(\d+)/gis;
	my $ncoeff=$1;

	printf("%6s,",$ncoeff);

	while($l!~/GROUP\s+1010/i){
		$l=<$f>;
	}
	$l=<$f>;
	$l=<$f>;
	$l=<$f>;
	$l=~m/JED\=\s*\-*[\d\.]+\s*(.*+)/gis;
	my $startDate=$1;
	$startDate=~s/\s*\r*\n*$//gis;
	$l=<$f>;
	$l=~m/JED\=\s*\-*[\d\.]+\s*(.*+)/gis;
	my $endDate=$1;
	$endDate=~s/\s*\r*\n*$//gis;

	while($l!~/GROUP\s+1030/i){
		$l=<$f>;
	}
	$l=<$f>;
	$l=<$f>;
	$l=~m/\s*([\d\.]+)\s+([\d\.]+)\s+(\d+)/gis;
	my $start=$1;
	my $end=$2;
	my $daysPerBlock=$3;

	printf("%25s,%25s,%15s,%15s,%4s,","\"".$startDate."\"","\"".$endDate."\"",$start,$end,$daysPerBlock);

	while($l!~/GROUP\s+1050/i){
		$l=<$f>;
	}
	$l=<$f>;

	my $numProperties=20;
	printf("%3s,",$numProperties);

	my @propertyNames=
		("","mercury","venus","emb","mars","jupiter","saturn","uranus","neptune","pluto","moon","sun","nutation","libration","mantle V","TT-TDB","future","future","future","future","future");
	printStrings(\@propertyNames,$numProperties);

	$l=<$f>;
	my @offsets=split(/\s+/,$l);
	#print "\r\n";
	printOffsets(\@offsets,$numProperties);

	$l=<$f>;
	my @coeff=split(/\s+/,$l);
	#print "\r\n";
	printArray(\@coeff,$numProperties);

	$l=<$f>;
	my @subintervals=split(/\s+/,$l);
	#print "\r\n";
	printArray(\@subintervals,$numProperties);

	my @properties=("","3","3","3","3","3","3","3","3","3","3","3","2","3","3","1");
	#print "\r\n";
	printArray(\@properties,$numProperties);

	print " \"end\"],\r\n";
}

sub printOffsets(){
	my @a=@{$_[0]};
	my $numProperties=$_[1];

	my $prevIndex="0";

	for(my $i=1;$i<=$numProperties;$i++){
		my $t=$prevIndex;
		if($i < scalar(@a)){
			$t=$a[$i];
		}
		printf("%5s,",$t);
		$prevIndex=$t;
	}
}

sub printArray(){
	my @a=@{$_[0]};
	my $numProperties=$_[1];

	for(my $i=1;$i<=$numProperties;$i++){
		my $t="0";
		if($i < scalar(@a)){
			$t=$a[$i];
		}
		printf("%5s,",$t);
	}
}

sub printStrings(){
	my @a=@{$_[0]};
	my $numProperties=$_[1];

	for(my $i=1;$i<=$numProperties;$i++){
		my $t="0";
		if($i < scalar(@a)){
			$t=$a[$i];
		}
		printf("%13s,","\"".$t."\"");
	}
}
