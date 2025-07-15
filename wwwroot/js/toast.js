    function showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.classList.add('toast', type);
        toast.innerText = message;

        const container = document.getElementById('toast-container');
        container.appendChild(toast);

        setTimeout(() => toast.classList.add('show'), 100);

        setTimeout(() => {
        toast.classList.remove('show');
            setTimeout(() => container.removeChild(toast), 500);
        }, 4000);
    }