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
			// 'using var' ensures sr is disposed on every code path.
			using var sr = new StreamReader(filePath, UTF8Encoding);
			sr.Peek();
			if (!UTF8Encoding.Equals(sr.CurrentEncoding))
			{
				return sr.CurrentEncoding;
			}
			// Read a bounded sample to detect invalid UTF-8 bytes without reading the entire file.
			// This avoids double-reading large script files while still catching Shift-JIS content.
			char[] sample = new char[4096];
			sr.Read(sample, 0, sample.Length);
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
			// leaveOpen:true keeps the underlying stream open after the StreamReader is disposed.
			using var sr = new StreamReader(stream, UTF8Encoding, true, -1, true);
			sr.Peek();
			if (!UTF8Encoding.Equals(sr.CurrentEncoding))
			{
				stream.Seek(pos, SeekOrigin.Begin);
				return sr.CurrentEncoding;
			}
			// Read a bounded sample to detect invalid UTF-8 bytes without reading the entire stream.
			char[] sample = new char[4096];
			sr.Read(sample, 0, sample.Length);
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
