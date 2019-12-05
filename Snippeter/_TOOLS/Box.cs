using System;
using System.Windows;

namespace LaraSPQ.Tools
{
	internal static class Box
	{
		/// <summary>
		/// Single environment EOL
		/// </summary>
		internal static readonly string NL = Environment.NewLine;
		/// <summary>
		/// Double environment EOL
		/// </summary>
		internal static readonly string DNL = Box.NL + Box.NL;


		/// <summary>
		/// Question single line
		/// </summary>
		internal static MessageBoxResult Question( params string[] lines )
		{
			return Question( lines.Join( DNL ) );
		}




		/// <summary>
		/// Question multi-line
		/// </summary>
		internal static MessageBoxResult Question( string msg )
		{
			try
			{
				return MessageBox.Show( msg,
										"Question",
										MessageBoxButton.YesNo,
										MessageBoxImage.Question );
			}
			catch( InvalidOperationException )
			{
				return MessageBoxResult.None;
			}
		}




		/// <summary>
		/// Yes/no/cancel question multi-line
		/// </summary>

		internal static MessageBoxResult QuestionYesNoCancel( params string[] lines )
		{
			return QuestionYesNoCancel( lines.Join( DNL ) );
		}




		/// <summary>
		/// Yes/no/cancel question single line
		/// </summary>
		internal static MessageBoxResult QuestionYesNoCancel( string msg )
		{
			try
			{
				return MessageBox.Show( msg,
										"Question",
										MessageBoxButton.YesNoCancel,
										MessageBoxImage.Question );
			}
			catch( InvalidOperationException )
			{
				return MessageBoxResult.None;
			}
		}




		/// <summary>
		/// Warning multi-line
		/// </summary>
		internal static MessageBoxResult Warning( params string[] lines )
		{
			return Warning( lines.Join( DNL ) );
		}




		/// <summary>
		/// Warning single line
		/// </summary>
		internal static MessageBoxResult Warning( string msg )
		{
			try
			{
				return MessageBox.Show( msg,
										"Warning",
										MessageBoxButton.OK,
										MessageBoxImage.Warning );
			}
			catch( InvalidOperationException )
			{
				return MessageBoxResult.None;
			}
		}




		/// <summary>
		/// Info multi-line
		/// </summary>
		internal static MessageBoxResult Info( params string[] lines )
		{
			return Info( lines.Join( DNL ) );
		}




		/// <summary>
		/// Info single line
		/// </summary>
		internal static MessageBoxResult Info( string msg )
		{
			try
			{
				return MessageBox.Show( msg,
										"Information",
										MessageBoxButton.OK,
										MessageBoxImage.Information );
			}
			catch( InvalidOperationException )
			{
				return MessageBoxResult.None;
			}
		}




		/// <summary>
		/// Error multi-line
		/// </summary>
		internal static MessageBoxResult Error( params string[] lines )
		{
			return Error( lines.Join( DNL ) );
		}




		/// <summary>
		/// Error single line
		/// </summary>
		internal static MessageBoxResult Error( string msg )
		{
			try
			{
				return MessageBox.Show( msg,
										"ERROR",
										MessageBoxButton.OK,
										MessageBoxImage.Error );
			}
			catch( InvalidOperationException )
			{
				return MessageBoxResult.None;
			}
		}
	}
}
