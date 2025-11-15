// ============= PRODUCT DETAIL PAGE JAVASCRIPT =============

// Image zoom effect
document.addEventListener('DOMContentLoaded', function() {
    const productImage = document.querySelector('.product-main-image');
    
    if (productImage) {
        productImage.addEventListener('click', function() {
            this.classList.toggle('zoomed');
        });
    }

    // Smooth scroll to reviews when clicking review count
    const reviewLinks = document.querySelectorAll('[href="#reviews"]');
    reviewLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            document.querySelector('.reviews-section')?.scrollIntoView({ 
                behavior: 'smooth' 
            });
        });
    });

    // Add to cart animation
    const addToCartBtn = document.querySelector('.btn-primary');
    if (addToCartBtn && addToCartBtn.textContent.includes('Thêm vào giỏ hàng')) {
        addToCartBtn.addEventListener('click', function() {
            // Animation effect
            this.innerHTML = '<i class="bi bi-check-circle me-2"></i>Đã thêm!';
            this.classList.add('btn-success');
            this.classList.remove('btn-primary');
            
            setTimeout(() => {
                this.innerHTML = '<i class="bi bi-cart-plus me-2"></i>Thêm vào giỏ hàng';
                this.classList.add('btn-primary');
                this.classList.remove('btn-success');
            }, 2000);
        });
    }

    // Wishlist button functionality
    const wishlistBtn = document.querySelector('.wishlist-btn-detail');
    if (wishlistBtn) {
        const productId = wishlistBtn.getAttribute('data-product-id');
        
        // Check initial wishlist status
        checkWishlistStatusDetail(wishlistBtn, productId);
        
        // Add click handler
        wishlistBtn.addEventListener('click', function() {
            toggleWishlistDetail(this, productId);
        });
    }
});

function toggleWishlistDetail(button, productId) {
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
                button.classList.remove('btn-outline-danger');
                button.classList.add('btn-danger');
                button.title = 'Xóa khỏi yêu thích';
                showToastDetail(data.message, 'success');
            } else {
                icon.className = 'bi bi-heart';
                button.classList.remove('btn-danger');
                button.classList.add('btn-outline-danger');
                button.title = 'Thêm vào yêu thích';
                showToastDetail(data.message, 'info');
            }
        } else {
            icon.className = originalClasses;
            if (data.requireLogin) {
                showToastDetail(data.message, 'error');
                setTimeout(() => {
                    window.location.href = '/Account/Login';
                }, 2000);
            } else {
                showToastDetail(data.message, 'error');
            }
        }
    })
    .catch(error => {
        icon.className = originalClasses;
        showToastDetail('Có lỗi xảy ra. Vui lòng thử lại sau.', 'error');
    })
    .finally(() => {
        button.disabled = false;
    });
}

function checkWishlistStatusDetail(button, productId) {
    fetch(`/Wishlist?handler=CheckWishlistStatus&productId=${productId}`)
        .then(response => response.json())
        .then(data => {
            const icon = button.querySelector('i');
            if (data.inWishlist) {
                icon.className = 'bi bi-heart-fill';
                button.classList.remove('btn-outline-danger');
                button.classList.add('btn-danger');
                button.title = 'Xóa khỏi yêu thích';
            } else {
                icon.className = 'bi bi-heart';
                button.classList.remove('btn-danger');
                button.classList.add('btn-outline-danger');
                button.title = 'Thêm vào yêu thích';
            }
        })
        .catch(error => {
            // Default to not in wishlist if error
            const icon = button.querySelector('i');
            icon.className = 'bi bi-heart';
            button.classList.remove('btn-danger');
            button.classList.add('btn-outline-danger');
            button.title = 'Thêm vào yêu thích';
        });
}

function showToastDetail(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.style.position = 'fixed';
    toast.style.top = '20px';
    toast.style.right = '20px';
    toast.style.zIndex = '9999';
    toast.style.padding = '1rem 1.5rem';
    toast.style.borderRadius = '8px';
    toast.style.color = 'white';
    toast.style.fontWeight = '500';
    toast.style.transform = 'translateX(100%)';
    toast.style.transition = 'transform 0.3s ease';
    toast.style.boxShadow = '0 4px 12px rgba(0, 0, 0, 0.15)';
    toast.style.maxWidth = '400px';
    
    const bgColors = {
        success: 'linear-gradient(135deg, #28a745, #20c997)',
        error: 'linear-gradient(135deg, #dc3545, #fd7e14)',
        info: 'linear-gradient(135deg, #17a2b8, #6f42c1)'
    };
    toast.style.background = bgColors[type] || bgColors.info;
    
    toast.innerHTML = `
        <div style="display: flex; align-items: center; gap: 0.5rem;">
            <i class="bi ${type === 'success' ? 'bi-check-circle' : type === 'error' ? 'bi-exclamation-circle' : 'bi-info-circle'}" style="font-size: 1.2rem;"></i>
            <span>${message}</span>
        </div>
    `;
    
    document.body.appendChild(toast);
    setTimeout(() => toast.style.transform = 'translateX(0)', 100);
    
    setTimeout(() => {
        toast.style.transform = 'translateX(100%)';
        setTimeout(() => document.body.removeChild(toast), 300);
    }, 3000);
}

