//make header white if scroll position is not top
$(window).scroll(function () {
    var ScrollTop = parseInt($(window).scrollTop());

    if (ScrollTop > 150) {
        $("#main-header").addClass("scrollHeader", 500);
    }
    else {
        $("#main-header").removeClass("scrollHeader", 500);
    }
});


/*--------ARTICLE OVERFLOW HANDLER START-----------*/
var articleCount = {}; //define global vars
var targetPage = {};
var page = {}; //create global var for page count in for loop
var divInnerText = {};
var currentPage = {};

$(document).ready(function () { //set up paging function
    articleCount.postNumber = $(".articlePreview").length; //fill global var

    if (articleCount.postNumber > 9) {
        $(".articlePreview:gt(8)").hide(); //hide overflow articles
        $("#pageCount").addClass("multiPageActive"); //show older entries button
    
        var divideByNine = Math.ceil(articleCount.postNumber / 9); //variables required for the loop
        var para1 = 0;
        var para2 = 9;
        targetPage.single = 1;

        for (targetPage.single; targetPage.single < divideByNine + 1; ++targetPage.single) {
            page[targetPage.single] = $(".articlePreview").slice(para1, para2);  //create a new var for every page

            para1 = para1 + 9;
            para2 = para2 + 9;
            $("#pageCount").append("<div id='pageNr" + targetPage.single + "' class='pageNr'>" + targetPage.single + "</div>");
        }

        $("#pageNr1").addClass("pageActive"); //default active page
        $("#nextPage").addClass("multiPageActive");

        //cut overload articles and replace with ...
        var pageCount = $(".pageNr");
        if (pageCount.length > 4) {
            console.log("über 4");

            var pageOverload = pageCount.toArray().length - 3;
            pageOverload = pageCount.slice(pageOverload);
            console.log(pageOverload);
            pageOverload.hide();
            $("#pageCount").append("<div id='pageOverload'> ...</div>");
        }
    }
});



$(document).ready(function () { //paging function
    $(".pageNr").click(function () {
        divInnerText.global = $(this).text();
        globalF1();

        $(".pageNr").removeClass("pageActive");
        $(this).addClass("pageActive");

        $("html, body").animate({ scrollTop: 0 }, "slow");

        if ($("#pageNr1").hasClass("pageActive")) {
            $("#nextPage").addClass("multiPageActive");
            $("#lastPage").removeClass("multiPageActive");
        }
        else if (page[targetPage.single - 1]) {
            $("#nextPage").removeClass("multiPageActive");
            $("#lastPage").addClass("multiPageActive");
        }
        else {
            $("#nextPage").addClass("multiPageActive");
            $("#lastPage").addClass("multiPageActive");
        }
    });
});

function globalF1() {
    $.each(page, function (index, value) {
        if (index == divInnerText.global) {
            $(this).show();
        }
        else {
            $(this).hide();
        }
    });
}

function navPagesGlobal() {
    var currentPage = parseInt($(".pageActive").text(), 10);

    var currentPageDiv = "#pageNr" + currentPage;
    console.log(currentPageDiv);
    $(".pageNr").removeClass("pageActive");
    $(this).addClass("pageActive");

    $("html, body").animate({ scrollTop: 0 }, "slow");

    if ($("#pageNr1").hasClass("pageActive")) {
        $("#nextPage").addClass("multiPageActive");
        $("#lastPage").removeClass("multiPageActive");
    }
    else if (page[targetPage.single - 1]) {
        $("#nextPage").removeClass("multiPageActive");
        $("#lastPage").addClass("multiPageActive");
    }
    else {
        $("#nextPage").addClass("multiPageActive");
        $("#lastPage").addClass("multiPageActive");
    }
}

function showNextPage() {
    //find out what page is active
    var divInnerText = $(".pageActive").text();
    
    //move to page = current page +1
    var nextPage = parseInt(divInnerText, 10) + 1;

    $(".pageNr").removeClass("pageActive");
    $("#pageCount").find("#pageNr" + nextPage).addClass("pageActive");

    $.each(page, function (index, value) {
        if (index == nextPage) {
            $(this).show();
        }
        else {
            $(this).hide();
        }
    });

    $("html, body").animate({ scrollTop: 0 }, "slow");

    if ($("#pageNr1").hasClass("pageActive")) {
        $("#nextPage").addClass("multiPageActive");
        $("#lastPage").removeClass("multiPageActive");
    }
    else if (page[targetPage.single - 1]) {
        $("#nextPage").removeClass("multiPageActive");
        $("#lastPage").addClass("multiPageActive");
    }
    else {
        $("#nextPage").addClass("multiPageActive");
        $("#lastPage").addClass("multiPageActive");
    }

}

function showLastPage() {
    //find out what page is active
    var divInnerText = $(".pageActive").text();

    //move to page = current page +1
    var lastPage = parseInt(divInnerText, 10) -1;

    $(".pageNr").removeClass("pageActive");
    $("#pageCount").find("#pageNr" + lastPage).addClass("pageActive");

    $.each(page, function (index, value) {
        if (index == lastPage) {
            $(this).show();
        }
        else {
            $(this).hide();
        }
    });

    $("html, body").animate({ scrollTop: 0 }, "slow");

    if ($("#pageNr1").hasClass("pageActive")) {
        $("#nextPage").addClass("multiPageActive");
        $("#lastPage").removeClass("multiPageActive");
    }
    else if (page[targetPage.single - 1]) {
        $("#nextPage").removeClass("multiPageActive");
        $("#lastPage").addClass("multiPageActive");
    }
    else {
        $("#nextPage").addClass("multiPageActive");
        $("#lastPage").addClass("multiPageActive");
    }
}
/*--------ARTICLE OVERFLOW HANDLER END-----------*/


$(document).ready(function () {
    $("#et_search_icon").click(function () {
        $(".et_search_outer").addClass("searchActive");

        $(".et_close_search_field").click(function () {
            $(".et_search_outer").removeClass("searchActive");
        });
    });

    $(".entry-title").click(function () {
        $(".et_search_outer").removeClass("searchActive");
    });

});

$(document).ready(function () {
    $("#et_search_icon").click(function () { //move elements when opening search
        $(".et-search-form").fadeIn(500); //delay in opening search field

        if ($(window).width() > 1085) {
            $("#main-header").addClass("externalSearchActive", 500);
        }
        $(".et-search-field").focus();
        
        $(document).click(function (e) { //move elements when closing search
            if ($(e.target).closest("#main-header").length === 0) {
                $(".et_close_search_field").click();
                $("#main-header").removeClass("externalSearchActive", 500);
            }
            else if (e.target === $(".et_close_search_field")[0]) {
                $("#main-header").removeClass("externalSearchActive", 500);
            }
        });
    });
});


function hideDownloads() { //show donwloads only if any are available
    if ($("#downloadWrapper").children(".thumb").length === 0) {
        $("#downloadWrapper").hide();
    }
}


//mobile nav toggle function
function activateMobileNav() {
    if ($(window).width() < 1085) {
        //fill mobile nav with nav items
        $("#et_top_search").appendTo("#mobileNavItems").addClass("mobileNavActiveItems");
        $(".et_search_outer").appendTo("#et_top_search").addClass("mobileNavActiveItems");
        $("#top-menu-nav").appendTo("#mobileNavItems").addClass("mobileNavActiveItems");
        $("#moreInfo").appendTo("#mobileNavItems").addClass("mobileNavActiveItems");


        $(".mobile_menu_bar_toggle").click(function () {
            //mobile nav icon
            $(".mobile_nav").toggleClass("closed");
            $(".mobile_nav").toggleClass("opened");

            //display mobile nav and its components
            $("#mobileNavItems").toggleClass("mobileNavSleeping");
            $("#mobileNavItems").toggleClass("mobileNavActive");
            $(".logo_container").toggleClass("mobileNavActiveItems");
            $(".a2a_kit ").toggleClass("mobileNavActiveItems"); //hide social sharing links
        });
    }
    else {
        $("#moreInfo").appendTo("#et-top-navigation").removeClass("mobileNavActiveItems");
        $("#et_top_search").appendTo("#et-top-navigation").removeClass("mobileNavActiveItems");
        $(".et_search_outer").appendTo("#main-header").removeClass("mobileNavActiveItems");
        $("#top-menu-nav").appendTo("#et-top-navigation").removeClass("mobileNavActiveItems");
    }
}
$(document).ready(function () {
    activateMobileNav();
});
$(window).resize(function () {
    activateMobileNav();
});

function moreInfoOpen() {
    $("#moreInfoTooltip").toggleClass("moreInfoActive");

    $(window).scroll(function () { //close tooltip on scroll
        var ScrollTop = parseInt($(window).scrollTop());

        if (ScrollTop > 350) {
            $("#moreInfoTooltip").removeClass("moreInfoActive");
        }
    });

    $(document).click(function (e) { //close tooltip on click anywhere
        if ($(e.target).closest("#moreInfo").length === 0) {
            $("#moreInfoTooltip").removeClass("moreInfoActive");
        }
    });
}