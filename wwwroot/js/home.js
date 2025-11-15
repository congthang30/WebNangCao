// ============= HOME PAGE JAVASCRIPT =============

// Toast notification function
function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.innerHTML = `
        <div class="toast-content">
            <i class="bi ${type === 'success' ? 'bi-check-circle' : type === 'error' ? 'bi-exclamation-circle' : 'bi-info-circle'}"></i>
            <span>${message}</span>
        </div>
    `;
    
    document.body.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 100);
    
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => document.body.removeChild(toast), 300);
    }, 3000);
}

// Product card animations
document.addEventListener('DOMContentLoaded', function() {
    const cards = document.querySelectorAll('.product-card-modern');
    cards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.05}s`;
        card.classList.add('fade-in');
    });

    // Initialize wishlist buttons
    initializeWishlistButtons();
});

// Wishlist functionality
function initializeWishlistButtons() {
    const wishlistButtons = document.querySelectorAll('.wishlist-btn');
    
    wishlistButtons.forEach(button => {
        const productId = button.getAttribute('data-product-id');
        
        // Check initial wishlist status
        checkWishlistStatus(button, productId);
        
        // Add click handler
        button.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            toggleWishlist(this, productId);
        });
    });
}

function toggleWishlist(button, productId) {
    button.disabled = true;
    const icon = button.querySelector('i');
    const originalClasses = icon.className;
    
    // Show loading
    icon.className = 'bi bi-arrow-repeat bi-spin';
    
    fetch('/Wishlist?handler=AddWishListAjax', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: `productId=${productId}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (data.added) {
                icon.className = 'bi bi-heart-fill';
                button.style.color = '#ff6b6b';
                button.title = 'Xóa khỏi yêu thích';
                showToast(data.message, 'success');
            } else {
                icon.className = 'bi bi-heart';
                button.style.color = '#6c757d';
                button.title = 'Thêm vào yêu thích';
                showToast(data.message, 'info');
            }
        } else {
            icon.className = originalClasses;
            if (data.requireLogin) {
                showToast(data.message, 'error');
                setTimeout(() => {
                    window.location.href = '/Account/Login';
                }, 2000);
            } else {
                showToast(data.message, 'error');
            }
        }
    })
    .catch(error => {
        icon.className = originalClasses;
        showToast('Có lỗi xảy ra. Vui lòng thử lại sau.', 'error');
    })
    .finally(() => {
        button.disabled = false;
        button.style.transform = 'scale(1.2)';
        setTimeout(() => {
            button.style.transform = 'scale(1)';
        }, 200);
    });
}

function checkWishlistStatus(button, productId) {
    fetch(`/Wishlist?handler=CheckWishlistStatus&productId=${productId}`)
        .then(response => response.json())
        .then(data => {
            const icon = button.querySelector('i');
            if (data.inWishlist) {
                icon.className = 'bi bi-heart-fill';
                button.style.color = '#ff6b6b';
                button.title = 'Xóa khỏi yêu thích';
            } else {
                icon.className = 'bi bi-heart';
                button.style.color = '#6c757d';
                button.title = 'Thêm vào yêu thích';
            }
        })
        .catch(error => {
            // Default to not in wishlist if error
            const icon = button.querySelector('i');
            icon.className = 'bi bi-heart';
            button.style.color = '#6c757d';
            button.title = 'Thêm vào yêu thích';
        });
}

