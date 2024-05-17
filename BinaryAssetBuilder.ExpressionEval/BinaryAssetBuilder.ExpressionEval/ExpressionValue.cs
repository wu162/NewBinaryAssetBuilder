using System;
using System.Globalization;

namespace BinaryAssetBuilder.ExpressionEval
{
	public class ExpressionValue
	{
		private enum ValueType
		{
			Integer,
			Float,
			String
		}

		private ValueType _ValueType;

		private int _Int;

		private double _Float;

		private string _String;

		private CultureInfo _CultureInfo = new CultureInfo("en-US", useUserOverride: false);

		private ExpressionValue()
		{
		}

		private static void ThrowError(string msg)
		{
			throw new BinaryAssetBuilderException(ErrorCode.ExpressionEvaluationError, "Expression error: {0}", msg);
		}

		private void SetInteger(string val)
		{
			_ValueType = ValueType.Integer;
			_Int = Convert.ToInt32(val);
		}

		private void SetInteger(int val)
		{
			_ValueType = ValueType.Integer;
			_Int = val;
		}

		public static ExpressionValue NewInteger(string val)
		{
			ExpressionValue expressionValue = new ExpressionValue();
			expressionValue.SetInteger(val);
			return expressionValue;
		}

		private void SetFloat(string val)
		{
			_ValueType = ValueType.Float;
			_Float = Convert.ToDouble(val);
		}

		private void SetFloat(double val)
		{
			_ValueType = ValueType.Float;
			_Float = val;
		}

		public static ExpressionValue NewFloat(string val)
		{
			ExpressionValue expressionValue = new ExpressionValue();
			expressionValue.SetFloat(val);
			return expressionValue;
		}

		private void SetString(string val)
		{
			_ValueType = ValueType.String;
			_String = val;
		}

		public static ExpressionValue NewQuotedString(string val)
		{
			ExpressionValue expressionValue = new ExpressionValue();
			expressionValue.SetString(val.Substring(1, val.Length - 2));
			return expressionValue;
		}

		public static ExpressionValue NewFromLookup(string val)
		{
			ExpressionValue expressionValue = new ExpressionValue();
			float result2;
			if (int.TryParse(val, out var result))
			{
				expressionValue.SetInteger(result);
			}
			else if (float.TryParse(val, out result2))
			{
				expressionValue.SetFloat(result2);
			}
			else
			{
				expressionValue.SetString(val);
			}
			return expressionValue;
		}

		public int AsInteger()
		{
			return _ValueType switch
			{
				ValueType.Integer => _Int, 
				ValueType.Float => Convert.ToInt32(_Float), 
				ValueType.String => Convert.ToInt32(_String), 
				_ => throw new ApplicationException(), 
			};
		}

		public double AsFloat()
		{
			return _ValueType switch
			{
				ValueType.Integer => _Int, 
				ValueType.Float => _Float, 
				ValueType.String => Convert.ToDouble(_String, _CultureInfo.NumberFormat), 
				_ => throw new ApplicationException(), 
			};
		}

		public string AsString()
		{
			return _ValueType switch
			{
				ValueType.Integer => Convert.ToString(_Int), 
				ValueType.Float => Convert.ToString(_Float, _CultureInfo.NumberFormat), 
				ValueType.String => _String, 
				_ => throw new ApplicationException(), 
			};
		}

		private ValueType PromoteToSameType(ExpressionValue rhs)
		{
			if (_ValueType <= rhs._ValueType)
			{
				return rhs._ValueType;
			}
			return _ValueType;
		}

		public void LogicalOr(ExpressionValue rhs)
		{
			switch (_ValueType)
			{
			case ValueType.Integer:
				_Int = ((_Int != 0 || rhs._Int != 0) ? 1 : 0);
				break;
			case ValueType.Float:
				ThrowError("'or' not allowed for floats");
				break;
			case ValueType.String:
				ThrowError("'or' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void LogicalAnd(ExpressionValue rhs)
		{
			switch (_ValueType)
			{
			case ValueType.Integer:
				_Int = ((_Int != 0 && rhs._Int != 0) ? 1 : 0);
				break;
			case ValueType.Float:
				ThrowError("'and' not allowed for floats");
				break;
			case ValueType.String:
				ThrowError("'and' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void IsEqual(ExpressionValue rhs)
		{
			switch (PromoteToSameType(rhs))
			{
			case ValueType.Integer:
				SetInteger((AsInteger() == rhs.AsInteger()) ? 1 : 0);
				break;
			case ValueType.Float:
				SetInteger((AsFloat() == rhs.AsFloat()) ? 1 : 0);
				break;
			case ValueType.String:
				SetInteger((AsString() == rhs.AsString()) ? 1 : 0);
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void IsLessThan(ExpressionValue rhs)
		{
			switch (PromoteToSameType(rhs))
			{
			case ValueType.Integer:
				SetInteger((AsInteger() < rhs.AsInteger()) ? 1 : 0);
				break;
			case ValueType.Float:
				SetInteger((AsFloat() < rhs.AsFloat()) ? 1 : 0);
				break;
			case ValueType.String:
				ThrowError("'<', '>', ... not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void IsGreaterThan(ExpressionValue rhs)
		{
			switch (PromoteToSameType(rhs))
			{
			case ValueType.Integer:
				SetInteger((AsInteger() > rhs.AsInteger()) ? 1 : 0);
				break;
			case ValueType.Float:
				SetInteger((AsFloat() > rhs.AsFloat()) ? 1 : 0);
				break;
			case ValueType.String:
				ThrowError("'<', '>', ... not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void Not()
		{
			switch (_ValueType)
			{
			case ValueType.Integer:
				_Int = ((_Int == 0) ? 1 : 0);
				break;
			case ValueType.Float:
				ThrowError("'!' not allowed for floats");
				break;
			case ValueType.String:
				ThrowError("'!' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void Negate()
		{
			switch (_ValueType)
			{
			case ValueType.Integer:
				_Int = -_Int;
				break;
			case ValueType.Float:
				_Float = 0.0 - _Float;
				break;
			case ValueType.String:
				ThrowError("'!' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void Multiply(ExpressionValue rhs)
		{
			switch (PromoteToSameType(rhs))
			{
			case ValueType.Integer:
				SetInteger(AsInteger() * rhs.AsInteger());
				break;
			case ValueType.Float:
				SetFloat(AsFloat() * rhs.AsFloat());
				break;
			case ValueType.String:
				ThrowError("'*' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void Divide(ExpressionValue rhs)
		{
			switch (PromoteToSameType(rhs))
			{
			case ValueType.Integer:
				if (rhs._Int == 0)
				{
					ThrowError("Division by zero");
				}
				else if (_Int % rhs._Int != 0)
				{
					SetFloat(AsFloat() / rhs.AsFloat());
				}
				else
				{
					SetInteger(AsInteger() / rhs.AsInteger());
				}
				break;
			case ValueType.Float:
				if (rhs.AsFloat() == 0.0)
				{
					ThrowError("Division by zero");
				}
				else
				{
					SetFloat(AsFloat() / rhs.AsFloat());
				}
				break;
			case ValueType.String:
				ThrowError("'/' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void Modulo(ExpressionValue rhs)
		{
			switch (PromoteToSameType(rhs))
			{
			case ValueType.Integer:
				if (rhs._Int == 0)
				{
					ThrowError("Division by zero");
				}
				else
				{
					SetInteger(AsInteger() % rhs.AsInteger());
				}
				break;
			case ValueType.Float:
				ThrowError("'%' not allowed for floats");
				break;
			case ValueType.String:
				ThrowError("'%' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void Add(ExpressionValue rhs)
		{
			switch (PromoteToSameType(rhs))
			{
			case ValueType.Integer:
				SetInteger(AsInteger() + rhs.AsInteger());
				break;
			case ValueType.Float:
				SetFloat(AsFloat() + rhs.AsFloat());
				break;
			case ValueType.String:
				SetString(AsString() + rhs.AsString());
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void Subtract(ExpressionValue rhs)
		{
			switch (PromoteToSameType(rhs))
			{
			case ValueType.Integer:
				SetInteger(AsInteger() - rhs.AsInteger());
				break;
			case ValueType.Float:
				SetFloat(AsFloat() - rhs.AsFloat());
				break;
			case ValueType.String:
				ThrowError("'-' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			}
		}

		public void FuncRound()
		{
			switch (_ValueType)
			{
			case ValueType.Float:
				SetInteger((int)Math.Round(_Float));
				break;
			case ValueType.String:
				ThrowError("'round' not allowed for strings");
				break;
			default:
				throw new ApplicationException();
			case ValueType.Integer:
				break;
			}
		}
	}
}
