using MinorShift.Emuera.GameProc.Function;
using Xunit;

namespace Emuera.Tests;

public class CircularBufferTests
{
	[Fact]
	public void Insert_InMiddle_PreservesOrder()
	{
		var buffer = new CircularBuffer<int>(8);
		buffer.Enqueue(1);
		buffer.Enqueue(2);
		buffer.Enqueue(3);
		buffer.Enqueue(4);

		buffer.Insert(1, 99);

		Assert.Equal(5, buffer.Count);
		Assert.Equal(1, buffer[0]);
		Assert.Equal(99, buffer[1]);
		Assert.Equal(2, buffer[2]);
		Assert.Equal(3, buffer[3]);
		Assert.Equal(4, buffer[4]);
	}
}
