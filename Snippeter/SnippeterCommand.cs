using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows.Interop;
using EnvDTE;
using EnvDTE80;
using LaraSPQ.Tools;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace Snippeter
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class SnippeterCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid( "98ea3419-d3e9-44f5-915d-932c95dbb026" );

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private AsyncPackage Package
		{
			get; set;
		}
		private static DTE2 Dte
		{
			get; set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SnippeterCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private SnippeterCommand( AsyncPackage package, OleMenuCommandService commandService )
		{
			Package			= package ?? throw new ArgumentNullException( nameof( package ) );
			commandService	= commandService ?? throw new ArgumentNullException( nameof( commandService ) );

			var menuCommandID	= new CommandID( CommandSet, CommandId );
			var menuItem		= new MenuCommand( this.Execute, menuCommandID );

			commandService.AddCommand( menuItem );
		}




		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static SnippeterCommand Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return Package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <exception cref="OperationCanceledException"></exception>
		public static async Task InitializeAsync( AsyncPackage package, DTE2 dte )
		{
			// Switch to the main thread - the call to AddCommand in Snippeter's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync( package.DisposalToken );

			Dte = dte;

			OleMenuCommandService commandService = await package.GetServiceAsync( typeof( IMenuCommandService ) ) as OleMenuCommandService;

			Instance = new SnippeterCommand( package, commandService );
		}




		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		private void Execute( object sender, EventArgs e )
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Get the user's snippet folder
			var userSnippetFolder = GetUserSnippetFolder();

			if( userSnippetFolder == "" )
			{
				// Quit quietly, GetUserSnippetFolder() already reported the problem
				return;
			}

			// Get snippet code
			var codeDoc = null as TextDocument;
			var proceed = ( Dte.ActiveWindow != null
							&& Dte.ActiveWindow.Document != null );

			if( proceed == true )
			{
				codeDoc = Dte.ActiveWindow.Document.Object( string.Empty ) as TextDocument;

				proceed = ( codeDoc != null );
			}

			var snippetCode = "";

			if( proceed == true )
			{
				try
				{
					snippetCode = codeDoc.Selection.Text.Normalize();
				}
				catch( Exception )
				{
					// Certain property pages claim to be code windows and throw exceptions
					// Consume these quietly and open Snippeter without code (and in manager mode)
					snippetCode = "";
				}
			}

			// If no code has been selected, the extension will not allow adding a code-less snippet (i.e., "Add snippet" button)
			// and will instead function as a snippet manager
			var dlg		= new SnippeterWindow( Package, snippetCode, userSnippetFolder );
			var hwnd	= new IntPtr( Dte.MainWindow.HWnd );
			var window	= ( System.Windows.Window )HwndSource.FromHwnd( hwnd ).RootVisual;

			dlg.Owner = window;

			try
			{
				bool? result = dlg.ShowDialog();

				if( result.HasValue == true
					&& result.Value == true )
				{
					if( dlg.OpenSnippetInIDE == true )
					{
						Dte.StatusBar.Text = "Opening snippet file...";

						VsShellUtilities.OpenDocument( Package, dlg.SnippetPath );

						Dte.StatusBar.Text = "";
					}
					else
					{
						var text = "Snippet created. Title = {0} Shortcut = {1}".FormatWith( dlg.tbTitle.Text.Trim(),
																							 dlg.tbShortcut.Text.Trim() );

						_ = StatusBarWithDelayAsync( text, 5000 );
					}
				}
			}
			catch( InvalidOperationException ex )
			{
				Box.Error( "Unable to display Snippeter window, exception:", ex.Message );
			}
		}




		/// <summary>
		/// SendWithDelay
		/// </summary>
		/// <param name="title"></param>
		/// <param name="shortcut"></param>
		/// <returns></returns>
		/// <exception cref="OperationCanceledException">Ignore.</exception>
		private async Task StatusBarWithDelayAsync( string text, int delayMs )
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			Dte.StatusBar.Text = text;

			Dte.StatusBar.Highlight( true );

			await Task.Delay( delayMs );

			Dte.StatusBar.Text = "";
		}




		/// <summary>
		/// Gets the user's snippet folder
		/// </summary>
		private string GetUserSnippetFolder()
		{
			var path = "";

			try
			{
				var vsKey = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\VisualStudio\" + Dte.Version );

				if( vsKey != null )
				{
					path = (string)vsKey.GetValue( "VisualStudioLocation", "" );
				}
			}
			catch( Exception ex )
			{
				Box.Error( "Unable to obtain the location of Visual Studio {1} from the registry, exception:".FormatWith( Dte.Version ), ex.Message );
				path = "";
			}

			if( path.IsNullOrWhitespace() == true )
			{
				return "";
			}
			else
			{
				return Path.Combine( path, @"Code Snippets\Visual C#\My Code Snippets\" );
			}
		}
	}
}
