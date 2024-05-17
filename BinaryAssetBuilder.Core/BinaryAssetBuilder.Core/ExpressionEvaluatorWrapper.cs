using System;
using System.Reflection;

namespace BinaryAssetBuilder.Core
{
	public class ExpressionEvaluatorWrapper
	{
		private static Tracer _Tracer = Tracer.GetTracer("XIncludingReader", "Provides xi:include reading functionality");

		private static Assembly _Lib = null;

		public static void LoadAssembly()
		{
			try
			{
				_Lib = Assembly.Load("BinaryAssetBuilder.ExpressionEval");
				_Lib.GetType("BinaryAssetBuilder.ExpressionEval.ExpressionEvaluator");
			}
			catch (Exception)
			{
				_Lib = null;
				_Tracer.TraceError("Could not load ExpressionEvaluator from BinaryAssetBuilder.ExpressionEval.dll. Expressions are disabled");
			}
		}

		public static IExpressionEvaluator GetEvaluator(AssetDeclarationDocument document)
		{
			try
			{
				if (_Lib != null)
				{
					object[] args = new object[1] { document };
					return (IExpressionEvaluator)_Lib.CreateInstance("BinaryAssetBuilder.ExpressionEval.ExpressionEvaluator", ignoreCase: false, BindingFlags.CreateInstance, null, args, null, null);
				}
			}
			catch (Exception)
			{
				_Tracer.TraceError("Could not load ExpressionEvaluator from BinaryAssetBuilder.ExpressionEval.dll. Expressions are disabled");
			}
			return null;
		}
	}
}
