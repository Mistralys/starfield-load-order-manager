using System;
using System.Windows;
using LoadOrderKeeper.ViewModels;

namespace LoadOrderKeeper.Views
{
    public partial class ConfirmationDialog : Window
    {
        public ConfirmationDialog()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public ConfirmationDialog(string title, string message, ConfirmationIcon icon = ConfirmationIcon.None, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK)
            : this()
        {
            var viewModel = new ConfirmationDialogViewModel(title, message, icon, buttons, defaultResult);
            DataContext = viewModel;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ConfirmationDialogViewModel oldVm)
            {
                oldVm.DialogResultChanged -= OnDialogResultChanged;
            }

            if (e.NewValue is ConfirmationDialogViewModel newVm)
            {
                newVm.DialogResultChanged += OnDialogResultChanged;
            }
        }

        private void OnDialogResultChanged(object? sender, EventArgs e)
        {
            if (sender is ConfirmationDialogViewModel viewModel)
            {
                DialogResult = viewModel.Result switch
                {
                    ConfirmationResult.OK => true,
                    ConfirmationResult.Yes => true,
                    ConfirmationResult.No => false,
                    ConfirmationResult.Cancel => false,
                    _ => false
                };
                Close();
            }
        }

        public new ConfirmationResult ShowDialog()
        {
            base.ShowDialog();
            return DataContext is ConfirmationDialogViewModel vm ? vm.Result : ConfirmationResult.None;
        }

        public static ConfirmationResult Show(string title, string message, ConfirmationIcon icon = ConfirmationIcon.Information, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK, Window? owner = null)
        {
            var dialog = new ConfirmationDialog(title, message, icon, buttons, defaultResult);
            if (owner != null)
            {
                dialog.Owner = owner;
            }
            return dialog.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is ConfirmationDialogViewModel viewModel)
            {
                viewModel.DialogResultChanged -= OnDialogResultChanged;
            }
            base.OnClosed(e);
        }
    }
}
