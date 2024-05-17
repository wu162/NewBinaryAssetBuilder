using antlr;
using antlr.collections.impl;
using BinaryAssetBuilder;
using BinaryAssetBuilder.ExpressionEval;

public class ExpressionParser : LLkParser
{
	public const int EOF = 1;

	public const int NULL_TREE_LOOKAHEAD = 3;

	public const int EQ = 4;

	public const int OR = 5;

	public const int AND = 6;

	public const int NE = 7;

	public const int LS = 8;

	public const int GT = 9;

	public const int LE = 10;

	public const int GE = 11;

	public const int PLUS = 12;

	public const int MINUS = 13;

	public const int MUL = 14;

	public const int DIV = 15;

	public const int MOD = 16;

	public const int LPAREN = 17;

	public const int RPAREN = 18;

	public const int INT = 19;

	public const int FLOAT = 20;

	public const int STRING = 21;

	public const int STRING2 = 22;

	public const int DEFINE = 23;

	public const int ROUND = 24;

	public const int WS = 25;

	public const int DIGIT = 26;

	public const int LETTER = 27;

	public const int NUMBER = 28;

	private ExpressionEvaluator _Eval;

	public static readonly string[] tokenNames_ = new string[29]
	{
		"\"<0>\"", "\"EOF\"", "\"<2>\"", "\"NULL_TREE_LOOKAHEAD\"", "\"EQ\"", "\"OR\"", "\"AND\"", "\"NE\"", "\"LS\"", "\"GT\"",
		"\"LE\"", "\"GE\"", "\"PLUS\"", "\"MINUS\"", "\"MUL\"", "\"DIV\"", "\"MOD\"", "\"LPAREN\"", "\"RPAREN\"", "\"INT\"",
		"\"FLOAT\"", "\"STRING\"", "\"STRING2\"", "\"DEFINE\"", "\"ROUND\"", "\"WS\"", "\"DIGIT\"", "\"LETTER\"", "\"NUMBER\""
	};

	public static readonly BitSet tokenSet_0_ = new BitSet(mk_tokenSet_0_());

	public static readonly BitSet tokenSet_1_ = new BitSet(mk_tokenSet_1_());

	public static readonly BitSet tokenSet_2_ = new BitSet(mk_tokenSet_2_());

	public static readonly BitSet tokenSet_3_ = new BitSet(mk_tokenSet_3_());

	public static readonly BitSet tokenSet_4_ = new BitSet(mk_tokenSet_4_());

	public static readonly BitSet tokenSet_5_ = new BitSet(mk_tokenSet_5_());

	public static readonly BitSet tokenSet_6_ = new BitSet(mk_tokenSet_6_());

	public static readonly BitSet tokenSet_7_ = new BitSet(mk_tokenSet_7_());

	public void SetEvaluator(ExpressionEvaluator e)
	{
		_Eval = e;
	}

	public override void reportError(RecognitionException ex)
	{
		throw new BinaryAssetBuilderException(ErrorCode.ExpressionEvaluationError, "Expression error: {0}", ex.Message);
	}

	protected void initialize()
	{
		tokenNames = tokenNames_;
	}

	protected ExpressionParser(TokenBuffer tokenBuf, int k)
		: base(tokenBuf, k)
	{
		initialize();
	}

	public ExpressionParser(TokenBuffer tokenBuf)
		: this(tokenBuf, 1)
	{
	}

	protected ExpressionParser(TokenStream lexer, int k)
		: base(lexer, k)
	{
		initialize();
	}

	public ExpressionParser(TokenStream lexer)
		: this(lexer, 1)
	{
	}

	public ExpressionParser(ParserSharedInputState state)
		: base(state, 1)
	{
		initialize();
	}

	public ExpressionValue expr()
	{
		ExpressionValue result = null;
		try
		{
			match(4);
			result = orExpr();
			match(1);
			return result;
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_0_);
			return result;
		}
	}

	public ExpressionValue orExpr()
	{
		ExpressionValue expressionValue = null;
		try
		{
			expressionValue = andExpr();
			while (LA(1) == 5)
			{
				match(5);
				ExpressionValue rhs = andExpr();
				expressionValue.LogicalOr(rhs);
			}
			return expressionValue;
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_1_);
			return expressionValue;
		}
	}

	public ExpressionValue andExpr()
	{
		ExpressionValue expressionValue = null;
		try
		{
			expressionValue = eqExpr();
			while (LA(1) == 6)
			{
				match(6);
				ExpressionValue rhs = eqExpr();
				expressionValue.LogicalAnd(rhs);
			}
			return expressionValue;
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_2_);
			return expressionValue;
		}
	}

	public ExpressionValue eqExpr()
	{
		ExpressionValue expressionValue = null;
		try
		{
			expressionValue = relExpr();
			while (true)
			{
				switch (LA(1))
				{
				case 4:
				{
					match(4);
					ExpressionValue rhs = relExpr();
					expressionValue.IsEqual(rhs);
					break;
				}
				case 7:
				{
					match(7);
					ExpressionValue rhs = relExpr();
					expressionValue.IsEqual(rhs);
					expressionValue.Not();
					break;
				}
				default:
					return expressionValue;
				}
			}
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_3_);
			return expressionValue;
		}
	}

	public ExpressionValue relExpr()
	{
		ExpressionValue expressionValue = null;
		try
		{
			expressionValue = addExpr();
			while (true)
			{
				switch (LA(1))
				{
				case 8:
				{
					match(8);
					ExpressionValue rhs = addExpr();
					expressionValue.IsLessThan(rhs);
					break;
				}
				case 9:
				{
					match(9);
					ExpressionValue rhs = addExpr();
					expressionValue.IsGreaterThan(rhs);
					break;
				}
				case 10:
				{
					match(10);
					ExpressionValue rhs = addExpr();
					expressionValue.IsGreaterThan(rhs);
					expressionValue.Not();
					break;
				}
				case 11:
				{
					match(11);
					ExpressionValue rhs = addExpr();
					expressionValue.IsLessThan(rhs);
					expressionValue.Not();
					break;
				}
				default:
					return expressionValue;
				}
			}
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_4_);
			return expressionValue;
		}
	}

	public ExpressionValue addExpr()
	{
		ExpressionValue expressionValue = null;
		try
		{
			expressionValue = mulExpr();
			while (true)
			{
				switch (LA(1))
				{
				case 12:
				{
					match(12);
					ExpressionValue rhs = mulExpr();
					expressionValue.Add(rhs);
					break;
				}
				case 13:
				{
					match(13);
					ExpressionValue rhs = mulExpr();
					expressionValue.Subtract(rhs);
					break;
				}
				default:
					return expressionValue;
				}
			}
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_5_);
			return expressionValue;
		}
	}

	public ExpressionValue mulExpr()
	{
		ExpressionValue expressionValue = null;
		try
		{
			expressionValue = unaryExpr();
			while (true)
			{
				switch (LA(1))
				{
				case 14:
				{
					match(14);
					ExpressionValue rhs = unaryExpr();
					expressionValue.Multiply(rhs);
					break;
				}
				case 15:
				{
					match(15);
					ExpressionValue rhs = unaryExpr();
					expressionValue.Divide(rhs);
					break;
				}
				case 16:
				{
					match(16);
					ExpressionValue rhs = unaryExpr();
					expressionValue.Modulo(rhs);
					break;
				}
				default:
					return expressionValue;
				}
			}
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_6_);
			return expressionValue;
		}
	}

	public ExpressionValue unaryExpr()
	{
		ExpressionValue expressionValue = null;
		try
		{
			switch (LA(1))
			{
			case 12:
				match(12);
				expressionValue = unaryExpr();
				return expressionValue;
			case 13:
				match(13);
				expressionValue = unaryExpr();
				expressionValue.Negate();
				return expressionValue;
			case 17:
			case 19:
			case 20:
			case 21:
			case 22:
			case 23:
			case 24:
				expressionValue = singleValue();
				return expressionValue;
			default:
				throw new NoViableAltException(LT(1), getFilename());
			}
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_7_);
			return expressionValue;
		}
	}

	public ExpressionValue singleValue()
	{
		ExpressionValue result = null;
		IToken token = null;
		IToken token2 = null;
		IToken token3 = null;
		IToken token4 = null;
		IToken token5 = null;
		try
		{
			switch (LA(1))
			{
			case 17:
				match(17);
				result = orExpr();
				match(18);
				return result;
			case 19:
				token = LT(1);
				match(19);
				result = ExpressionValue.NewInteger(token.getText());
				return result;
			case 20:
				token2 = LT(1);
				match(20);
				result = ExpressionValue.NewFloat(token2.getText());
				return result;
			case 21:
				token3 = LT(1);
				match(21);
				result = ExpressionValue.NewQuotedString(token3.getText());
				return result;
			case 22:
				token4 = LT(1);
				match(22);
				result = ExpressionValue.NewQuotedString(token4.getText());
				return result;
			case 23:
				token5 = LT(1);
				match(23);
				result = ExpressionValue.NewFromLookup(_Eval.LookupDefine(token5.getText()));
				return result;
			case 24:
				result = regularFunction();
				return result;
			default:
				throw new NoViableAltException(LT(1), getFilename());
			}
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_7_);
			return result;
		}
	}

	public ExpressionValue regularFunction()
	{
		ExpressionValue expressionValue = null;
		try
		{
			match(24);
			match(17);
			expressionValue = orExpr();
			match(18);
			expressionValue.FuncRound();
			return expressionValue;
		}
		catch (RecognitionException ex)
		{
			reportError(ex);
			recover(ex, tokenSet_7_);
			return expressionValue;
		}
	}

	private void initializeFactory()
	{
	}

	private static long[] mk_tokenSet_0_()
	{
		return new long[2] { 2L, 0L };
	}

	private static long[] mk_tokenSet_1_()
	{
		return new long[2] { 262146L, 0L };
	}

	private static long[] mk_tokenSet_2_()
	{
		return new long[2] { 262178L, 0L };
	}

	private static long[] mk_tokenSet_3_()
	{
		return new long[2] { 262242L, 0L };
	}

	private static long[] mk_tokenSet_4_()
	{
		return new long[2] { 262386L, 0L };
	}

	private static long[] mk_tokenSet_5_()
	{
		return new long[2] { 266226L, 0L };
	}

	private static long[] mk_tokenSet_6_()
	{
		return new long[2] { 278514L, 0L };
	}

	private static long[] mk_tokenSet_7_()
	{
		return new long[2] { 393202L, 0L };
	}
}
