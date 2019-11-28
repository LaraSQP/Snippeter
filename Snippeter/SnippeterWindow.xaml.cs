using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.XPath;

using ICSharpCode.AvalonEdit;

using Microsoft.VisualStudio.Shell;

namespace Snippeter
{
	/// <summary>
	/// Interaction logic for DetailsDialog.xaml
	/// </summary>
	public partial class SnippeterWindow : Window
	{
		// Properties
		internal string SnippetTitle
		{
			get { return tbTitle.Text.Trim(); }
		}
		internal string SnippetShortcut
		{
			get { return tbShortcut.Text.Trim(); }
		}
		private SnippeterPackage SnippeterPackage
		{
			get; set;
		}
		private string UserSnippetPath
		{
			get; set;
		}

		// Controls
		private readonly TextEditor Avalon = new TextEditor();

		// Data
		private readonly SortedDictionary<string, Item> _snippets = new SortedDictionary<string, Item>();
		private class Item
		{
			public string Path
			{
				get; set;
			}
			public string Title
			{
				get; set;
			}
			public string Shortcut
			{
				get; set;
			}
			public string Description
			{
				get; set;
			}
			public string Code
			{
				get; set;
			}
		}



		/// <summary>
		/// Creates the window and loads either the editor or the manager
		/// </summary>
		internal SnippeterWindow( SnippeterPackage snippeterPackage, string snippetCode, string userSnippetPath )
		{
			InitializeComponent();

			lvSnippets.MouseUp += lvSnippets_MouseUp;

			// Store extension basic data
			SnippeterPackage	= snippeterPackage;
			UserSnippetPath		= userSnippetPath;

			// Hide/disable minimize button
			WindowExtensions.HideDisableMinimizeButton( this );

			// Restore window size and position, if any
			if( Properties.Settings.Default.WindowHeight != -1 )
			{
				WindowStartupLocation	= WindowStartupLocation.Manual;
				SizeToContent			= SizeToContent.Manual;
				Top						= Properties.Settings.Default.WindowTop;
				Left					= Properties.Settings.Default.WindowLeft;
				Width					= Properties.Settings.Default.WindowHeight;
				Height					= Properties.Settings.Default.WindowWidth;
				WindowState				= Properties.Settings.Default.WindowState;
			}

			// Set the extension to act as creator of a new snippet or as manager of existing ones
			var addSnippet = ( snippetCode.IsNullOrWhitespace() == false );

			btAdd.Visibility		= ( addSnippet == false )? Visibility.Hidden : Visibility.Visible;
			laModify.Visibility		= ( addSnippet == false )? Visibility.Visible : Visibility.Hidden;
			btUpdate.Visibility		= Visibility.Hidden;
			btOpen.Visibility		= Visibility.Hidden;
			btDismiss.Visibility	= Visibility.Hidden;

			if( addSnippet == true )
			{
				// New snippet mode
				RequiredLabels( true );

				ShowEditor( snippetCode );
			}
		}




		/// <summary>
		/// Called when Snippeter's window has loaded
		/// </summary>
		private void OnLoad( object sender, RoutedEventArgs e )
		{
			if( lvSnippets.Visibility == Visibility.Visible )
			{
				// Manager mode, thus load available snippets
				try
				{
					var paths = UserSnippetPath.GetFilesInFolder( "*.snippet" );

					LoadSnippets( paths );
				}
				catch( Exception ex )
				{
					Box.Error( "Unable to obtain snippets from the user's snippet directory and, thus, will not run Snippeter.",
							   "Exception: ", ex.Message );

					Close();
				}

				lvSnippets.ItemsSource = _snippets.Values;
			}
		}




		/// <summary>
		/// Loads snippets from the user's snippet directory while checking for their integrity and reporting broken ones, if any
		/// </summary>
		private void LoadSnippets( List<string> paths )
		{
			var xmlDocument = new XmlDocument();
			var failedLoads = new List<string>();

			foreach( var path in paths )
			{
				try
				{
					xmlDocument.Load( path );
				}
				catch( Exception )
				{
					failedLoads.Add( path );
					continue;
				}

				var xmlns	= xmlDocument.DocumentElement.NamespaceURI;
				var nsMgr	= null as XmlNamespaceManager;

				try
				{
					nsMgr = new XmlNamespaceManager( xmlDocument.NameTable );
				}
				catch( NullReferenceException )
				{
					failedLoads.Add( path );
					continue;
				}

				nsMgr.AddNamespace( "ns1", xmlns );

				var item = new Item();

				try
				{
					item.Title			= xmlDocument.SelectSingleNode( "//ns1:Header/ns1:Title", nsMgr )?.InnerText.Trim().Unescape();
					item.Shortcut		= xmlDocument.SelectSingleNode( "//ns1:Header/ns1:Shortcut", nsMgr ).InnerText.Trim().Unescape();
					item.Code			= xmlDocument.SelectSingleNode( "//ns1:Snippet/ns1:Code", nsMgr ).InnerText.Trim();
					item.Description	= xmlDocument.SelectSingleNode( "//ns1:Header/ns1:Description", nsMgr ).InnerText.Trim().Unescape();
					item.Path			= path;
				}
				catch( XPathException )
				{
					failedLoads.Add( path );
					continue;
				}

				_snippets.Add( path, item );
			}

			if( failedLoads.Count > 0 )
			{
				// Copy full paths to the clipboard
				failedLoads.Join( Box.NL ).CopyToClipboard();

				// Create report
				var ls = new List<string>
				{
					"The following snippets failed to load:",
					failedLoads.Select( x => Path.GetFileName( x ) ).Join( Box.NL ),
					"Check their integrity manually.",
					"Their full paths have been copied to the clipboard.",
					"Do you want to open them now in the IDE?",
					"(If 'yes' Snippeter will close, if 'no' Snippeter will open without those bad snippets)",
				};

				if( Box.Question( ls.ToArray() ) == MessageBoxResult.Yes )
				{
					foreach( var path in failedLoads )
					{
						VsShellUtilities.OpenDocument( SnippeterPackage, path );
					}

					Close();
				}
			}
		}




		/// <summary>
		/// Flags the color of the snippet attributes that are required
		/// </summary>
		private void RequiredLabels( bool required )
		{
			laTitle.Foreground		= ( required == true )? Brushes.DarkRed : laDescription.Foreground;
			laShortcut.Foreground	= ( required == true )? Brushes.DarkRed : laDescription.Foreground;
			laTitle.FontWeight		= ( required == true )? FontWeights.Bold : laDescription.FontWeight;
			laShortcut.FontWeight	= ( required == true )? FontWeights.Bold : laDescription.FontWeight;
		}




		/// <summary>
		/// Displays the editor with the snippet's code
		/// </summary>
		private void ShowEditor( string snippetCode )
		{
			// No need to do this more than once
			if( Avalon.BorderBrush != Brushes.DarkRed )
			{
				Avalon.BorderBrush						= Brushes.DarkRed;
				Avalon.BorderThickness					= new Thickness( 1 );
				Avalon.Margin							= new Thickness( 4 );
				Avalon.Padding							= new Thickness( 4 );
				Avalon.WordWrap							= false;
				Avalon.VerticalScrollBarVisibility		= ScrollBarVisibility.Auto;
				Avalon.HorizontalScrollBarVisibility	= ScrollBarVisibility.Auto;
				Avalon.SyntaxHighlighting				= ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition( "C#" );
			}

			Avalon.Text = snippetCode;

			// Swap controls
			Avalon.Visibility = Visibility.Visible;

			grid.Children.Remove( lvSnippets );
			grid.Children.Add( Avalon );
			Grid.SetRow( Avalon, 4 );
			Grid.SetColumn( Avalon, 0 );
			Grid.SetColumnSpan( Avalon, 2 );

			lvSnippets.Visibility = Visibility.Hidden;
		}




		/// <summary>
		/// Hides the listview and shows the editor
		/// </summary>
		private void ShowListview()
		{
			// Swap controls
			lvSnippets.Visibility = Visibility.Visible;

			grid.Children.Remove( Avalon );
			grid.Children.Add( lvSnippets );
			Grid.SetRow( Avalon, 4 );
			Grid.SetColumn( Avalon, 0 );
			Grid.SetColumnSpan( Avalon, 2 );

			Avalon.Visibility = Visibility.Hidden;
		}




		/// <summary>
		/// Saves window size and position
		/// </summary>
		protected override void OnClosing( CancelEventArgs e )
		{
			base.OnClosing( e );

			Properties.Settings.Default.WindowTop		= Top;
			Properties.Settings.Default.WindowLeft		= Left;
			Properties.Settings.Default.WindowHeight	= Width;
			Properties.Settings.Default.WindowWidth		= Height;
			Properties.Settings.Default.WindowState		= WindowState;
		}




		/// <summary>
		/// Deletes the currently selected item in the listview and its corresponding snippet file
		/// </summary>
		private void lvSnippets_PreviewKeyDown( object sender, KeyEventArgs e )
		{
			if( e.Key == Key.Delete )
			{
				var item = (Item)lvSnippets.SelectedItem;

				if( item == null )
				{
					return;
				}

				if( Box.Question( "Are you sure you want to delete this snippet?",
								  "Title:", item.Title,
								  "Shortcut:", item.Shortcut,
								  "Path:", item.Path,
								  "This is a non-reversible operation." ) == MessageBoxResult.Yes )
				{
					try
					{
						File.Delete( item.Path );
					}
					catch( Exception ex )
					{
						Box.Error( "Unable to delete snippet file, exception:", ex.Message );
						return;
					}

					// Remove from source, listview, and clear attribute fields
					_snippets.Remove( item.Path );

					lvSnippets.Items.Refresh();

					tbTitle.Text = tbDescription.Text = tbShortcut.Text = "";
				}
			}
		}




		private void lvSnippets_MouseUp( object sender, MouseButtonEventArgs e )
		{
			if( e.ChangedButton == MouseButton.Right )
			{
				var item = (Item)lvSnippets.SelectedItem;

				if( item != null )
				{
					if( item.Path.CopyToClipboard() == true )
					{
						Box.Info( "The path of snippet {0} has been copied to the clipboard.".FormatWith( item.Title ),
								  item.Path );
					}
				}
			}
		}




		/// <summary>
		/// Overrides the ESC key to quickly dismiss the editor or the manager
		/// </summary>
		protected override void OnPreviewKeyDown( KeyEventArgs e )
		{
			base.OnPreviewKeyDown( e );

			if( e.Key == Key.Escape )
			{
				if( lvSnippets.Visibility == Visibility.Hidden
					&& lvSnippets.Items.Count == 0 )
				{
					// Quit quietly
					Close();
				}
				else
				{
					// Discard any changes
					DismissEditing();
				}
			}
		}




		/// <summary>
		/// Prepares the selected snippet for modification
		/// </summary>
		private void lvSnippets_MouseDoubleClick( object sender, MouseButtonEventArgs e )
		{
			var item = (Item)lvSnippets.SelectedItem;

			if( item == null )
			{
				return;
			}

			// Toggle controls' visibility
			laModify.Visibility		= Visibility.Hidden;
			btAdd.Visibility		= Visibility.Hidden;
			btUpdate.Visibility		= Visibility.Visible;
			btOpen.Visibility		= Visibility.Visible;
			btDismiss.Visibility	= Visibility.Visible;

			// Show snippet
			tbTitle.Text		= item.Title;
			tbShortcut.Text		= item.Shortcut;
			tbDescription.Text	= item.Description;

			tbTitle.Focus();

			// Mark required fields
			RequiredLabels( true );

			ShowEditor( item.Code );
		}




		/// <summary>
		/// Dismiss currently selected snippet
		/// </summary>
		private void btDismiss_Click( object sender, RoutedEventArgs e )
		{
			DismissEditing();
		}




		/// <summary>
		/// Hide editing controls and clear snippet attribute fields
		/// </summary>
		private void DismissEditing()
		{
			// Toggle controls' visibility
			btUpdate.Visibility		= Visibility.Hidden;
			btOpen.Visibility		= Visibility.Hidden;
			btDismiss.Visibility	= Visibility.Hidden;
			laModify.Visibility		= Visibility.Visible;

			RequiredLabels( false );

			// Clear attribute fields
			tbTitle.Text = tbDescription.Text = tbShortcut.Text = "";

			// Done, show snippet manager
			ShowListview();
		}




		/// <summary>
		/// Updates the selected snippet
		/// </summary>
		private void btUpdate_Click( object sender, RoutedEventArgs e )
		{
			// Fetches snippet's data
			var title		= tbTitle.Text.Trim();
			var shortcut	= tbShortcut.Text.Trim();
			var description = tbDescription.Text.Trim();
			var code		= Avalon.Text;

			if( title == ""
				|| shortcut == ""
				|| code.Trim() == "" )
			{
				Box.Error( "A snippet must have a name, a shortcut, and some code." );
				return;
			}

			// Toggle controls' visibility
			btUpdate.Visibility		= Visibility.Hidden;
			btOpen.Visibility		= Visibility.Hidden;
			btDismiss.Visibility	= Visibility.Hidden;
			laModify.Visibility		= Visibility.Visible;

			// Mark required fields
			RequiredLabels( false );

			// Get the selected item
			var item = (Item)lvSnippets.SelectedItem;

			if( item == null )
			{
				return;
			}

			if( item.Title == title
				&& item.Shortcut == shortcut
				&& item.Description == description
				&& item.Code == code )
			{
				// Nothing to update
				Box.Info( "Nothing to update." );
				goto skipListViewUpdate;
			}

			// Update the snippet file, retaining the same file name if the title has not changed
			// If it has, a new file will be created and the old snippet file will be deleted a few lines below
			if( SaveSnippetToFile( title,
								   shortcut,
								   description,
								   code,
								   out string fullpath,
								   useThisPath : ( item.Title == title )? item.Path : "" ) == false )
			{
				// There was some problem and it was reported by the method, move on
				goto skipListViewUpdate;
			}

			// Update the listview
			var snippet = _snippets[ item.Path ];

			snippet.Shortcut	= shortcut;
			snippet.Description = description;
			snippet.Code		= code;

			if( item.Title != title )
			{
				// The snippet title has changed and, consequently so has its filename, which SaveSnippetToFile has created
				// Delete the old file and update the item
				try
				{
					// Delete the old file
					File.Delete( item.Path );
				}
				catch( Exception ex )
				{
					Box.Error( "Unable to delete the old snippet file {0}, exception: {1}".FormatWith( item.Path, ex.Message ) );
				}

				// Update the item
				snippet.Title = title;

				_snippets.Remove( item.Path );

				snippet.Path = fullpath;

				_snippets[ fullpath ] = snippet;
			}

			lvSnippets.Items.Refresh();

		skipListViewUpdate:
			tbTitle.Text = tbDescription.Text = tbShortcut.Text = "";

			// Done, show snippet manager
			ShowListview();
		}




		/// <summary>
		/// Opens snippet file in VS and closes window
		/// </summary>
		private void btOpen_Click( object sender, RoutedEventArgs e )
		{
			var item = (Item)lvSnippets.SelectedItem;

			if( item == null )
			{
				return;
			}

			if( Box.Question( "The XML snippet file will open in the editor for you to make changes directly (at your own risk).",
							  "Are you sure?" ) != MessageBoxResult.Yes )
			{
				return;
			}

			VsShellUtilities.OpenDocument( SnippeterPackage, item.Path );

			Close();
		}




		/// <summary>
		/// Creates snippet file and saves it
		/// </summary>
		private void btAdd_Click( object sender, RoutedEventArgs e )
		{
			var title		= tbTitle.Text.Trim();
			var shortcut	= tbShortcut.Text.Trim();
			var description = tbDescription.Text.Trim();
			var code		= Avalon.Text;

			if( title == ""
				|| shortcut == ""
				|| code.Trim() == "" )
			{
				Box.Error( "A snippet must have a name, a shortcut, and some code." );
				return;
			}

			if( title.IsFilenameValid() == false )
			{
				Box.Error( "The title of the snippet contains invalid characters.",
						   "The following are not allowed: " + Path.GetInvalidPathChars() );
				return;
			}

			// Commit to file
			if( SaveSnippetToFile( title, shortcut, description, code, out string fullpath ) == false )
			{
				// There was some problem and it was reported by the method, quit
				return;
			}

			// Report and dismiss
			DialogResult = true;
			Close();
		}




		/// <summary>
		/// Creates the snippet XML and saves it to file
		/// </summary>
		private bool SaveSnippetToFile( string title,
										string shortcut,
										string description,
										string code,
										out string fullpath,
										string useThisPath = "" )
		{
			fullpath = "";

			var fileName	= title;
			var counter		= 2;

			if( useThisPath == "" )
			{
				// Add a numeric tag to the filename if it already exists
				while( File.Exists( Path.Combine( UserSnippetPath, fileName + ".snippet" ) ) == true )
				{
					fileName = "{0} {1}".FormatWith( title, counter++ );
				}
			}

			var snippetXml = TEMPLATE.FormatWith( title.Escape(),
												  description.Escape(),
												  shortcut.Escape(),
												  code );
			var xmlDocument = new XmlDocument();

			try
			{
				xmlDocument.LoadXml( snippetXml );
			}
			catch( XmlException ex )
			{
				Box.Error( "Error creating XML snippet, exception:", ex.Message );
				return false;
			}

			try
			{
				fullpath = Path.Combine( UserSnippetPath, fileName + ".snippet" );

				xmlDocument.Save( ( useThisPath == "" )? fullpath : useThisPath );
			}
			catch( XmlException ex )
			{
				Box.Error( "Error saving XML snippet, exception:", ex.Message );
				return false;
			}

			return true;
		}




		/// <summary>
		///
		/// </summary>
		private const string TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?>
<CodeSnippets xmlns = ""http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet"">
  <CodeSnippet Format=""1.0.0"">
	<Header>
	  <SnippetTypes>
		<SnippetType>Expansion</SnippetType>
	  </SnippetTypes>
	  <Title>{0}</Title>
	  <Author>
	  </Author>
	  <Description>{1}</Description>
	  <HelpUrl>
	  </HelpUrl>
	  <Shortcut>{2}</Shortcut>
	</Header>
	<Snippet>
	  <Code Language = ""csharp"" Delimiter=""$"">
		<![CDATA[{3}]]>
	  </Code>
	</Snippet>
  </CodeSnippet>
</CodeSnippets>";
	};
}
