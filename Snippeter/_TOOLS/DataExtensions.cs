using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Windows;

namespace LaraSPQ.Tools
{
	internal static class DataExtensions
	{
		/// <summary>
		/// Copies text to the clipboard in text and Unicode formats
		/// </summary>
		public static bool CopyToClipboard( this string text )
		{
			var success = true;

			// DataObject.SetText() will thrown an exception on null or ""
			if( text.IsNullOrWhitespace() == false )
			{
				try
				{
					var dataObject = new DataObject();

					dataObject.SetText( text, TextDataFormat.Text );
					dataObject.SetText( text, TextDataFormat.UnicodeText );

					Clipboard.Clear();

					Clipboard.SetDataObject( dataObject );
				}
				catch( Exception ex )
				{
					Box.Error( "Unable to copy data to the clipboard, exception:", ex.Message,
							   "Carry on, this is a non-critical error." );

					success = false;
				}
			}

			return success;
		}




		/// <summary>
		/// Encodes problematic XML characters
		/// </summary>
		internal static string Escape( this string value )
		{
			return SecurityElement.Escape( value );
		}




		/// <summary>
		/// Decodes everything, including problematic XML characters
		/// </summary>
		internal static string Unescape( this string value )
		{
			return WebUtility.HtmlDecode( value );
		}




		/// <summary>
		/// Checks whether a filename contains invalid characters
		/// </summary>
		internal static bool IsFilenameValid( this string filename )
		{
			return ( filename.IndexOfAny( Path.GetInvalidPathChars() ) == -1 );
		}




		/// <summary>
		/// Gets the paths to all files in a folder, with optional search pattern and filename only (no extension).<para/>
		/// </summary>
		/// <exception cref="System.Security.SecurityException"></exception>
		/// <exception cref="PathTooLongException"></exception>
		/// <exception cref="DirectoryNotFoundException"></exception>
		internal static List<string> GetFilesInFolder( this string sourcePath,
													   string searchPattern = "*.*",
													   bool filenamesOnly = false )
		{
			if( sourcePath == ""
				|| Directory.Exists( sourcePath ) == false )
			{
				return null;
			}

			var di		= new DirectoryInfo( sourcePath );
			var fsi		= di.GetFileSystemInfos( searchPattern );
			var paths	= fsi.Where( f => ( f.Attributes & FileAttributes.Archive ) == FileAttributes.Archive )
						  .OrderBy( f => f.Name );
			var ls = new List<string>( paths.Count() );

			foreach( var path in paths )
			{
				if( filenamesOnly == true )
				{
					ls.Add( Path.GetFileNameWithoutExtension( path.Name ) );
				}
				else
				{
					ls.Add( path.FullName );
				}
			}

			return ls;
		}




		/// <summary>
		/// Simplifies string.IsNullOrWhiteSpace(...) by turning it into an extension, e.g., text.IsNullOrWhitespace()<para/>
		/// </summary>
		internal static bool IsNullOrWhitespace( this string text )
		{
			return string.IsNullOrWhiteSpace( text );
		}




		/// <summary>
		/// Simplifies string.Format(...) by turning it into an extension, e.g., "{0}-{1}".FormatWith(x,y)<para/>
		/// </summary>
		internal static string FormatWith( this string format, params object[] args )
		{
			if( format.IsNullOrWhitespace() == true )
			{
				return "";
			}

			return string.Format( format, args );
		}




		/// <summary>
		/// Simplifies string.Join(...) by turning it into an extension, e.g., collection.Join( EOL )<para/>
		/// </summary>
		internal static string Join( this IEnumerable<string> ie, string delimiter )
		{
			return string.Join( delimiter, ie );
		}
	}
}
