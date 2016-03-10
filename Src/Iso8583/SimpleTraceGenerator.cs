using System;

namespace Fintec.Iso8583
{

	/// <summary> An <see cref="ITraceGenerator"/> that generates a sequence from 1 to 999999 </summary>
	/// <remarks> TODO: This is not a robust implementation. Revisit</remarks>
	public class SimpleTraceGenerator : ITraceGenerator
	{
		private int _value;

		public SimpleTraceGenerator(int initialValue)
		{
			if (initialValue < 1 || initialValue > 999999)
			{
				throw new ArgumentException("initialValue must be between 1 and 999999", nameof(initialValue));
			}
			_value = initialValue;
		}

		public int LastTrace => _value;

		public int NextTrace()
		{
			lock (this)
			{
				_value++;
				if (_value > 999999)
				{
					_value = 1;
				}
				return _value;
			}
		}
	}
}
