$(document).ready(function() {
    // Handle ticket form submission with validation
    $('#ticketForm').on('submit', function(e) {
        if (!validateTicketForm()) {
            e.preventDefault();
        }
    });
    
    // Close ticket confirmation dialog
    $('.close-ticket-btn').on('click', function(e) {
        e.preventDefault();
        const ticketId = $(this).data('id');
        
        if (confirm('Are you sure you want to close this ticket? This action cannot be undone.')) {
            $('#closeTicketForm' + ticketId).submit();
        }
    });
    
    // Reopen ticket confirmation dialog
    $('.reopen-ticket-btn').on('click', function(e) {
        e.preventDefault();
        const ticketId = $(this).data('id');
        
        if (confirm('Are you sure you want to reopen this ticket?')) {
            $('#reopenTicketForm' + ticketId).submit();
        }
    });
    
    // Ticket category-subcategory relationship
    $('#Category').on('change', function() {
        updateSubcategories();
    });
    
    // Initialize subcategories on page load
    if ($('#Category').length) {
        updateSubcategories();
    }
    
    // Filter tickets functionality
    $('#ticketFilter').on('change', function() {
        const filterValue = $(this).val();
        
        if (filterValue === 'all') {
            $('.ticket-item').show();
        } else {
            $('.ticket-item').hide();
            $('.ticket-item[data-status="' + filterValue + '"]').show();
        }
    });
    
    // Search tickets functionality
    $('#ticketSearch').on('keyup', function() {
        const searchText = $(this).val().toLowerCase();
        
        $('.ticket-item').each(function() {
            const subject = $(this).find('.ticket-subject').text().toLowerCase();
            const description = $(this).find('.ticket-description').text().toLowerCase();
            
            if (subject.includes(searchText) || description.includes(searchText)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });
    
    // Function to validate ticket form
    function validateTicketForm() {
        let isValid = true;
        
        // Clear previous validation messages
        $('.validation-message').remove();
        
        // Validate Subject
        if ($('#Subject').val().trim() === '') {
            $('#Subject').after('<div class="validation-message text-danger">Subject is required</div>');
            isValid = false;
        }
        
        // Validate Description
        if ($('#Description').val().trim() === '') {
            $('#Description').after('<div class="validation-message text-danger">Description is required</div>');
            isValid = false;
        }
        
        // Validate Category
        if ($('#Category').val() === '') {
            $('#Category').after('<div class="validation-message text-danger">Category is required</div>');
            isValid = false;
        }
        
        return isValid;
    }
    
    // Function to update subcategories based on selected category
    function updateSubcategories() {
        const category = $('#Category').val();
        const $subcategorySelect = $('#SubCategory');
        
        if (!$subcategorySelect.length) return;
        
        $subcategorySelect.empty();
        $subcategorySelect.append('<option value="">-- Select Subcategory --</option>');
        
        if (category === 'Hardware') {
            $subcategorySelect.append('<option value="Desktop">Desktop</option>');
            $subcategorySelect.append('<option value="Laptop">Laptop</option>');
            $subcategorySelect.append('<option value="Printer">Printer</option>');
            $subcategorySelect.append('<option value="Network">Network</option>');
            $subcategorySelect.append('<option value="Other">Other</option>');
        } else if (category === 'Software') {
            $subcategorySelect.append('<option value="Operating System">Operating System</option>');
            $subcategorySelect.append('<option value="Applications">Applications</option>');
            $subcategorySelect.append('<option value="Email">Email</option>');
            $subcategorySelect.append('<option value="Database">Database</option>');
            $subcategorySelect.append('<option value="Other">Other</option>');
        } else if (category === 'Network') {
            $subcategorySelect.append('<option value="Internet">Internet</option>');
            $subcategorySelect.append('<option value="VPN">VPN</option>');
            $subcategorySelect.append('<option value="Email">Email</option>');
            $subcategorySelect.append('<option value="Connectivity">Connectivity</option>');
            $subcategorySelect.append('<option value="Other">Other</option>');
        } else if (category === 'Account') {
            $subcategorySelect.append('<option value="Password Reset">Password Reset</option>');
            $subcategorySelect.append('<option value="Access Request">Access Request</option>');
            $subcategorySelect.append('<option value="Permissions">Permissions</option>');
            $subcategorySelect.append('<option value="Other">Other</option>');
        } else if (category === 'Other') {
            $subcategorySelect.append('<option value="Other">Other</option>');
        }
    }
}); 