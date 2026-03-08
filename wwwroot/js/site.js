window.imageModalInstance = null;

window.initImageModal = function() {
    const contentImages = document.querySelectorAll('.content img');
    const modalElement = document.getElementById('imageModal');
    const modalImage = document.getElementById('modalImage');

    // Dispose existing modal instance if it exists
    if (window.imageModalInstance) {
        window.imageModalInstance.dispose();
    }

    // Create new modal instance
    window.imageModalInstance = new bootstrap.Modal(modalElement);

    contentImages.forEach(img => {
        // Remove inline width and height to allow CSS width: 100% to work
        img.style.width = '';
        img.style.height = '';
        img.removeAttribute('width');
        img.removeAttribute('height');
        
        // Remove any existing click listeners by cloning (prevents duplicates)
        const newImg = img.cloneNode(true);
        img.parentNode.replaceChild(newImg, img);
        
        // Add click handler for modal
        newImg.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            modalImage.src = this.src;
            modalImage.alt = this.alt;
            window.imageModalInstance.show();
        });
        
        // Also prevent the parent <a> tag from navigating
        const parentLink = newImg.closest('a');
        if (parentLink) {
            parentLink.addEventListener('click', function(e) {
                e.preventDefault();
            });
        }
    });

    // Clean up backdrop when modal is hidden
    modalElement.addEventListener('hidden.bs.modal', function () {
        document.body.classList.remove('modal-open');
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());
    });
};