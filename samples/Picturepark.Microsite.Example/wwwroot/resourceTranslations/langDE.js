$(document).ready(function () {
    //translations - insert you're custom translations here

    //header information
    var contactBtn = "Picturepark kontaktieren";
    var entryTitle = "Sie sehen eine Demo einer Presseportal Microsite <br /> der Picturepark Content Platform."
    var et_pb_text_inner = "<p>Weitere Beispiele sind verfügbar - <a target='_blank' href='https://picturepark.com/'>klicken Sie hier.</a></p>";
    var searchPlaceholder = "Suchen...";

    //overview information
    var lastPageBtn = "Zurück";
    var nextPageBtn = "Weiter";

    //detail view information
    var backOverviewLink = "<span class='separator'>#</span> Zurück zur Übersicht";
    var articleAuthor = "Von ";

    //search results information
    var searchResults = "Suchresultate für ";
    var searchNoResults = "Keine Suchresultate gefunden <br /> <a href='/'>Zurück zur Übersicht</a>";

    //footer information
    var privacyPolicy = "Datenschutzbedingungen";
    var termsOfUse = "Nutzungsbedingungen";



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