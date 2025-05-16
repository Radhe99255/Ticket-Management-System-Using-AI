// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Common site-wide functionality
$(document).ready(function() {
    // Handling flash messages/alerts with auto-dismiss
    setTimeout(function() {
        $('.alert-dismissible').fadeOut('slow');
    }, 5000);
    
    // Add tooltip initialization
    $('[data-toggle="tooltip"]').tooltip();
    
    // Back button handling
    $('.btn-back').on('click', function(e) {
        e.preventDefault();
        window.history.back();
    });
    
    // Confirm delete/dangerous actions
    $('.confirm-action').on('click', function(e) {
        const message = $(this).data('confirm-message') || 'Are you sure you want to perform this action?';
        if (!confirm(message)) {
            e.preventDefault();
        }
    });
    
    // Toggle sidebar if present
    $('#sidebarToggle').on('click', function(e) {
        e.preventDefault();
        $('body').toggleClass('sidebar-toggled');
        $('.sidebar').toggleClass('toggled');
    });
    
    // Mobile menu toggle
    $('.navbar-toggler').on('click', function() {
        if ($('.navbar-collapse').hasClass('show')) {
            $('.navbar-collapse').removeClass('show');
        } else {
            $('.navbar-collapse').addClass('show');
        }
    });
    
    // Initialize any date pickers
    if ($.fn.datepicker) {
        $('.datepicker').datepicker({
            format: 'mm/dd/yyyy',
            autoclose: true,
            todayHighlight: true
        });
    }
});
