using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

using ICSharpCode.AvalonEdit;

using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

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
		private string VsVersion
		{
			get; set;
		}
		private string PathSettings
		{
			get; set;
		}

		// Controls
		private readonly TextEditor Avalon = new TextEditor();

		// Data
		private readonly SortedDictionary<string, Item> Snippets = new SortedDictionary<string, Item>();
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
		internal SnippeterWindow( SnippeterPackage snippeterPackage, string snippetCode, string vsVersion )
		{
			InitializeComponent();

			lvSnippets.MouseUp += lvSnippets_MouseUp;

			// Store extension basic data
			SnippeterPackage	= snippeterPackage;
			VsVersion			= vsVersion;

			// Fetch user snippet folder or simply quit for no work can be done without it
			var snippetPath = GetUserSnippetFolder();

			if( snippetPath.IsNullOrWhitespace() == true )
			{
				Box.Error( "Unable to obtain the user's snippet directory." );

				Close();
			}

			// Load settings
			WindowExtensions.HideMinimizeAndMaximizeButtons( this );

			try
			{
				PathSettings = Path.GetFullPath( Path.Combine( snippetPath, @"..\Snippeter.ini" ) );
			}
			catch( Exception ex )
			{
				PathSettings = "";

				Box.Error( "Unable to obtain a valid path to store settings, exception:", ex.Message );
			}

			LoadSettings();

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
			else
			{
				// Manager mode, thus load available snippets
				try
				{
					var paths		= snippetPath.GetFilesInFolder( "*.snippet" );
					var xmlDocument = new XmlDocument();

					foreach( var path in paths )
					{
						xmlDocument.Load( path );

						var xmlns	= xmlDocument.DocumentElement.NamespaceURI;
						var nsMgr	= new XmlNamespaceManager( xmlDocument.NameTable );

						nsMgr.AddNamespace( "ns1", xmlns );

						var item = new Item
						{
							Path		= path,
							Title		= xmlDocument.SelectSingleNode( "//ns1:Header/ns1:Title", nsMgr )?.InnerText.Trim().Unescape(),
							Shortcut	= xmlDocument.SelectSingleNode( "//ns1:Header/ns1:Shortcut", nsMgr ).InnerText.Trim().Unescape(),
							Description = xmlDocument.SelectSingleNode( "//ns1:Header/ns1:Description", nsMgr ).InnerText.Trim().Unescape(),
							Code		= xmlDocument.SelectSingleNode( "//ns1:Snippet/ns1:Code", nsMgr ).InnerText.Trim(),
						};

						Snippets.Add( path, item );
					}

					lvSnippets.ItemsSource = Snippets.Values;
				}
				catch( Exception ex )
				{
					Box.Error( "DetailsDialog constructor, exception:", ex.Message );
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
		/// Loads and sets window size and position
		/// </summary>
		private void LoadSettings()
		{
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
								  "Path:", item.Path ) == MessageBoxResult.Yes )
				{
					try
					{
						File.Delete( item.Path );
					}
					catch( Exception ex )
					{
						Box.Error( "Unable to delete file, exception:", ex.Message );
						return;
					}

					// Remove from source, listview, and clear attribute fields
					Snippets.Remove( item.Path );

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
		/// Overrides the ESC key to quickly dimiss the editor or the manager
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

			// Update the snippet file
			if( SaveSnippetToFile( title, shortcut, description, code, out string fullpath ) == false )
			{
				// There was some problem and it was reported by the method, move on
				goto skipListViewUpdate;
			}

			// Update the listview
			var snippet = Snippets[ item.Path ];

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

				Snippets.Remove( item.Path );

				snippet.Path = fullpath;

				Snippets[ fullpath ] = snippet;
			}

			lvSnippets.Items.Refresh();

			skipListViewUpdate :
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

			if( Box.Question( "Raw snippet file will open in the editor for you to make changes directly at your own risk.",
							  "Are you sure?" ) != MessageBoxResult.Yes )
			{
				return;
			}

			VsShellUtilities.OpenDocument( SnippeterPackage, item.Path );

			Close();
		}




		/// <summary>
		/// Gets the user's snippet folder
		/// </summary>
		private string GetUserSnippetFolder()
		{
			var path = "";

			try
			{
				var vsKey = Registry.CurrentUser.OpenSubKey( @"Software\Microsoft\VisualStudio\" + VsVersion );

				if( vsKey != null )
				{
					path = (string)vsKey.GetValue( "VisualStudioLocation", "" );
				}
			}
			catch( Exception ex )
			{
				Box.Error( "Unable to obtain the user's snippet folder from the registry, exception:", ex.Message );
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
		/// Creates the snippet xml and saves it to file
		/// </summary>
		private bool SaveSnippetToFile( string title,
										string shortcut,
										string description,
										string code,
										out string fullpath )
		{
			fullpath = "";

			var path		= GetUserSnippetFolder();
			var fileName	= title;
			var counter		= 2;

			// Add a numeric tag to the filename if it already exists
			while( File.Exists( Path.Combine( path, fileName + ".snippet" ) ) == true )
			{
				fileName = "{0} {1}".FormatWith( title, counter++ );
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
				Box.Error( "Error creating xml snippet, exception:", ex.Message );
				return false;
			}

			try
			{
				fullpath = Path.Combine( path, fileName + ".snippet" );

				xmlDocument.Save( fullpath );
			}
			catch( XmlException ex )
			{
				Box.Error( "Error saving xml snippet, exception:", ex.Message );
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
      <Title>
{0}
      </Title>
      <Author>
      </Author>
      <Description>
{1}
      </Description>
      <HelpUrl>
      </HelpUrl>
      <Shortcut>{2}</Shortcut>
    </Header>
    <Snippet>
      <Code Language = ""csharp"" Delimiter=""$"">
			<![CDATA[
{3}
			]]>
	  </Code>
    </Snippet>
  </CodeSnippet>
</CodeSnippets>";
	};
}
