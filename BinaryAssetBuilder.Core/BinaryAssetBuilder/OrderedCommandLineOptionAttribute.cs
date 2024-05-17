namespace BinaryAssetBuilder
{
	public class OrderedCommandLineOptionAttribute : CommandLineOptionAttribute
	{
		private int _Ordinal;

		public int Ordinal => _Ordinal;

		public OrderedCommandLineOptionAttribute(int ordinal)
		{
			_Ordinal = ordinal;
		}

		public OrderedCommandLineOptionAttribute(int ordinal, object minValue, object maxValue)
			: base(minValue, maxValue)
		{
			_Ordinal = ordinal;
		}

		public OrderedCommandLineOptionAttribute(int ordinal, object[] validValueSet)
			: base(validValueSet)
		{
			_Ordinal = ordinal;
		}
	}
}
