
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

function SetupVoteTool(id, page, average) {
	var voting = true;
	$('#serialStar' + id).change(function() {
		if(voting) {
			voting = false;
			var vote = $('#serialStar' + id).val();
			$.ajax({
				type: 'POST',
				url: '_setrating.ashx?vote=' + vote + '&page=' + encodeURIComponent(page)
			});
			$('#serialStar' + id).remove();
			$('.ui-rating').remove();
			$('#staticStar' + id).html(GenerateStaticStars(vote, 'ui-rating-hover'));
			$('#average' + id).html('Thanks!');
		}
	});
	$('#serialStar' + id).rating({ showCancel: false, startValue: average });
}
