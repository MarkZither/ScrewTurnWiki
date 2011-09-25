
function GenerateStaticStars(rate, cssClass) {
	var string = '';
	for(var i = 0; i < rate; i++) {
		string += '<span class="static-rating ' + cssClass + '"></span>';
	}
	for(var i = rate; i < 5; i++) {
		string += '<span class="static-rating ui-rating-empty"></span>';
	}
	return string;
}
