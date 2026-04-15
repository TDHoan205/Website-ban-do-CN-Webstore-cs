/* ================================================
   Webstore Global JavaScript
   ================================================ */

// ================================================
// TOAST NOTIFICATION (Global)
//
// Usage:
//   showToast('success', 'Thành công!');
//   showToast('error',   'Có lỗi xảy ra!');
//   showToast('warning', 'Cảnh báo!');
//   showToast('info',    'Thông tin!');
// ================================================
window.showToast = function(type, message, duration) {
    var icons = {
        success: 'fa-check-circle',
        error:   'fa-exclamation-circle',
        warning: 'fa-exclamation-triangle',
        info:    'fa-info-circle'
    };
    // Remove any existing toasts
    var existing = document.querySelectorAll('.alert-toast');
    existing.forEach(function(el) { el.remove(); });

    var toast = document.createElement('div');
    toast.className = 'alert-toast alert-toast-' + (type || 'info');
    toast.innerHTML =
        '<i class="fas ' + (icons[type] || 'fa-info-circle') + '"></i>' +
        '<span style="flex:1">' + message + '</span>' +
        '<button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>';

    document.body.appendChild(toast);

    var timeout = duration || 3500;
    setTimeout(function() {
        if (toast.parentElement) {
            toast.style.animation = 'toastSlideIn 0.3s ease reverse';
            setTimeout(function() {
                if (toast.parentElement) toast.remove();
            }, 300);
        }
    }, timeout);
};


// ================================================
// FORMAT CURRENCY
//
// Usage:
//   formatCurrency(1234567) => "1.234.567 ₫"
// ================================================
function formatCurrency(amount) {
    return amount.toLocaleString('vi-VN') + ' \u20AB';
}


// ================================================
// PRODUCT IMAGE PLACEHOLDER
//
// Usage: called by Razor views to generate
// a consistent fallback when ImageUrl is empty.
// ================================================
function getProductPlaceholder(productName) {
    var name = productName || 'SP';
    return '<div class="img-placeholder">' +
        '<i class="fas fa-box" style="font-size:3rem;"></i>' +
        '<span class="placeholder-name">' + name + '</span>' +
        '</div>';
}

// ================================================
// GLOBAL IMAGE FALLBACK
// Replaces broken/missing image sources consistently
// across admin + shop pages.
// ================================================
(function setupGlobalImageFallback() {
    var FALLBACK_SRC = '/images/products/placeholder.svg';

    function applyFallback(img) {
        if (!img || img.dataset.fallbackApplied === '1') return;
        img.dataset.fallbackApplied = '1';
        img.src = FALLBACK_SRC;
    }

    function bindImage(img) {
        if (!img) return;

        if (!img.getAttribute('loading')) {
            img.setAttribute('loading', 'lazy');
        }

        if (!img.src || !img.src.trim()) {
            applyFallback(img);
            return;
        }

        img.addEventListener('error', function onImageError() {
            applyFallback(img);
        }, { once: true });
    }

    function bindAll(root) {
        var scope = root || document;
        var imgs = scope.querySelectorAll('img');
        imgs.forEach(bindImage);
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindAll(document);

        // Handle dynamically injected images (AJAX/search results/modals)
        var observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (m) {
                m.addedNodes.forEach(function (node) {
                    if (!node || node.nodeType !== 1) return;
                    if (node.tagName === 'IMG') {
                        bindImage(node);
                    } else {
                        bindAll(node);
                    }
                });
            });
        });

        observer.observe(document.body, { childList: true, subtree: true });
    });
})();
