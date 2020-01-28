
package JPLSeries;
use strict;
use POSIX;

	sub new{
		my $class=shift;
		#self,seriesName,seriesOffset,numberOfProperties,numberOfCoefficients,numberOfSubIntervals
		my $self={};
		$self->{name}=shift;
		$self->{offset}=(shift)-1;
		$self->{numberOfProperties}=shift;
		$self->{numberOfCoefficients}=shift;
		$self->{numberOfSubIntervals}=shift;
		bless $self,$class;
		return $self;

	}

	sub getAllPropertiesForSeries{
		my $self=shift;
		my $JD=shift;
		my @coefficients=@{shift()};
		my $blockOffset=shift;

		my $startJD=$coefficients[0+$blockOffset];
		my $endJD=$coefficients[1+$blockOffset];
		my $blockDuration=$endJD-$startJD;
		my $subintervalDuration=$blockDuration/$self->{numberOfSubIntervals};
		my $subintervalSize=$self->{numberOfCoefficients}*$self->{numberOfProperties};
		my $subintervalNumber=floor(($JD-$startJD)/$subintervalDuration);
		my $subintervalStart=$subintervalDuration*$subintervalNumber;
		my $subintervalEnd=$subintervalDuration*$subintervalNumber+$subintervalDuration;

		#Normalize time variable (x) to be in the range -1 to 1 over the given subinterval
		#If using two doubles for JD, this is where the two parts should be combined:
		#e.g. jd=(JD[0]-(startJD+subintervalStart))+JD[1]
		my $jd=$JD-($startJD+$subintervalStart);
		my $x=($jd/$subintervalDuration)*2-1;

		my @properties=(0,0,0,0,0,0);
		for (my $i=0;$i<$self->{numberOfProperties};$i++){
			my $offset=$blockOffset+$self->{offset}+$i*$self->{numberOfCoefficients}+$subintervalSize*$subintervalNumber;
			my @t=$self->computePropertyForSeries($x,\@coefficients,$offset);
			$properties[$i]=$t[0];

			my $velocity = $t[1];
			my $velocity=$velocity*(2.0*$self->{numberOfSubIntervals}/$blockDuration);
			$properties[$i+$self->{numberOfProperties}]=$velocity;
		}
		return @properties;
	}

	sub computePropertyForSeries{
		my $self=shift;
		my $x=shift;
		my @coefficients=@{shift()};
		my $offset=shift;
		my @c=();
		for (my $i=0;$i<$self->{numberOfCoefficients};$i++){
			$c[$i]=$coefficients[$offset+$i];
		}
		my @p=$self->computePolynomial($x,\@c);
		return @p;
	}

	sub computePolynomial{
		my $self=shift;
		my $x=shift;
		my @coefficients=@{shift()};

		#Equation 14.20 from Explanetory Supplement 3rd ed.
		my @t=(1.0,$x);

		for (my $n=2;$n<scalar(@coefficients);$n++){
			my $tn=2*$x*$t[$n-1]-$t[$n-2];
			$t[$n]=$tn;
		}

		#Multiply the polynomial by the coefficients.
		#Loop through coefficients backwards (from smallest to largest) to avoid floating point rounding errors
		my $position=0;
		for (my $i=scalar(@coefficients)-1;$i>-1;$i--){
			$position+=$coefficients[$i]*$t[$i];
		}

		#Compute velocity (just the derivitave of the above)
		my @v=(0,1,4*$x);
		for (my $n=3;$n<scalar(@coefficients);$n++){
			$v[$n]=2*$x*$v[$n-1]+2*$t[$n-1]-$v[$n-2];
		}

		my $velocity=0;
		for (my $i=scalar(@coefficients)-1;$i>-1;$i--){
			$velocity+=$v[$i]*$coefficients[$i];
		}
		return ($position,$velocity);
	}


1;