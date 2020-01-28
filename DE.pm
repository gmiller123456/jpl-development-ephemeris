#Example lib to compute DE405 Development Ephemeris from JPL
#Greg Miller (gmiller@gregmiller.net) 2019
#Released as public domain

#TODO:

package DE;
use strict;
use JPLSeries;
use POSIX;

sub new {

	my $class=shift;
	my @data=@{$_[0]};
	my $self={};
	#First three variables (after "name") are from "GROUP 1030" in header file
	#Last variable is NCOEFF from first line of header file

	$self->{name}=$data[0];
	$self->{start}=$data[6];
	$self->{end}=$data[7];
	$self->{daysPerBlock}=$data[8];
	$self->{coefficientsPerBlock}=$data[3] + 2;  #+2 because each block is padded with two zeros
	$self->{yearsPerFile}=$data[2];
	$self->{fileBaseName}=$data[1];
	$self->{fileBase}=substr($self->{fileBaseName},4,4);
	$self->{fileNamePad}=4;
	if(substr($self->{fileBaseName},9,1) eq "."){
		$self->{fileBase}=substr($self->{fileBaseName},4,5);
		$self->{fileNamePad}=5;
	}

	if(substr($self->{fileBaseName},3,1) eq "m"){
		$self->{fileBase}*=-1;
	}

	my $propertyCountIndex=9;
	my $propertyCount=$data[$propertyCountIndex];

	my @series=();
	for (my $i=0;$i<$propertyCount;$i++){
		$series[$i]=JPLSeries->new($data[$i+$propertyCountIndex+1+$propertyCount*0],
			$data[$i+$propertyCountIndex+1+$propertyCount*1],
			$data[$i+$propertyCountIndex+1+$propertyCount*4],
			$data[$i+$propertyCountIndex+1+$propertyCount*2],
			$data[$i+$propertyCountIndex+1+$propertyCount*3]);
	}

	$self->{series}=\@series;
	$self->{loadedFile}="";

	bless $self,$class;
	return $self;
}

sub getAllPropertiesForSeries{
	my $self=shift;
	my $series=shift;
	my $JD=shift;

	$self->loadFileForJD($JD);
	my $blockNumber=floor(($JD-$self->{chunkStart})/$self->{daysPerBlock});
	my $blockOffset=$blockNumber*($self->{coefficientsPerBlock});

	return $self->{series}[$series]->getAllPropertiesForSeries($JD,$self->{coefficients},$blockOffset);
}

#@staticmethod
sub getEarthPositionFromEMB{
	my @emb=@{$_[0]};
	my @moon=@{$_[1]};

	my $earthMoonRatio=0.813005600000000044E+02;
	my @earth=(0,0,0,0,0,0);
	for (my $i=0;$i<6;$i++){
		$earth[$i]=$emb[$i]-$moon[$i]/(1.0+$earthMoonRatio);
	}

	return @earth;
}

sub loadFile{
	my $self=shift;
	my $filename=shift;
	if($self->{loadedFile} eq $filename){
		return;
	}
	$self->{coefficients}=();

	open(my $f,"de".$self->{name}."/".$filename);
	if(!$f){die $!;}
	my $count=0;
	while(my $l=<$f>){
		if(length($l) >17 && substr($l,17,1) ne " "){
			my @t=split(" +",$l);
			for (my $i=1;$i<4;$i++){
				if($i>scalar(@t)-1){
					$self->{coefficients}[$count]=0.0;
					$count++;
				}else{
					$t[$i]=~s/D/e/gi;
					$self->{coefficients}[$count]=$t[$i];
					$count++;
				}
			}
		}
	}

	close($f);
	$self->{loadedFile}=$filename;
	$self->{chunkStart}=$self->{coefficients}[0];

	$self->{chunkEnd}=$self->{coefficients}[$count-$self->{coefficientsPerBlock}+1];
}

sub loadFileForJD{
	my $self=shift;
	my $jd=shift;

	my $year=(DE::julainDateToGregorian($jd))[0];
	my $pm="p";
	if($year<0){
		$year=abs($year);
		$pm="m";
	}

	$year=floor(($year-$self->{fileBase})/$self->{yearsPerFile})*$self->{yearsPerFile}+$self->{fileBase};

	my $fileName=sprintf("asc".$pm."%0".$self->{fileNamePad}."d.".$self->{name},$year);
	my $neededFile=$fileName;
	$self->loadFile($neededFile);
}

#@staticmethod
#sub INT(a){
sub INT{
	my $a=shift;
	return floor($a);
}

#From Meeus, CH7
#@staticmethod
sub julainDateToGregorian {
	my $jd=shift;
	my $temp=$jd+.5;
	my $Z=floor($temp);
	my $F=$temp-$Z;
	my $A=$Z;
	if($Z>=2299161){
		my $alpha=INT(($Z-1867216.25)/36524.25);
		$A=$Z+1+$alpha-DE::INT($alpha/4);
	}
	
	my $B=$A+1524;
	my $C=DE::INT(($B-122.1)/365.25);
	my $D=DE::INT(365.25*$C);
	my $E=DE::INT(($B-$D)/30.6001);

	my $day=$B-$D-DE::INT(30.6001*$E)+$F;
	my $month=$E-1;
	if($E>13){
		$month=$E-13;
	}
	
	my $year=$C-4716;
	if($month<3){
		$year=$C-4715;
	}

	return ($year,$month,$day);
}



1;