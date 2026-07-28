// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function animateBell() {

    let bell = $(".fa-bell");

    bell.addClass("bell-shake");

    setTimeout(function () {

        bell.removeClass("bell-shake");

    }, 700);

}