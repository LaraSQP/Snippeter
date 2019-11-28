using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Snippeter
{
	internal static class WindowExtensions
	{
		// https://stackoverflow.com/questions/339620/how-do-i-remove-minimize-and-maximize-from-a-resizable-window-in-wpf
		// From winuser.h
		private const int GWL_STYLE = -16,
		//WS_MAXIMIZEBOX	= 0x10000,
						  WS_MINIMIZEBOX = 0x20000;

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
	}
}
