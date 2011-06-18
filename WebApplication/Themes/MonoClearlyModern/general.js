jQuery.noConflict();
jQuery(function($) { 

// DROP DOWN MENU
	// http://users.tpg.com.au/j_birch/plugins/superfish/#getting-started
	// http://users.tpg.com.au/j_birch/plugins/superfish/#options
	$(".sf-menu").supersubs({ 
			minWidth:    10,   // minimum width of sub-menus in em units 
			maxWidth:    27,   // maximum width of sub-menus in em units 
			extraWidth:  1     // extra width can ensure lines don't sometimes turn over 
							   // due to slight rounding differences and font-family 
		}).superfish({
			dropShadows:    false,
			delay:			400
			
							}); // call supersubs first, then superfish, so that subs are 
                         		// not display:none when measuring. Call before initialising 
                         		// containing tabs for same reason. 

	
	// Scroll to top animation
	$('.scroll-top').click(function(){ 
		$('html, body').animate({scrollTop:0}, 600); return false; 
	});
	
	
	// Hide parent on click (error messages, etc...)
	$('a.hideparent').click(function(){ 
		$(this).parent().fadeOut();
		return false;
	});

	// Lightbox setup
	// Ex: open any link <a href="large.jpg" />...
	$('a[href$="jpg"], a[href$="jpeg"], a[href$="png"], a[href$="gif"]').fancybox();

	// Vimeo Popup - Large
	$(".vimeo-popup-large").click(function() {
		$.fancybox({
			'padding'		: 0,
			'autoScale'		: false,
			'transitionIn'	: 'none',
			'transitionOut'	: 'none',
			'title'			: this.title,
			'width'			: 600,
			'height'		: 340,
			'href'			: this.href.replace(new RegExp("([0-9])","i"),'moogaloop.swf?clip_id=$1'),
			'type'			: 'swf'
		});
		return false;
	});
	
	// Vimeo Popup - Regula Size
	$(".vimeo-popup").click(function() {
		$.fancybox({
			'padding'		: 0,
			'autoScale'		: false,
			'transitionIn'	: 'none',
			'transitionOut'	: 'none',
			'title'			: this.title,
			'width'			: 400,
			'height'		: 225,
			'href'			: this.href.replace(new RegExp("([0-9])","i"),'moogaloop.swf?clip_id=$1'),
			'type'			: 'swf'
		});
		return false;
	});

	// Default Modal box
	$(".modal-box").fancybox({
		'modal' : true
	});

	
	
	// contact form validation
		var hasChecked = false;
		$(".standard #submit").click(function () { 
			hasChecked = true;
			return checkForm();
		});
		$(".standard #name,.standard #email,.standard #message").live('change click', function(){
			if(hasChecked == true)
			{
				return checkForm();
			}
		});
		function checkForm()
		{
			var hasError = false;
			var emailReg = /^([\w-\.]+@([\w-]+\.)+[\w-]{2,4})?$/;

			if($(".standard #name").val() == '') {
				$(".standard #error-name").fadeIn();
				hasError = true;
			}else{
				$(".standard #error-name").fadeOut();
			}
			if($(".standard #email").val() == '') {
				$(".standard #error-email").fadeIn();
				hasError = true;
			}else if(!emailReg.test( $(".standard #email").val() )) {
				$(".standard #error-email").fadeIn();
				hasError = true;
			}else{
				$(".standard #error-email").fadeOut();
			}
			if($(".standard #message").val() == '') {
				$(".standard #error-message").fadeIn();
				hasError = true;
			}else{
				$(".standard #error-message").fadeOut();
			}
			if(hasError == true)
			{
				return false;
			}else{
				return true;
			}
		}
		// end contact form validation
	
	
	
		// Latest Tweets
		$("#latest-footer-tweets").tweet({
			join_text: "auto",
			username: "cudazi",
			count: 4,
			auto_join_text_default: "we said,", 
			auto_join_text_ed: "we",
			auto_join_text_ing: "we were",
			auto_join_text_reply: "we replied",
			auto_join_text_url: "we were checking out",
			loading_text: "Loading tweets..."
		  });
		
		
		// Toggle Content!
		$(".hidden").hide();
		$("a.toggle").click(function(event){
			if( $(this).text() == 'Show More' ) {
				$(this).text("Show Less");
			}else{
				$(this).text("Show More");
			}
			$(this).parents(".toggle-container").find(".hidden").slideToggle("normal");
			return false;
		});
		
		
		// This script is for demonstration purposes only, you can remove once you are done experimenting with the css/theme switcher
		$('a.switch-theme').click(function(event){
			$('#theme-colors').attr('href', "css/themes/" + $(this).attr('title') + ".css ");
			$('a.switch-theme').css('font-weight','normal');
			$(this).css('font-weight','bold');
			return false;
		});
 		
		
		
}); // end jQuery