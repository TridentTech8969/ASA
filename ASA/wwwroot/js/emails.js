// Enhanced Contact Form Handler
document.addEventListener('DOMContentLoaded', function () {
    const contactForm = document.getElementById('contactForm');
    const submitBtn = document.getElementById('submitBtn');

    if (contactForm) {
        contactForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            // Disable submit button to prevent double submission
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Sending...';

            try {
                const formData = new FormData(contactForm);

                const response = await fetch(contactForm.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                const result = await response.json();

                if (result.success) {
                    // Show success message
                    showAlert('success', result.message, result.referenceId);

                    // Reset form
                    contactForm.reset();

                    // Clear any validation errors
                    clearValidationErrors();

                    // Optional: Track the submission
                    if (typeof gtag !== 'undefined') {
                        gtag('event', 'contact_form_submit', {
                            'event_category': 'engagement',
                            'event_label': 'contact_form',
                            'value': 1
                        });
                    }
                } else {
                    // Show error message
                    showAlert('error', result.message);

                    // If there are field-specific errors, you can handle them here
                    if (result.errors) {
                        showValidationErrors(result.errors);
                    }
                }
            } catch (error) {
                console.error('Error submitting contact form:', error);
                showAlert('error', 'Sorry, there was a problem sending your message. Please try again later.');
            } finally {
                // Re-enable submit button
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-paper-plane me-2"></i>Send Message';
            }
        });
    }

    // Function to show alert messages
    function showAlert(type, message, referenceId = null) {
        // Remove existing alerts
        const existingAlerts = document.querySelectorAll('.contact-alert');
        existingAlerts.forEach(alert => alert.remove());

        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type === 'success' ? 'success' : 'danger'} alert-dismissible fade show contact-alert`;
        alertDiv.setAttribute('role', 'alert');

        const icon = type === 'success' ? 'fas fa-check-circle' : 'fas fa-exclamation-triangle';

        let alertContent = `
            <i class="${icon} me-2"></i>
            ${message}
        `;

        if (referenceId) {
            alertContent += `<br><small><strong>Reference ID:</strong> ${referenceId}</small>`;
        }

        alertContent += `
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;

        alertDiv.innerHTML = alertContent;

        // Insert alert at the top of the form
        contactForm.insertBefore(alertDiv, contactForm.firstChild);

        // Auto-hide success messages after 10 seconds
        if (type === 'success') {
            setTimeout(() => {
                if (alertDiv.parentNode) {
                    const bsAlert = new bootstrap.Alert(alertDiv);
                    bsAlert.close();
                }
            }, 10000);
        }

        // Scroll to the alert
        alertDiv.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    // Function to clear validation errors
    function clearValidationErrors() {
        const errorSpans = document.querySelectorAll('.text-danger');
        errorSpans.forEach(span => {
            if (span.getAttribute('data-valmsg-for')) {
                span.textContent = '';
            }
        });

        const invalidInputs = document.querySelectorAll('.is-invalid');
        invalidInputs.forEach(input => input.classList.remove('is-invalid'));
    }

    // Function to show validation errors
    function showValidationErrors(errors) {
        Object.keys(errors).forEach(fieldName => {
            const input = document.querySelector(`[name="${fieldName}"]`);
            const errorSpan = document.querySelector(`[data-valmsg-for="${fieldName}"]`);

            if (input) {
                input.classList.add('is-invalid');
            }

            if (errorSpan && errors[fieldName] && errors[fieldName].length > 0) {
                errorSpan.textContent = errors[fieldName][0];
            }
        });
    }

    // Real-time validation
    const formInputs = contactForm.querySelectorAll('input, select, textarea');
    formInputs.forEach(input => {
        input.addEventListener('blur', function () {
            validateField(this);
        });

        input.addEventListener('input', function () {
            // Clear error state when user starts typing
            this.classList.remove('is-invalid');
            const errorSpan = document.querySelector(`[data-valmsg-for="${this.name}"]`);
            if (errorSpan) {
                errorSpan.textContent = '';
            }
        });
    });

    // Field validation function
    function validateField(field) {
        const value = field.value.trim();
        const fieldName = field.name;
        let isValid = true;
        let errorMessage = '';

        // Required field validation
        if (field.hasAttribute('required') || field.getAttribute('data-val-required')) {
            if (!value) {
                isValid = false;
                errorMessage = `${getFieldLabel(field)} is required.`;
            }
        }

        // Email validation
        if (field.type === 'email' && value) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
                isValid = false;
                errorMessage = 'Please enter a valid email address.';
            }
        }

        // Phone validation
        if (field.type === 'tel' && value) {
            const phoneRegex = /^[\+]?[1-9][\d]{0,15}$/;
            if (!phoneRegex.test(value.replace(/[\s\-\(\)]/g, ''))) {
                isValid = false;
                errorMessage = 'Please enter a valid phone number.';
            }
        }

        // GST number validation (basic format check)
        if (fieldName === 'GSTNumber' && value) {
            const gstRegex = /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/;
            if (!gstRegex.test(value)) {
                isValid = false;
                errorMessage = 'Please enter a valid GST number (e.g., 22AAAAA0000A1Z5).';
            }
        }

        // String length validation
        const maxLength = field.getAttribute('maxlength') || field.getAttribute('data-val-maxlength-max');
        if (maxLength && value.length > parseInt(maxLength)) {
            isValid = false;
            errorMessage = `${getFieldLabel(field)} cannot exceed ${maxLength} characters.`;
        }

        // Update field appearance
        const errorSpan = document.querySelector(`[data-valmsg-for="${fieldName}"]`);

        if (isValid) {
            field.classList.remove('is-invalid');
            field.classList.add('is-valid');
            if (errorSpan) errorSpan.textContent = '';
        } else {
            field.classList.remove('is-valid');
            field.classList.add('is-invalid');
            if (errorSpan) errorSpan.textContent = errorMessage;
        }

        return isValid;
    }

    // Get user-friendly field label
    function getFieldLabel(field) {
        const label = document.querySelector(`label[for="${field.id}"]`);
        if (label) {
            return label.textContent.replace('*', '').trim();
        }

        // Fallback to field name
        return field.name.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase());
    }

    // Form validation before submit
    function validateForm() {
        let isFormValid = true;

        formInputs.forEach(input => {
            if (!validateField(input)) {
                isFormValid = false;
            }
        });

        return isFormValid;
    }

    // Character counter for message field
    const messageField = document.querySelector('#Message');
    if (messageField) {
        const maxLength = messageField.getAttribute('maxlength') || 4000;

        // Create character counter
        const counterDiv = document.createElement('div');
        counterDiv.className = 'form-text text-muted mt-1';
        counterDiv.innerHTML = `<small>Characters: <span id="char-count">0</span>/${maxLength}</small>`;
        messageField.parentNode.appendChild(counterDiv);

        const charCountSpan = document.getElementById('char-count');

        messageField.addEventListener('input', function () {
            const currentLength = this.value.length;
            charCountSpan.textContent = currentLength;

            if (currentLength > maxLength * 0.9) {
                charCountSpan.style.color = '#dc3545'; // Bootstrap danger color
            } else if (currentLength > maxLength * 0.8) {
                charCountSpan.style.color = '#ffc107'; // Bootstrap warning color
            } else {
                charCountSpan.style.color = '#6c757d'; // Bootstrap muted color
            }
        });
    }

    // Auto-format phone number
    const phoneField = document.querySelector('#PhoneNumber');
    if (phoneField) {
        phoneField.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, ''); // Remove non-digits

            // Format as +91 XXXXX XXXXX for Indian numbers
            if (value.startsWith('91') && value.length <= 12) {
                if (value.length > 2) {
                    value = '+91 ' + value.substring(2, 7) + (value.length > 7 ? ' ' + value.substring(7, 12) : '');
                }
            } else if (value.length <= 10 && !value.startsWith('91')) {
                // Assume it's a local Indian number
                if (value.length > 5) {
                    value = value.substring(0, 5) + ' ' + value.substring(5, 10);
                }
            }

            e.target.value = value;
        });
    }

    // Auto-format GST number
    const gstField = document.querySelector('#GSTNumber');
    if (gstField) {
        gstField.addEventListener('input', function (e) {
            e.target.value = e.target.value.toUpperCase();
        });
    }

    // Subject dropdown enhancement
    const subjectField = document.querySelector('#Subject');
    if (subjectField) {
        subjectField.addEventListener('change', function () {
            if (this.value === 'Other') {
                // Convert to text input for custom subject
                const textInput = document.createElement('input');
                textInput.type = 'text';
                textInput.name = this.name;
                textInput.id = this.id;
                textInput.className = this.className;
                textInput.placeholder = 'Please specify your subject...';
                textInput.required = this.required;

                this.parentNode.replaceChild(textInput, this);
                textInput.focus();
            }
        });
    }
});