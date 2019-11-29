using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snippeter
{
	internal static class WindowExtensions
	{
		// https://stackoverflow.com/questions/339620/how-do-i-remove-minimize-and-maximize-from-a-resizable-window-in-wpf
		private const int	GWL_STYLE		= -16;
		private const int	WS_MINIMIZEBOX	= 0x20000;

		[ DllImport( "user32.dll" ) ]
		extern private static int GetWindowLong( IntPtr hwnd, int index );
		[ DllImport( "user32.dll" ) ]
		extern private static int SetWindowLong( IntPtr hwnd, int index, int value );

		internal static void HideDisableMinimizeButton( this Window window )
		{
			window.SourceInitialized += ( s, e ) =>
			{
				var hwnd			= new System.Windows.Interop.WindowInteropHelper( window ).Handle;
				var currentStyle	= GetWindowLong( hwnd, GWL_STYLE );

				SetWindowLong( hwnd, GWL_STYLE, ( currentStyle & ~WS_MINIMIZEBOX ) );
			};
		}




		// https://stackoverflow.com/questions/26260654/wpf-converting-bitmap-to-imagesource
		[ DllImport( "gdi32.dll", EntryPoint = "DeleteObject" ) ]
		[ return : MarshalAs( UnmanagedType.Bool ) ]
		extern private static bool DeleteObject([ In ] IntPtr hObject );

		internal static ImageSource ImageSourceFromIcon( Icon icon )
		{
			var imageSource = null as ImageSource;

			try
			{
				var bmp		= icon.ToBitmap();
				var handle	= bmp.GetHbitmap();

				imageSource = Imaging.CreateBitmapSourceFromHBitmap( handle,
																	 IntPtr.Zero,
																	 Int32Rect.Empty,
																	 BitmapSizeOptions.FromEmptyOptions() );
				DeleteObject( handle );
				bmp.Dispose();
			}
			catch( Exception )
			{
				// Ignore exception quietly
				imageSource = null;
			}

			return imageSource;
		}
	}
}
