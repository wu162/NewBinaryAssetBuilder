using System;

namespace BinaryAssetBuilder
{
	public abstract class CommandLineOptionAttribute : Attribute
	{
		private object _MinValue;

		private object _MaxValue;

		private object[] _ValidValueSet;

		public object MinValue => _MinValue;

		public object MaxValue => _MaxValue;

		public object[] ValidValueSet => _ValidValueSet;

		public CommandLineOptionAttribute()
		{
		}

		public CommandLineOptionAttribute(object minValue, object maxValue)
		{
			_MinValue = minValue;
			_MaxValue = maxValue;
		}

		public CommandLineOptionAttribute(object[] validValueSet)
		{
			_ValidValueSet = validValueSet;
		}
	}
}
