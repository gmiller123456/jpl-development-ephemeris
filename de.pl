#Example lib to compute DE405 Development Ephemeris from JPL
#Greg Miller (gmiller@gregmiller.net) 2019
#Released as public domain

#TODO:

package DE;
	sub new(self,data){
		#First three variables (after "name") are from "GROUP 1030" in header file
		#Last variable is NCOEFF from first line of header file
		$self->{name}=$data[0];
		$self->{start}=float($data[6]);
		$self->{end}=float($data[7]);
		$self->{daysPerBlock}=int($data[8]);
		$self->{coefficientsPerBlock}=int($data[3]) + 2;  #+2 because each block is padded with two zeros
		$self->{yearsPerFile}=int($data[2]);
		$self->{fileBaseName}=$data[1];
		$self->{fileBase}=int(substr($self->{fileBaseName},4,8));
		$self->{fileNamePad}=4;
		if(substr($self->{fileBaseName},9,10) eq "."){
			$self->{fileBase}=int(substr($self->{fileBaseName},4,9));
			$self->{fileNamePad}=5;
		}

		if(substr($self->{fileBaseName},3,4) eq "m"){
			$self->{fileBase}*=-1;
		}

		$propertyCountIndex=9;
		$propertyCount=$data[$propertyCountIndex];

		$series=[];
		for (my $i=0;$i<propertyCount;$i++){
			$series.append(JPLSeries($data[$i+$propertyCountIndex+1+$propertyCount*0],
				$data[$i+$propertyCountIndex+1+$propertyCount*1],
				$data[$i+$propertyCountIndex+1+$propertyCount*4],
				$data[$i+$propertyCountIndex+1+$propertyCount*2],
				$data[$i+$propertyCountIndex+1+$propertyCount*3]));
		}

		$self->{series}=series;
		$self->{loadedFile}="";
	}

	sub loadFile(self,filename){
		if($self->{loadedFile} eq $filename){
			return;
		}
		
		self->{coefficients}=[];
		open($f,"de"+$self->{name}+"/"+$filename,"r");
		while($l=<$f>){
			if(len($l) >17 && substr($l,17,18) ne " "){
				@t=split(" +",$l);
				for ($i=1;$i<4;$i++){
					if(i>scalar($t)-1){
						$self->{coefficients}.append(0.0);
					}else{
						$self->{coefficients}.append(Decimal($t[$i].replace("D","e")));
					}
				}
			}
		}

		close($f);
		$self->{loadedFile}=$filename;
		$self->{chunkStart}=self->{coefficients}[0];

		$self->{chunkEnd}=$self->{coefficients}[len($self->{coefficients})-$self->{coefficientsPerBlock}+1];
	}

	sub loadFileForJD(self,jd){
		$year=(DE::julainDateToGregorian($jd))[0];

		$pm="p";
		if($year<0){
			$year=abs($year);
			$pm="m";
		}

		$year=floor(($year-$self->{fileBase})/$self->{yearsPerFile})*$self->{yearsPerFile}+$self->{fileBase};

		$fileName="asc".$pm.$self->{fileNamePad}.$self->{name};
		$neededFile=$fileName;
		$self->loadFile($neededFile);
	}

	sub getAllPropertiesForSeries(self,series,JD){
		$self->loadFileForJD($JD);

		$blockNumber=floor(($JD-$self->{chunkStart})/$self->{daysPerBlock});
		$blockOffset=$blockNumber*($self->{coefficientsPerBlock});
		return $self->{series}[$series]->getAllPropertiesForSeries($JD,$self->{coefficients},$blockOffset);
	}

	#@staticmethod
	sub getEarthPositionFromEMB(emb,moon){
		$earthMoonRatio=0.813005600000000044E+02;
		$earth=[0,0,0,0,0,0];
		for ($i=0;$i<6;$i++){
			$earth[i]=$emb[i]-$moon[i]/(1.0+$earthMoonRatio);
		}
		return $earth;
	}
	
	#@staticmethod
	#sub INT(a){
	sub INT{
		my $a=shift;
		return floor($a);
	}

	#From Meeus, CH7
	#@staticmethod
	sub julainDateToGregorian(jd){
		$temp=jd+.5;
		$Z=trunc($temp);
		$F=$temp-$Z;
		$A=$Z;
		if($Z>=2299161){
			$alpha=INT(($Z-1867216.25)/36524.25);
			$A=$Z+1+$alpha-DE.INT($alpha/4);
		}
		
		$B=$A+1524;
		$C=DE::INT(($B-122.1)/365.25);
		$D=DE::INT(365.25*$C);
		$E=DE::INT(($B-$D)/30.6001);

		$day=$B-$D-DE::INT(30.6001*$E)+$F;
		$month=$E-1;
		if($E>13){
			$month=$E-13;
		}
		
		$year=$C-4716;
		if($month<3){
			$year=$C-4715;
		}

		return [$year,$month,$day];
	}


package JPLSeries;
	sub new{
		#self,seriesName,seriesOffset,numberOfProperties,numberOfCoefficients,numberOfSubIntervals
		$self->{name}=$seriesName;
		$self->{offset}=$seriesOffset-1;
		$self->{numberOfProperties}=$numberOfProperties;
		$self->{numberOfCoefficients}=$numberOfCoefficients;
		$self->{numberOfSubIntervals}=$numberOfSubIntervals;
	}

	sub getAllPropertiesForSeries(self,JD,coefficients,blockOffset){
		$startJD=$coefficients[0+$blockOffset];
		$endJD=$coefficients[1+$blockOffset];
		$blockDuration=endJD-$startJD;
		$subintervalDuration=$blockDuration/$self->{numberOfSubIntervals};
		$ubintervalSize=$self->{numberOfCoefficients}*$self->{numberOfProperties};
		$subintervalNumber=floor(($JD-$startJD)/$subintervalDuration);
		$subintervalStart=$subintervalDuration*$subintervalNumber;
		$subintervalEnd=$subintervalDuration*$subintervalNumber+$subintervalDuration;

		#Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
		#If using two doubles for JD, this is where the two parts should be combined:
		#e.g. jd=(JD[0]-(startJD+subintervalStart))+JD[1]
		$jd=JD-($startJD+$subintervalStart);
		$x=($jd/$subintervalDuration)*2-1;

		$properties=[0,0,0,0,0,0];
		for (my $i=0;$i<self->{numberOfProperties};$i++){
			$offset=$blockOffset+$self->{offset}+$i*$self->{numberOfCoefficients}+$subintervalSize*$subintervalNumber;
			$t=self->computePropertyForSeries($x,$coefficients,$offset);
			$properties[i]=$t[0];

			$velocity = $t[1];
			$velocity=$velocity*(2.0*$self->{numberOfSubIntervals}/$blockDuration);
			$properties[$i+$self->{numberOfProperties}]=$velocity;
		}
		return $properties;
	}

	sub computePropertyForSeries(self,x,coefficients,offset){
		$c=[];
		for (my $i=0;$i<$self->{numberOfCoefficients};$i++){
			c.append($coefficients[$offset+$i]);
		}
		$p=$self->computePolynomial($x,$c);
		return $p;
	}

	sub computePolynomial(self,x,coefficients){
		#Equation 14.20 from Explanetory Supplement 3rd ed.
		$t=[1,$x];

		for (my $n=0;$n<2,scalar(@coefficients);$n++){
			$tn=2*$x*$t[$n-1]-$t[$n-2];
			$t.append($tn);
		}

		#Multiply the polynomial by the coefficients.
		#Loop through coefficients backwards (from smallest to largest) to avoid floating point rounding errors
		$position=0;
		for (my $i=scalar(@coefficients)-1;$i>-1;$i--){
			$position+=$coefficients[$i]*$t[$i];
		}

		#Compute velocity (just the derivitave of the above)
		$v=[0,1,4*$x];
		for (my $n=3;$n<scalar(@coefficients);$n++){
			$v.append(2*$x*$v[$n-1]+2*$t[$n-1]-$v[$n-2]);
		}

		$velocity=0;
		for (my $i=scalar(coefficients)-1;$i>-1;$i--){
			$velocity+=$v[$i]*$coefficients[$i];
		}

		return [$position,$velocity];
	}



1;