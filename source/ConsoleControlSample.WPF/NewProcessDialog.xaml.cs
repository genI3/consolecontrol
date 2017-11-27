using System.Windows;

namespace ConsoleControlSample.WPF
{
    /// <summary>
    /// Interaction logic for NewProcessDialog.xaml
    /// </summary>
    public partial class NewProcessDialog : Window
    {
        public NewProcessDialog()
        {
            InitializeComponent();

            ViewModel.AcceptCommand.Executed += new Apex.MVVM.CommandEventHandler(AcceptCommand_Executed);
        }

        /// <summary>
        /// Handles the Executed event of the AcceptCommand control. 
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="Apex.MVVM.CommandEventArgs"/> instance containing the event data.</param>
        void AcceptCommand_Executed(object sender, Apex.MVVM.CommandEventArgs args)
        {
            DialogResult = true;
            Close();
        }
    }
}
