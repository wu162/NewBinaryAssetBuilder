using System.Collections.Generic;

namespace BinaryAssetBuilder.Core
{
	public class DefinitionSet : SortedDictionary<string, Definition>
	{
		public void AddDefinitions(DefinitionSet definitions)
		{
			foreach (Definition value2 in definitions.Values)
			{
				if (TryGetValue(value2.Name, out var value) && !value2.Document.SourcePath.Equals(value.Document.SourcePath))
				{
					throw new BinaryAssetBuilderException(ErrorCode.DuplicateDefine, "Definition {0} defined in {1} is already defined in {2}", value2.Name, value2.Document.SourcePath, value.Document.SourcePath);
				}
				base[value2.Name] = value2;
			}
		}

		public string GetEvaluatedValue(string name)
		{
			if (!TryGetValue(name, out var value))
			{
				return null;
			}
			return value.EvaluatedValue;
		}
	}
}
