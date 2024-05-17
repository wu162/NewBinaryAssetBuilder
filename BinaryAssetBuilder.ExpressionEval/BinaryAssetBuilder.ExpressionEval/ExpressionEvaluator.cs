using System.IO;
using BinaryAssetBuilder.Core;

namespace BinaryAssetBuilder.ExpressionEval
{
	public class ExpressionEvaluator : IExpressionEvaluator
	{
		private AssetDeclarationDocument _Document;

		public ExpressionEvaluator(AssetDeclarationDocument document)
		{
			_Document = document;
		}

		public string LookupDefine(string name)
		{
			name = name.Remove(0, 1);
			Definition value = null;
			if (_Document.AllDefines.TryGetValue(name, out value) && value.EvaluatedValue != null)
			{
				if (value.Document != _Document)
				{
					_Document.UsedDefines[name] = value.EvaluatedValue;
				}
				return value.EvaluatedValue;
			}
			throw new BinaryAssetBuilderException(ErrorCode.ExpressionEvaluationError, "Unknown define {0}", name);
		}

		public string Evaluate(string expression)
		{
			ExpressionLexer lexer = new ExpressionLexer(new StringReader(expression));
			ExpressionParser expressionParser = new ExpressionParser(lexer);
			expressionParser.SetEvaluator(this);
			return expressionParser.expr().AsString();
		}

		public void EvaluateDefinition(Definition definition)
		{
			if (definition.OriginalValue != null)
			{
				if (definition.OriginalValue.Length > 0 && definition.OriginalValue[0] == '=')
				{
					ExpressionLexer lexer = new ExpressionLexer(new StringReader(definition.OriginalValue));
					ExpressionParser expressionParser = new ExpressionParser(lexer);
					expressionParser.SetEvaluator(this);
					definition.EvaluatedValue = expressionParser.expr().AsString();
				}
				else
				{
					definition.EvaluatedValue = definition.OriginalValue;
				}
				definition.OriginalValue = null;
			}
		}
	}
}
