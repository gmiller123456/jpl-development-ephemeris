#!/usr/bin/perl
use strict;

for(my $i=0;$i<1000;$i++){
	generateRandomSample();
}

sub generateRandomSample(){
	my $planet=randomPlanet();
	my @dates=randomDate($planet);

	getData($dates[0],$dates[1],$planet);
}

sub randomDate(){
	my $planet=shift();
	#Horizons CGI interface only has data for these years
	#Others use 1000 - 3000
	#Mars (499) 1600 - 2500
	#Jupiter (599) 1600 - 2599
	#Saturn (699) 1849 - 2150
	#Uranus (799) 1599 - 2600
	#Neptune (899) 1950 - 2049
	my $start=2020;
	my $end=2039;
	my $year=int(($end-$start)*rand())+$start;
	my $month=int(12*rand())+1;
	my $day=int(27*rand())+1;
	my $hour=int(24*rand());
	my $min=int(60*rand());
	my $sec=int(59*rand());

	my $start=sprintf("%04d-%02d-%02d %02d:%02d:%02d",$year,$month,$day,$hour,$min,$sec);
	my $end=sprintf("%04d-%02d-%02d %02d:%02d:%02d",$year,$month,$day,$hour,$min,$sec+1);

	my @return;
	push(@return,$start);
	push(@return,$end);
	return @return;
}

sub randomPlanet(){
	my $p=int(9*rand());

	$p=$p*100+99;
	if($p==999){$p=301;} #Moon

	return $p;
}

sub getData(){
	my $startTime=shift();
	my $stopTime=shift();
	my $stepSize="1d";
	my $command=shift();

	my $url="https://ssd.jpl.nasa.gov/horizons_batch.cgi?batch=1&command=$command&TABLE_TYPE=%27VECTORS%27&REF_PLANE=%27FRAME%27&START_TIME=%27$startTime%27&STOP_TIME=%27$stopTime%27&STEP_SIZE=%271%20hours%27&OUT_UNITS=%27KM-D%27&CENTER=500\@0";
	#my $url="https://ssd.jpl.nasa.gov/horizons_batch.cgi?batch=1&TABLE_TYPE=VECTOR&QUANTITIES='A'&COMMAND=\"$command\"&CSV_FORMAT=YES&CAL_FORMAT=BOTH&ANG_FORMAT=DEG&APPARENT=AIRLESS&REF_SYSTEM=J2000&EXTRA_PREC=yes&CENTER='coord'&SITE_COORD=\"$siteCoord\"&START_TIME=\"$startTime\"&STOP_TIME=\"$stopTime\"&STEP_SIZE=\"$stepSize\"&SKIP_DAYLT=NO";
print "$url\r\n";
return;
	my $text=`wget -q -O- '$url'`;

	my $dataStart=index($text,"\$\$SOE")+5;
	my $dataEnd=index($text,"\$\$EOE");
	my $data=substr($text,$dataStart,$dataEnd-$dataStart);

	$data=~s/^\s*//gis;
	$data=~s/\s*$//gis;

	my @f=split(/,/,$data);

	#print "Date:        $f[0]\r\n";
	#print "Jul Day:     $f[1]\r\n";
	#print "RA J2000:    $f[4]\r\n";
	#print "DEC J2000:   $f[5]\r\n";
	#print "RA of Date:  $f[6]\r\n";
	#print "DEC of Date: $f[7]\r\n";
	#print "Azi:         $f[10]\r\n";
	#print "Alt:         $f[11]\r\n";
	#print "SID Time:    $f[17]\r\n";
	#print "1 way LT:    $f[41]\r\n";
	#print "TDB-UT:      $f[54]\r\n";
	#print "Hour Ang:    $f[76]\r\n";

	#planet, lat, lon, date, jul day, RA J2000, DEC J2000, RA of Date, Dec of date, Azi, Alt, Sidereal Time, 1 way Light Time, TDB-UT, Hour angle

	#print "$command,$lat,$lon,\"$f[0]\",$f[1],$f[4],$f[5],$f[6],$f[7],$f[10],$f[11],$f[17],$f[41],$f[54],$f[76]\r\n";
}