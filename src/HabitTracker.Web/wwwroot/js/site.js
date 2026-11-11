/* HTMS client behaviour: sidebar, confirmations, chart helpers, form helpers. */
(function () {
    'use strict';

    function initSidebar() {
        var sidebar = document.getElementById('htmsSidebar');
        var backdrop = document.getElementById('htmsBackdrop');
        var toggle = document.getElementById('htmsSidebarToggle');
        if (!sidebar || !toggle) return;

        function close() {
            sidebar.classList.remove('open');
            if (backdrop) backdrop.classList.remove('show');
        }

        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('open');
            if (backdrop) backdrop.classList.toggle('show', sidebar.classList.contains('open'));
        });

        if (backdrop) backdrop.addEventListener('click', close);
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') close(); });
        window.addEventListener('resize', function () { if (window.innerWidth >= 992) close(); });
    }

    function initConfirm() {
        document.querySelectorAll('form[data-confirm]').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                var message = form.getAttribute('data-confirm') || 'Are you sure?';
                if (!window.confirm(message)) {
                    e.preventDefault();
                    e.stopPropagation();
                }
            });
        });
    }

    function initAlerts() {
        document.querySelectorAll('.alert[data-auto-dismiss]').forEach(function (el) {
            setTimeout(function () {
                el.style.transition = 'opacity .4s ease';
                el.style.opacity = '0';
                setTimeout(function () { el.remove(); }, 420);
            }, 6000);
        });
    }

    function initPasswordToggle() {
        document.querySelectorAll('[data-toggle-password]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var target = document.querySelector(btn.getAttribute('data-toggle-password'));
                if (!target) return;
                var isPassword = target.type === 'password';
                target.type = isPassword ? 'text' : 'password';
                var icon = btn.querySelector('i');
                if (icon) icon.className = isPassword ? 'bi bi-eye-slash' : 'bi bi-eye';
                btn.setAttribute('aria-label', isPassword ? 'Hide password' : 'Show password');
            });
        });
    }

    function initHabitForm() {
        var frequency = document.getElementById('FrequencyType');
        var scheduleBlock = document.getElementById('scheduleBlock');
        if (frequency && scheduleBlock) {
            var sync = function () {
                var needsDays = frequency.value === '1' || frequency.value === '2';
                scheduleBlock.style.display = needsDays ? '' : 'none';
            };
            frequency.addEventListener('change', sync);
            sync();
        }

        document.querySelectorAll('[data-color-swatch]').forEach(function (swatch) {
            swatch.addEventListener('click', function () {
                var input = document.getElementById('Color');
                if (input) input.value = swatch.getAttribute('data-color-swatch');
                document.querySelectorAll('[data-color-swatch]').forEach(function (s) { s.classList.remove('selected'); });
                swatch.classList.add('selected');
            });
        });

        document.querySelectorAll('[data-icon-option]').forEach(function (option) {
            option.addEventListener('click', function () {
                var input = document.getElementById('Icon');
                if (input) input.value = option.getAttribute('data-icon-option');
                document.querySelectorAll('[data-icon-option]').forEach(function (o) { o.classList.remove('selected'); });
                option.classList.add('selected');
            });
        });
    }

    function initTracker() {
        document.querySelectorAll('[data-tracker-form]').forEach(function (form) {
            form.addEventListener('submit', function () {
                var btn = form.querySelector('button[type="submit"]');
                if (btn) {
                    btn.disabled = true;
                    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving';
                }
            });
        });
    }

    function initChartDefaults() {
        if (typeof Chart === 'undefined') return;
        Chart.defaults.font.family = '"Segoe UI", -apple-system, BlinkMacSystemFont, Arial, sans-serif';
        Chart.defaults.font.size = 12;
        Chart.defaults.color = '#64748B';
        Chart.defaults.plugins.legend.labels.usePointStyle = true;
        Chart.defaults.plugins.legend.labels.boxWidth = 8;
        Chart.defaults.plugins.legend.labels.padding = 14;
        Chart.defaults.plugins.tooltip.backgroundColor = 'rgba(15,23,42,.92)';
        Chart.defaults.plugins.tooltip.padding = 10;
        Chart.defaults.plugins.tooltip.cornerRadius = 8;
        Chart.defaults.plugins.tooltip.titleFont = { weight: '600' };
        Chart.defaults.maintainAspectRatio = false;
    }

    function initDateNav() {
        var picker = document.getElementById('trackerDate');
        if (picker) {
            picker.addEventListener('change', function () {
                if (picker.value) window.location.href = '/Tracker?date=' + encodeURIComponent(picker.value);
            });
        }
    }

    function initRangeForm() {
        var range = document.getElementById('rangeSelect');
        var custom = document.getElementById('customRange');
        if (!range) return;
        var sync = function () { if (custom) custom.style.display = range.value === 'custom' ? '' : 'none'; };
        range.addEventListener('change', sync);
        sync();
    }

    document.addEventListener('DOMContentLoaded', function () {
        initSidebar();
        initConfirm();
        initAlerts();
        initPasswordToggle();
        initHabitForm();
        initTracker();
        initDateNav();
        initRangeForm();
    });
})();
