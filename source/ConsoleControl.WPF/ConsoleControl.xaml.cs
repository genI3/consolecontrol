using ConsoleControlAPI;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ConsoleControl.WPF
{
    /// <summary>
    /// Interaction logic for ConsoleControl.xaml
    /// </summary>
    public partial class ConsoleControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleControl"/> class.
        /// </summary>
        public ConsoleControl()
        {
            InitializeComponent();

            //  Handle process events.
            processInterface.OnProcessOutput += processInterface_OnProcessOutput;
            processInterface.OnProcessError += processInterface_OnProcessError;
            processInterface.OnProcessInput += processInterface_OnProcessInput;
            processInterface.OnProcessExit += processInterface_OnProcessExit;

            // Handle paste event
            DataObject.AddPastingHandler(richTextBoxConsole, PasteCommand);
        }

        /// <summary>
        /// Handles the OnProcessError event of the processInterface control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        void processInterface_OnProcessError(object sender, ProcessEventArgs args)
        {
            //  Write the output, in red
            WriteOutput(args.Content, Colors.Red);

            //  Fire the output event.
            FireProcessOutputEvent(args);
        }

        /// <summary>
        /// Handles the OnProcessOutput event of the processInterface control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        void processInterface_OnProcessOutput(object sender, ProcessEventArgs args)
        {
            //  Write the output, in white
            WriteOutput(args.Content, Colors.White);

            //  Fire the output event.
            FireProcessOutputEvent(args);
        }

        /// <summary>
        /// Handles the OnProcessInput event of the processInterface control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        void processInterface_OnProcessInput(object sender, ProcessEventArgs args)
        {
            FireProcessInputEvent(args);
        }

        /// <summary>
        /// Handles the OnProcessExit event of the processInterface control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        void processInterface_OnProcessExit(object sender, ProcessEventArgs args)
        {
            //  Read only again.
            RunOnUIDespatcher(() =>
            {
                //  Are we showing diagnostics?
                if (ShowDiagnostics)
                {
                    WriteOutput(Environment.NewLine + processInterface.ProcessFileName + " exited.", Color.FromArgb(255, 0, 255, 0));
                }

                richTextBoxConsole.IsReadOnly = true;

                //  And we're no longer running.
                IsProcessRunning = false;
            });
        }

        /// <summary>
        /// Handles the KeyDown event of the richTextBoxConsole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs" /> instance containing the event data.</param>
        void richTextBoxConsole_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsProcessRunning)
            {                                                             
                GetSelectionIndexes(out var startIndex, out var endIndex);

                //  If we're at the input point and it's backspace                
                if (e.Key == Key.Back)
                    if (startIndex < 0  || startIndex > inputTextBuilder.Length ||
                        endIndex   < 0  || endIndex   > inputTextBuilder.Length ||
                        (startIndex == endIndex && startIndex == 0)             ||
                        inputTextBuilder.Length == 0)
                    {
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        if (startIndex == endIndex)
                            inputTextBuilder.Remove(startIndex - 1, 1);
                        else if (startIndex > endIndex)
                            inputTextBuilder.Remove(endIndex, startIndex - endIndex);
                        else
                            inputTextBuilder.Remove(startIndex, endIndex - startIndex);

                        return;
                    }

                //  If we're at the input point and it's delete
                if (e.Key == Key.Delete)
                    if (startIndex < 0 || startIndex > inputTextBuilder.Length ||
                        endIndex   < 0 || endIndex   > inputTextBuilder.Length ||
                        (startIndex == endIndex && 
                         startIndex == inputTextBuilder.Length)                ||
                        inputTextBuilder.Length == 0)
                    {
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        if (startIndex == endIndex)
                            inputTextBuilder.Remove(startIndex, 1);
                        else if (startIndex > endIndex)
                            inputTextBuilder.Remove(endIndex, startIndex - endIndex);
                        else
                            inputTextBuilder.Remove(startIndex, endIndex - startIndex);

                        return;
                    }

                //  If we're at the input point and it's space
                if (e.Key == Key.Space)
                    if (startIndex < 0 || endIndex < 0 ||
                        startIndex > inputTextBuilder.Length ||
                        endIndex   > inputTextBuilder.Length)
                    {
                        e.Handled = true;
                        return;
                    }
                    else
                    {
                        InsertTextIntoSelection(startIndex, endIndex, " ");

                        return;
                    }

                //  Are we in the read-only zone?
                if (startIndex < 0 || endIndex < 0)
                {
                    //  Allow arrows and Ctrl+C.
                    if (!(e.Key == Key.Left ||
                        e.Key == Key.Right ||
                        e.Key == Key.Up ||
                        e.Key == Key.Down ||
                        (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))))
                    {
                        e.Handled = true;
                        return;
                    }
                }

                // Allow Ctrl+X
                if(e.Key == Key.X && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {                
                    if (startIndex < 0 || endIndex < 0 ||
                        startIndex == endIndex)
                    {
                        e.Handled = true;
                        return;
                    }                                                                                

                    InsertTextIntoSelection(startIndex, endIndex, string.Empty);

                    return;
                }

                // Disallow Ctrl+Z
                if (e.Key == Key.Z && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    e.Handled = true;
                    return;
                }

                //  Is it the return key?
                if (e.Key == Key.Return)
                {
                    // Setting the caret to the end of the input to ensure
                    // that the input text is not broken into lines at view
                    richTextBoxConsole.CaretPosition = richTextBoxConsole.Document.ContentEnd;

                    //  Get the input.
                    var input = inputTextBuilder.ToString();

                    //  Write the input (without echoing).
                    WriteInput(input, Colors.White, false);

                    // Clear inputed text
                    inputTextBuilder.Clear();
                }
            }
        }

        /// <summary>
        /// Handles the PreviewTextInput event of the richTextBoxConsole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.TextCompositionEventArgs" /> instance containing the event data.</param>
        void richTextBoxConsole_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {   
            if (IsProcessRunning)
            {
                GetSelectionIndexes(out var startIndex, out var endIndex);

                // In read-only zone?
                if (startIndex < 0 || endIndex < 0 ||
                    startIndex > inputTextBuilder.Length ||
                    endIndex   > inputTextBuilder.Length)
                    return;

                InsertTextIntoSelection(startIndex, endIndex, e.Text);
            }
        }

        /// <summary>
        /// Handles the Paste event of the richTextBoxConsole control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DataObjectPastingEventArgs" /> instance containing the event data.</param>
        void PasteCommand(object sender, DataObjectPastingEventArgs e)
        {                     
            if (IsProcessRunning)
            {
                GetSelectionIndexes(out var startIndex, out var endIndex);

                // In read-only zone?
                if (startIndex < 0 || endIndex < 0 ||
                    startIndex > inputTextBuilder.Length ||
                    endIndex   > inputTextBuilder.Length)
                    return;

                var insertedText = (string)e.DataObject.GetData(typeof(string));

                if (string.IsNullOrEmpty(insertedText))
                    return;

                InsertTextIntoSelection(startIndex, endIndex, insertedText);

                var selection = new TextRange(richTextBoxConsole.Selection.Start, 
                                              richTextBoxConsole.Selection.End);

                selection.Text = insertedText;
                selection.ApplyPropertyValue(TextElement.ForegroundProperty, 
                                             new SolidColorBrush(Colors.White));
                                                                                    
                inputStartPos = richTextBoxConsole.Document.ContentEnd.GetPositionAtOffset(-2);

                // Setting the caret to the end of the inserted text
                if (startIndex > endIndex)
                    richTextBoxConsole.CaretPosition = richTextBoxConsole.Selection.Start;
                else
                    richTextBoxConsole.CaretPosition = richTextBoxConsole.Selection.End;

                e.CancelCommand();
            }
        }

        /// <summary>
        /// Returns the current start and end indexes of the current 
        /// selection relative to the <see cref="inputTextBuilder"/>.
        /// </summary>
        /// <param name="startIndex">The character index to the right of the selection start.</param>
        /// <param name="endIndex">The character index to the right of the selection end.</param>
        void GetSelectionIndexes(out int startIndex, out int endIndex)
        {
            var startOffset = richTextBoxConsole.Selection.Start.GetOffsetToPosition(inputStartPos);

            var endOffset = richTextBoxConsole.Selection.End.GetOffsetToPosition(inputStartPos);
                                                                                         
            startIndex = inputTextBuilder.Length - startOffset;

            endIndex = inputTextBuilder.Length - endOffset;      
        }

        /// <summary>
        /// Inserts <paramref name="text"/> into the <see cref="inputTextBuilder"/> according
        /// to the <paramref name="start"/> and the <paramref name="end"/> indexes.  
        /// </summary>
        /// <param name="start">The start index of the <see cref="inputTextBuilder"/>.</param>
        /// <param name="end">The end index of the <see cref="inputTextBuilder"/>.</param>
        /// <param name="text">Inserted text.</param>
        void InsertTextIntoSelection(int start, int end, string text)
        {
            int startIndex = 0;

            if (start == end)
                startIndex = end;
            else if (start < end)
            {
                inputTextBuilder.Remove(start, end - start);
                startIndex = start;
            }
            else
            {
                inputTextBuilder.Remove(end, start - end);
                startIndex = end;                
            }

            if (inputTextBuilder.Length <= startIndex)
                inputTextBuilder.Append(text);
            else
                inputTextBuilder.Insert(startIndex, text);
        }

        /// <summary>
        /// Writes the output to the console control.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="color">The color.</param>
        public void WriteOutput(string output, Color color)
        {
            if (string.IsNullOrEmpty(lastInput) == false &&
                (output == lastInput || output.Replace("\r\n", "") == lastInput) ||
                IsMute)
                return;

            RunOnUIDespatcher(() =>
            {
                //  Write the output.
                var range = new TextRange(richTextBoxConsole.Document.ContentEnd, richTextBoxConsole.Document.ContentEnd);
                range.Text = output;
                range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));

                //  Record the new input start.
                inputStartPos = richTextBoxConsole.Document.ContentEnd.GetPositionAtOffset(-2);

                // Setting the caret to the end for entering new commands
                richTextBoxConsole.CaretPosition = richTextBoxConsole.Document.ContentEnd;
                richTextBoxConsole.ScrollToEnd();
            });
        }

        /// <summary>
        /// Clears the output.
        /// </summary>
        public void ClearOutput()
        {
            richTextBoxConsole.Document.Blocks.Clear();
            inputTextBuilder.Clear();

            // Send standard clear screen command
            if (IsProcessRunning)
                WriteInput("cls", Colors.White, false);
        }

        /// <summary>
        /// Writes the input to the console control.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="color">The color.</param>
        /// <param name="echo">if set to <c>true</c> echo the input.</param>
        public void WriteInput(string input, Color color, bool echo)
        {
            RunOnUIDespatcher(() =>
            {
                //  Are we echoing?
                if (echo)
                {
                    richTextBoxConsole.Selection.ApplyPropertyValue(TextBlock.ForegroundProperty, new SolidColorBrush(color));
                    richTextBoxConsole.AppendText(input);
                    inputStartPos = richTextBoxConsole.Selection.Start;
                }

                lastInput = input;

                //  Write the input.
                processInterface.WriteInput(input);

                //  Fire the event.
                FireProcessInputEvent(new ProcessEventArgs(input));
            });
        }

        /// <summary>
        /// Runs the on UI despatcher.
        /// </summary>
        /// <param name="action">The action.</param>
        private void RunOnUIDespatcher(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                //  Invoke the action.
                action();
            }
            else
            {
                Dispatcher.BeginInvoke(action, null);
            }
        }


        /// <summary>
        /// Runs a process.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        public void StartProcess(string fileName, string arguments)
        {
            ClearOutput();

            //  Are we showing diagnostics?
            if (ShowDiagnostics)
            {
                WriteOutput("Preparing to run " + fileName, Color.FromArgb(255, 0, 255, 0));
                if (!string.IsNullOrEmpty(arguments))
                    WriteOutput(" with arguments " + arguments + "." + Environment.NewLine, Color.FromArgb(255, 0, 255, 0));
                else
                    WriteOutput("." + Environment.NewLine, Color.FromArgb(255, 0, 255, 0));
            }

            //  Start the process.
            processInterface.StartProcess(fileName, arguments);

            RunOnUIDespatcher(() =>
            {
                //  If we enable input, make the control not read only.
                if (IsInputEnabled)
                    richTextBoxConsole.IsReadOnly = false;
                else
                    richTextBoxConsole.IsReadOnly = true;

                //  We're now running.
                IsProcessRunning = true;
            });
        }

        /// <summary>
        /// Stops the process.
        /// </summary>
        public void StopProcess()
        {
            //  Stop the interface.
            processInterface.StopProcess();
        }

        /// <summary>
        /// Fires the console output event.
        /// </summary>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        private void FireProcessOutputEvent(ProcessEventArgs args)
        {
            //  Get the event.
            var theEvent = OnProcessOutput;
            if (theEvent != null)
                theEvent(this, args);
        }

        /// <summary>
        /// Fires the console input event.
        /// </summary>
        /// <param name="args">The <see cref="ProcessEventArgs"/> instance containing the event data.</param>
        private void FireProcessInputEvent(ProcessEventArgs args)
        {
            //  Get the event.
            var theEvent = OnProcessInput;
            if (theEvent != null)
                theEvent(this, args);
        }

        /// <summary>
        /// The internal process interface used to interface with the process.
        /// </summary>
        private readonly ProcessInterface processInterface = new ProcessInterface();

        /// <summary>
        /// Current position that input starts at.
        /// </summary>
        private TextPointer inputStartPos;
        

        /// <summary>
        ///  Current input text 
        /// </summary>
        private StringBuilder inputTextBuilder = new StringBuilder();

        /// <summary>
        /// The last input string (used so that we can make sure we don't echo input twice).
        /// </summary>
        private string lastInput;
        
        /// <summary>
        /// Occurs when console output is produced.
        /// </summary>
        public event ProcessEventHanlder OnProcessOutput;

        /// <summary>
        /// Occurs when console input is produced.
        /// </summary>
        public event ProcessEventHanlder OnProcessInput;
          
        private static readonly DependencyProperty ShowDiagnosticsProperty = 
          DependencyProperty.Register("ShowDiagnostics", typeof(bool), typeof(ConsoleControl),
          new PropertyMetadata(false, OnShowDiagnosticsChanged));

        /// <summary>
        /// Gets or sets a value indicating whether to show diagnostics.
        /// </summary>
        /// <value>
        ///   <c>true</c> if show diagnostics; otherwise, <c>false</c>.
        /// </value>
        public bool ShowDiagnostics
        {
            get { return (bool)GetValue(ShowDiagnosticsProperty); }
            set { SetValue(ShowDiagnosticsProperty, value); }
        }

        private static void OnShowDiagnosticsChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
        }


        private static readonly DependencyProperty IsInputEnabledProperty =
          DependencyProperty.Register("IsInputEnabled", typeof(bool), typeof(ConsoleControl),
          new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether this instance has input enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has input enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsInputEnabled
        {
            get { return (bool)GetValue(IsInputEnabledProperty); }
            set { SetValue(IsInputEnabledProperty, value); }
        }

        internal static readonly DependencyPropertyKey IsProcessRunningPropertyKey =
          DependencyProperty.RegisterReadOnly("IsProcessRunning", typeof(bool), typeof(ConsoleControl),
          new PropertyMetadata(false));

        private static readonly DependencyProperty IsProcessRunningProperty = IsProcessRunningPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether this instance has a process running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has a process running; otherwise, <c>false</c>.
        /// </value>
        public bool IsProcessRunning
        {
            get { return (bool)GetValue(IsProcessRunningProperty); }
            private set { SetValue(IsProcessRunningPropertyKey, value); }
        }

        /// <summary>
        /// Gets the internally used process interface.
        /// </summary>
        /// <value>
        /// The process interface.
        /// </value>
        public ProcessInterface ProcessInterface
        {
            get { return processInterface; }
        }

        /// <summary>
        /// Gets or sets code page of internally used process interface.
        /// </summary>
        /// <value>
        /// The process interface code page.
        /// </value>                                  
        public int CodePage
        {
            get { return (int)GetValue(CodePageProperty); }
            set { SetValue(CodePageProperty, value); }
        }              

        public static readonly DependencyProperty CodePageProperty =
          DependencyProperty.Register("CodePage", typeof(int), typeof(ConsoleControl),
          new PropertyMetadata(Console.OutputEncoding.CodePage, new PropertyChangedCallback(OnCodePageChanged))); 
                       
        private static void OnCodePageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as ConsoleControl;
            
            instance.ProcessInterface.OutputEncoding = System.Text.Encoding.GetEncoding((int)e.NewValue);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this 
        /// instance is write the output text to view.
        /// </summary>
        /// <value>
        /// <c>true</c> if writing the output text to 
        /// the view are disabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsMute
        {
            get { return (bool)GetValue(IsMuteProperty); }
            set { SetValue(IsMuteProperty, value); }
        }
                                                                                                                         
        public static readonly DependencyProperty IsMuteProperty =
            DependencyProperty.Register("IsMute", typeof(bool), typeof(ConsoleControl), new PropertyMetadata(false)); 
    }
}
