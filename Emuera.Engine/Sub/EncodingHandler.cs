using System.IO;
using System.Text;

namespace MinorShift.Emuera.Sub;

public static class EncodingHandler
{
	public static Encoding UTF8Encoding = new UTF8Encoding(false, true);
	public static Encoding shiftjisEncoding = GetEncoding(932);
	public static Encoding UTF8BOMEncoding = new UTF8Encoding(true, true);


	public static Encoding DetectEncoding(string filePath)
	{
		try
		{
			using var sr = new StreamReader(filePath, UTF8Encoding);
			sr.Peek();
			if (!UTF8Encoding.Equals(sr.CurrentEncoding))
			{
				return sr.CurrentEncoding;
			}
			sr.ReadToEnd();
			sr.Dispose();
			return UTF8Encoding;
		}
		catch
		{
			return shiftjisEncoding;
		}
	}

	public static Encoding DetectEncoding(Stream stream)
	{
		var pos = stream.Position;
		try
		{
			using var sr = new StreamReader(stream, UTF8Encoding, true, -1, true);
			sr.Peek();
			if (!UTF8Encoding.Equals(sr.CurrentEncoding))
			{
				stream.Seek(pos, SeekOrigin.Begin);
				return sr.CurrentEncoding;
			}
			sr.ReadToEnd();
			sr.Dispose();
			stream.Seek(pos, SeekOrigin.Begin);
			return UTF8Encoding;
		}
		catch
		{
			stream.Seek(pos, SeekOrigin.Begin);
			return shiftjisEncoding;
		}
	}
	public static Encoding GetEncoding(int codePage)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		return Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
	}
}
