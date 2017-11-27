using Apex.MVVM;

namespace ConsoleControlSample.WPF
{
    /// <summary>
    /// The New Process ViewModel.
    /// </summary>
    public class NewProcessViewModel : ViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewProcessViewModel"/> class.
        /// </summary>
        public NewProcessViewModel()
        {
            AcceptCommand = new Command(() => { });
        }

        /// <summary>
        /// Gets the accept command.
        /// </summary>
        public Command AcceptCommand
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets program file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets program arguments.
        /// </summary>
        public string Args { get; set; }
    }        
}
