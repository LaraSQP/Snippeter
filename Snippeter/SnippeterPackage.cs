using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;

using EnvDTE;
using EnvDTE80;

using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

using Task = System.Threading.Tasks.Task;

namespace Snippeter
{
	[ PackageRegistration( UseManagedResourcesOnly = true, AllowsBackgroundLoading = true ) ]
	[ Guid( SnippeterPackage.PackageGuidString ) ]
	[ ProvideMenuResource( "Menus.ctmenu", 1 ) ]
	public sealed class SnippeterPackage : AsyncPackage
	{
		private static readonly Guid	CommandSet			= new Guid( "98ea3419-d3e9-44f5-915d-932c95dbb026" );
		private const int				CommandId			= 0x0100;
		private const string			PackageGuidString	= "e58ad809-e8fd-4595-bcf6-372c77484b11";

		private DTE2					_dte;



		protected override async Task InitializeAsync( CancellationToken cancellationToken, IProgress<ServiceProgressData> progress )
		{
			if( await GetServiceAsync( typeof( IMenuCommandService ) ) is OleMenuCommandService mcs )
			{
				var menuCommandID	= new CommandID( CommandSet, CommandId );
				var menuItem		= new OleMenuCommand( ExecuteAsync, menuCommandID );
				mcs.AddCommand( menuItem );
			}
		}




		private async void ExecuteAsync( object sender, EventArgs e )
		{
			// Get VS handles
			try
			{
				await JoinableTaskFactory.SwitchToMainThreadAsync( DisposalToken );
			}
			catch( OperationCanceledException ex )
			{
				Box.Error( "Unable to SwitchToMainThreadAsync, exception:", ex.Message );
			}

			_dte = await GetServiceAsync( typeof( DTE ) ) as DTE2;

			Assumes.Present( _dte );

			// Get the user's snippet folder
			var userSnippetFolder = GetUserSnippetFolder();

			if( userSnippetFolder == "" )
			{
				// Quit quietly, GetUserSnippetFolder() already reported the problem
				return;
			}

			// Get snippet code
			var codeDoc = null as TextDocument;
			var proceed = ( _dte.ActiveWindow != null
							&& _dte.ActiveWindow.Document != null );

			if( proceed == true )
			{
				codeDoc = _dte.ActiveWindow.Document.Object( string.Empty ) as TextDocument;

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
			var dlg		= new SnippeterWindow( this, snippetCode, userSnippetFolder );
			var hwnd	= new IntPtr( _dte.MainWindow.HWnd );
			var window	= ( System.Windows.Window )HwndSource.FromHwnd( hwnd ).RootVisual;

			dlg.Owner = window;

			try
			{
				bool? result = dlg.ShowDialog();

				if( result.HasValue == true
					&& result.Value == true )
				{
					_dte.StatusBar.Text = "Snippet created. Title = {0} Shortcut = {1}".FormatWith( dlg.SnippetTitle, dlg.SnippetShortcut );

					_dte.StatusBar.Highlight( true );

					await Task.Delay( 5000 );

					_dte.StatusBar.Text = "";
				}
			}
			catch( InvalidOperationException ex )
			{
				Box.Error( "Unable to display Snippeter window, exception:", ex.Message );
			}
		}




		/// <summary>
		/// Gets the user's snippet folder
		/// </summary>
		private string GetUserSnippetFolder()
		{
			var path = "";

			try
			{
				var vsKey = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\VisualStudio\" + _dte.Version );

				if( vsKey != null )
				{
					path = (string)vsKey.GetValue( "VisualStudioLocation", "" );
				}
			}
			catch( Exception ex )
			{
				Box.Error( "Unable to obtain the location of Visual Studio {1} from the registry, exception:".FormatWith( _dte.Version ), ex.Message );
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
