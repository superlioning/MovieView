// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    const messageDiv = document.getElementById("tempMessage");
    if (messageDiv) {
        setTimeout(() => {
            messageDiv.style.display = "none";
        }, 2000);
    }
});
