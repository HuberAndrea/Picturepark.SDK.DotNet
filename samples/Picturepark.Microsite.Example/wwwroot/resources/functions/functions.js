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
//define global vars
var articleCount = {}; 
var targetPage = {};
var page = {}; //create global var for page count in for loop

$(document).ready(function () { //set up paging function
    articleCount.postNumber = $(".articlePreview").length; //fill global var

    if (articleCount.postNumber > 9) {
        $(".articlePreview:gt(8)").hide(); //hide overflow articles
        $("#pageCount").addClass("multiPageActive"); //show older entries button
    

        //variables required for the loop
        var divideByNine = Math.ceil(articleCount.postNumber / 9); //count how many pages need to be displayed
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
    }
});

$(document).ready(function () { //paging function
    $(".pageNr").click(function () {
        navPagesGlobal();

        /*var divInnerText = $(this).text();
        $.each(page, function (index, value) {
            if (index == divInnerText) {
                $(this).show();
            }
            else {
                $(this).hide();
            }
        });*/

    });
});

function navPagesGlobal() {
    var currentPage = parseInt($(".pageActive").text(), 10);
    $.each(page, function (index, value) {
        if (index == currentPage) {
            $(this).show();
        }
        else {
            $(this).hide();
        }
    });

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
    var nextPage = currentPage.instance + 1;
    console.log("The next page is page nr " + nextPage);
}

function showLastPage() {
    var lastPage = currentPage.instance - 1;
    console.log("The last page is page nr " + lastPage);
}
/*--------ARTICLE OVERFLOW HANDLER START-----------*/

