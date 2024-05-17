namespace BinaryAssetBuilder
{
	public class OptionalCommandLineOptionAttribute : CommandLineOptionAttribute
	{
		private string _Alias;

		public string Alias => _Alias;

		public OptionalCommandLineOptionAttribute(string alias)
		{
			_Alias = alias;
		}

		public OptionalCommandLineOptionAttribute(string alias, object minValue, object maxValue)
			: base(minValue, maxValue)
		{
			_Alias = alias;
		}

		public OptionalCommandLineOptionAttribute(string alias, object[] validValueSet)
			: base(validValueSet)
		{
			_Alias = alias;
		}
	}
}
