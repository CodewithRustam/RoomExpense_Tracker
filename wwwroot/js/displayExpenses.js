$(function () {
    debugger; // ✅ should hit here

    const $body = $('body');
    const $settleModal = $('#settleModal');
    const $form = $('#editExpenseForm');
    const $saveBtn = $form.find('button[type="submit"]');
    let original = {};

    /** ---------- Edit Expense Modal ---------- **/
    $('.edit-expense-btn').on('click', function () {
        const $btn = $(this);
        $('#editExpenseModal').removeClass('hidden');
        $body.addClass('overflow-hidden');
        $('#expenseId').val($btn.data('expense-id'));
        $('#item').val($btn.data('item'));
        $('#amount').val($btn.data('amount'));
        $('#date').val($btn.data('date'));
        $('#roomId').val($btn.data('room-id'));

        original = {
            item: $btn.data('item'),
            amount: parseFloat($btn.data('amount')),
            date: $btn.data('date')
        };
        $saveBtn.prop('disabled', true);
    });

    $('#item, #amount, #date').on('input change', function () {
        const changed =
            $('#item').val() !== original.item ||
            parseFloat($('#amount').val() || 0) !== original.amount ||
            $('#date').val() !== original.date;

        $saveBtn.prop('disabled', !changed);
    });

    /** ---------- Settle Up Modal ---------- **/
    $('.settle-btn').on('click', function () {
        const $btn = $(this);
        $settleModal.removeClass('hidden');
        $body.addClass('overflow-hidden');

        $('#settleRoomId').val($btn.data('room-id'));
        $('#settleMemberName').val($btn.data('member-name'));
        $('#settleMemberDisplay').text($btn.data('member-name'));
        $('#settleMonth').val($('#monthSelect').val());

        $('#settleAmount, #paidToMemberName').val('');
        $('#settleAmountDisplay, #paidToMemberDisplay').text('');
        $('#confirmSettleBtn').prop('disabled', true);
    });

    $('#paidToMemberName').on('change', function () {
        const $selected = $(this).find('option:selected');
        const settlementMember = $selected.val();
        const owedAmount = parseFloat($selected.data('owed-amount') || 0);
        const oweAmount = parseFloat($('.settle-btn').data('amount') || 0);

        if (settlementMember) {
            const settlementAmount = Math.min(Math.abs(oweAmount), Math.abs(owedAmount)).toFixed(2);
            $('#settleAmount').val(settlementAmount);
            $('#settleAmountDisplay').text('₹' + settlementAmount);
            $('#paidToMemberDisplay').text(settlementMember);
            $('#confirmSettleBtn').prop('disabled', false);
        } else {
            $('#settleAmount, #paidToMemberName').val('');
            $('#settleAmountDisplay, #paidToMemberDisplay').text('');
            $('#confirmSettleBtn').prop('disabled', true);
        }
    });

    /** ---------- Close Modals ---------- **/
    $('[data-modal-hide]').on('click', function () {
        $(`#${$(this).data('modal-hide')}`).addClass('hidden');
        $body.removeClass('overflow-hidden');
    });

    /** ---------- Tab Switch (Pills) ---------- **/
    document.querySelectorAll(".pill-switch").forEach(container => {
        const indicator = container.querySelector(".indicator");
        const buttons = container.querySelectorAll(".tab-btn");

        function setTab(index) {
            buttons.forEach((btn, i) => {
                if (i === index) {
                    btn.classList.add("text-white");
                    btn.classList.remove("text-gray-500");
                } else {
                    btn.classList.remove("text-white");
                    btn.classList.add("text-gray-500");
                }
            });

            // move indicator
            indicator.style.transform = `translateX(${index * 100}%)`;

            // optional: change gradient per tab
            if (index === 0) {
                indicator.style.backgroundImage = "linear-gradient(90deg, #6366f1, #a855f7, #ec4899)"; // Split
            } else {
                indicator.style.backgroundImage = "linear-gradient(90deg, #34d399, #14b8a6, #059669)"; // Non-Split
            }
        }

        // attach events
        buttons.forEach((btn, i) => btn.addEventListener("click", () => setTab(i)));

        // default
        setTab(0);
    });
});