using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;

using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;

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

		private DTE2					Dte;



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

			Dte = await GetServiceAsync( typeof( DTE ) ) as DTE2;

			Assumes.Present( Dte );

			// Get snippet
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
				catch( Exception ex )
				{
					Box.Error( "Failed to fetch text, exception:", ex.Message,
							   "Carry on, this is a non-critical error." );
					return;
				}
			}

			// If no code has been selected, the extension will not allow adding a code-less snippet (i.e., "Add snippet" button)
			// and will instead function as a snippet manager
			var dlg		= new SnippeterWindow( this, snippetCode, Dte.Version );
			var hwnd	= new IntPtr( Dte.MainWindow.HWnd );
			var window	= ( System.Windows.Window )HwndSource.FromHwnd( hwnd ).RootVisual;

			dlg.Owner = window;

			try
			{
				bool? result = dlg.ShowDialog();

				if( result.HasValue == true
					&& result.Value == true )
				{
					Dte.StatusBar.Text = "Snippet created. Title = {0} Shortcut = {1}".FormatWith( dlg.SnippetTitle, dlg.SnippetShortcut );
					Dte.StatusBar.Highlight( true );

					await Task.Delay( 5000 );

					Dte.StatusBar.Text = "";
				}
			}
			catch( InvalidOperationException ex )
			{
				Box.Error( "Unable to display Snippeter window, exception:", ex.Message );
			}
		}
	}
}
