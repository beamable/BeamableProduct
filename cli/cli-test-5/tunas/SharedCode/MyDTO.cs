using DinkToPdf;
using System;

namespace TunaTown
{
	public class MyDTO
	{
		public int x;

		/// <summary>
		/// Sample code
		/// </summary>
		public void Test()
		{
			var converter = new BasicConverter(new PdfTools());
			Console.WriteLine("Hello there");
		}
	}
}
