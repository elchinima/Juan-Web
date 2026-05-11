(function ($) {
	"use strict";

	// Sticky menu 
	var $window = $(window);
	$window.on('scroll', function () {
		var scroll = $window.scrollTop();
		if (scroll < 300) {
			$(".sticky").removeClass("is-sticky");
		} else {
			$(".sticky").addClass("is-sticky");
		}
	});

	// Background Image JS start
	var bgSelector = $(".bg-img");
	bgSelector.each(function (index, elem) {
		var element = $(elem),
			bgSource = element.data('bg');
		element.css('background-image', 'url(' + bgSource + ')');
	});

	// offcanvas search form active start
	$(".offcanvas-btn").on('click', function(){
		$("body").addClass('fix');
		$(".offcanvas-search-inner").addClass('show')
	})

	$(".minicart-btn").on('click', function(){
		$("body").addClass('fix');
		$(".minicart-inner").addClass('show')
	})

	$(".offcanvas-close, .minicart-close,.offcanvas-overlay").on('click', function(){
		$("body").removeClass('fix');
		$(".offcanvas-search-inner, .minicart-inner").removeClass('show')
	})

	// nice select active start
	$('select').niceSelect();

	// Off Canvas Open close start
	$(".off-canvas-btn").on('click', function () {
		$("body").addClass('fix');
		$(".off-canvas-wrapper").addClass('open');
	});

	$(".btn-close-off-canvas,.off-canvas-overlay").on('click', function () {
		$("body").removeClass('fix');
		$(".off-canvas-wrapper").removeClass('open');
	});
	

	// slide effect dropdown
	function dropdownAnimation() {
		$('.dropdown').on('show.bs.dropdown', function (e) {
			$(this).find('.dropdown-menu').first().stop(true, true).slideDown(500);
		});

		$('.dropdown').on('hide.bs.dropdown', function (e) {
			$(this).find('.dropdown-menu').first().stop(true, true).slideUp(500);
		});
	}
	dropdownAnimation();

	//offcanvas mobile menu start 
    var $offCanvasNav = $('.mobile-menu'),
        $offCanvasNavSubMenu = $offCanvasNav.find('.dropdown');
    
    /*Add Toggle Button With Off Canvas Sub Menu*/
    $offCanvasNavSubMenu.parent().prepend('<span class="menu-expand"><i></i></span>');
    
    /*Close Off Canvas Sub Menu*/
    $offCanvasNavSubMenu.slideUp();
    
    /*Category Sub Menu Toggle*/
    $offCanvasNav.on('click', 'li a, li .menu-expand', function(e) {
        var $this = $(this);
        if ( ($this.parent().attr('class').match(/\b(menu-item-has-children|has-children|has-sub-menu)\b/)) && ($this.attr('href') === '#' || $this.hasClass('menu-expand')) ) {
            e.preventDefault();
            if ($this.siblings('ul:visible').length){
                $this.parent('li').removeClass('active');
                $this.siblings('ul').slideUp();
            } else {
                $this.parent('li').addClass('active');
                $this.closest('li').siblings('li').removeClass('active').find('li').removeClass('active');
                $this.closest('li').siblings('li').find('ul:visible').slideUp();
                $this.siblings('ul').slideDown();
            }
        }
    });

	// tooltip active js
	$('[data-toggle="tooltip"]').tooltip();


	// Hero main slider active
	$('.hero-slider-active').slick({
		fade: true,
		autoplay: true,
		speed: 1000,
		prevArrow: '<button type="button" class="slick-prev"><i class="fa fa-angle-left"></i></button>',
		nextArrow: '<button type="button" class="slick-next"><i class="fa fa-angle-right"></i></button>',
		responsive: [{
			breakpoint: 992,
			settings: {
				arrows: false,
				dots: true
			}
		},
		{
			breakpoint: 480,
			settings: {
				arrows: false,
				dots: false
			}
		}]
	});


	// product carousel active
	$('.product-carousel-4').slick({
		slidesToShow: 4,
		prevArrow: '<button type="button" class="slick-prev"><i class="fa fa-angle-left"></i></button>',
		nextArrow: '<button type="button" class="slick-next"><i class="fa fa-angle-right"></i></button>',
		responsive: [
			{
				breakpoint: 1200,
				settings: {
					slidesToShow: 3
				}
			},
			{
				breakpoint: 992,
				settings: {
					slidesToShow: 2
				}
			},
			{
				breakpoint: 576,
				settings: {
					slidesToShow: 1
				}
			}
		]
	});


	// blog carousel active-2 js
	$('.top-seller-carousel').slick({
		rows: 2,
		arrows: false,
		slidesToShow: 2,
		responsive: [
			{
				breakpoint: 1200,
				settings: {
					slidesToShow: 1
				}
			},
			{
				breakpoint: 992,
				settings: {
					rows: 1,
					slidesToShow: 1
				}
			}
		]
	});


	// blog carousel active-2 js
	$('.blog-carousel-active').slick({
		arrows: false,
		slidesToShow: 3,
		responsive: [
			{
				breakpoint: 992,
				settings: {
					slidesToShow: 2
				}
			},
			{
				breakpoint: 768,
				settings: {
					slidesToShow: 1
				}
			}
		]
	});

	// brand slider active js
	$('.brand-active-carousel').slick({
		arrows: false,
		slidesToShow: 4,
		responsive: [
			{
				breakpoint: 992,
				settings: {
					slidesToShow: 2
				}
			},
			{
				breakpoint: 480,
				settings: {
					slidesToShow: 1
				}
			}
		]
	});



	// prodct details slider active
	$('.product-large-slider').slick({
		fade: true,
		arrows: false,
		asNavFor: '.pro-nav'
	});


	// product details slider nav active
	$('.pro-nav').slick({
		slidesToShow: 4,
		asNavFor: '.product-large-slider',
		arrows: false,
		focusOnSelect: true
	});

	// testimonial carousel active js
	$('.testimonial-active').slick({
		dots: true,
		arrows: false,
		responsive: [
			{
				breakpoint: 992,
				settings: {
					dots: false
				}
			}
		]
	});


	// image zoom effect (exclude quick view modal)
	$('.img-zoom').not('#quick_view .img-zoom').zoom();

	// pricing filter
	var rangeSlider = $(".price-range"),
		amount = $("#amount"),
		minPrice = rangeSlider.data('min'),
		maxPrice = rangeSlider.data('max');
	rangeSlider.slider({
		range: true,
		min: minPrice,
		max: maxPrice,
		values: [minPrice, maxPrice],
		slide: function (event, ui) {
			amount.val("$" + ui.values[0] + " - $" + ui.values[1]);
		}
	});
	amount.val(" $" + rangeSlider.slider("values", 0) +
		" - $" + rangeSlider.slider("values", 1));


	// product view mode change js
	$('.product-view-mode a').on('click', function (e) {
		e.preventDefault();
		var shopProductWrap = $('.shop-product-wrap');
		var viewMode = $(this).data('target');
		$('.product-view-mode a').removeClass('active');
		$(this).addClass('active');
		shopProductWrap.removeClass('grid-view list-view').addClass(viewMode);
	})


	// quantity change js
	$('.pro-qty').prepend('<span class="dec qtybtn">-</span>');
	$('.pro-qty').append('<span class="inc qtybtn">+</span>');
	$('.qtybtn').on('click', function () {
		var $button = $(this);
		var $input = $button.parent().find('input');
		var oldValue = parseInt($input.val(), 10) || 1;
		var maxValue = parseInt($input.attr('data-max-quantity'), 10) || 99;
		if (oldValue < 1) {
			oldValue = 1;
		}
		if ($button.hasClass('inc')) {
			var newVal = oldValue + 1;
		} else {
			if (oldValue > 1) {
				var newVal = oldValue - 1;
			} else {
				newVal = 1;
			}
		}
		if (newVal > maxValue) {
			newVal = maxValue;
		}
		$input.val(newVal);
	});
	$('.pro-qty input').on('input', function () {
		this.value = this.value.replace(/\D/g, '');
		var maxValue = parseInt(this.getAttribute('data-max-quantity'), 10) || 99;
		var value = parseInt(this.value, 10);
		if (value > maxValue) {
			this.value = maxValue;
		}
	});
	$('.pro-qty input').on('change', function () {
		var value = parseInt(this.value, 10);
		var maxValue = parseInt(this.getAttribute('data-max-quantity'), 10) || 99;
		if (!value || value < 1) {
			this.value = 1;
		} else if (value > maxValue) {
			this.value = maxValue;
		}
	});


	// Checkout Page accordion
	$("#create_pwd").on("change", function () {
		$(".account-create").slideToggle("100");
	});

	$("#ship_to_different").on("change", function () {
		$(".ship-to-different").slideToggle("100");
	});


	// Payment Method Accordion
	$('input[name="paymentmethod"]').on('click', function () {
		var $value = $(this).attr('value');
		$('.payment-method-details').slideUp();
		$('[data-method="' + $value + '"]').slideDown();
	});


	// scroll to top
	$(window).on('scroll', function () {
		if ($(this).scrollTop() > 600) {
			$('.scroll-top').removeClass('not-visible');
		} else {
			$('.scroll-top').addClass('not-visible');
		}
	});
	$('.scroll-top').on('click', function (event) {
		$('html,body').animate({
			scrollTop: 0
		}, 1000);
	});

	function initPageTransitions() {
		var body = document.body;

		function showPage() {
			body.classList.remove('page-leave');
			body.classList.add('page-ready');
		}

		function canTransition(link, event) {
			if (!link || !link.href || link.target === '_blank' || link.hasAttribute('download') || link.dataset.noTransition !== undefined) {
				return false;
			}

			if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.defaultPrevented) {
				return false;
			}

			var url = new URL(link.href, window.location.href);

			if (url.origin !== window.location.origin || url.pathname === window.location.pathname && url.search === window.location.search && url.hash) {
				return false;
			}

			return true;
		}

		window.addEventListener('load', showPage);
		window.addEventListener('pageshow', showPage);

		document.addEventListener('click', function (event) {
			var link = event.target.closest('a');

			if (!canTransition(link, event)) {
				return;
			}

			event.preventDefault();
			body.classList.add('page-leave');

			setTimeout(function () {
				window.location.href = link.href;
			}, 420);
		});

		document.addEventListener('submit', function (event) {
			var form = event.target;

			if (event.defaultPrevented || form.target === '_blank' || form.dataset.noTransition !== undefined) {
				return;
			}

			body.classList.add('page-leave');
		});
	}

	initPageTransitions();


}(jQuery));
