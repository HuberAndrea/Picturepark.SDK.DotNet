$(document).ready(function () {
    //translations - insert your custom translations here

    //header information
    var contactBtn = "Contact Picturepark";
    var entryTitle = "You are watching a sample demo of a Press Kits microsite of <br /> the Picturepark Content Platform."
    var et_pb_text_inner = "<p>Other samples are available - <a target='_blank' href='https://picturepark.com/'>please click here.</a></p>";
    var searchPlaceholder = "Search...";

    //overview informaiton
    var lastPageBtn = "Last page";
    var nextPageBtn = "Next page";

    //detail view information
    var backOverviewLink = "<span class='separator'>#</span> Back to Overview";
    var articleAuthor = "By ";

    //search results information
    var searchResults = "Search results for ";
    var searchNoResults = "No search results <br /> <a href='/'>Back to Overview</a>";

    //footer information
    var privacyPolicy = "Privacy";
    var termsOfUse = "Terms of use";



    //fill elements with translations
    $("#contactBtn").html(contactBtn);
    $("#menu-item-6723 a").html(contactBtn);
    $(".L1header .entry-title").html(entryTitle);
    $(".L1header .et_pb_text_inner").html(et_pb_text_inner);
    $(".et_search_outer .et-search-field").attr("placeholder", searchPlaceholder);
    $("#lastPage").html(lastPageBtn);
    $("#nextPage").html(nextPageBtn);
    $("#privacyLink").html(privacyPolicy);
    $("#termsOfUseLink").html(termsOfUse);
    $(".breadcrumbsBox .breadcrumbs a").html(backOverviewLink);
    $(".entry-content .et_pb_text_inner .post-meta .author").before(articleAuthor);
    $("#searchResultsTerm").prepend(searchResults);
    $("#searchNoResults").html(searchNoResults);
});