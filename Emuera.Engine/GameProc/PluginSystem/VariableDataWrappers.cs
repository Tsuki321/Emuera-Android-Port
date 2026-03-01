using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.GameData.Variable;
using System;
using System.Collections.Generic;

namespace MinorShift.Emuera.GameProc.PluginSystem
{
	public class GlobalInt1dWrapper
	{
		public Int64 this[string key]
		{
			get => Get(key);
			set => Set(key, value);
		}
		public Int64 this[Int64 key]
		{
			get => Get(key);
			set => Set(key, value);
		}

		internal GlobalInt1dWrapper(VariableToken token, ExpressionMediator exm, VariableCode variableCode)
		{
			this.token = token;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public Int64 Get(string key)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			return Get(dict[key]);
		}
		public Int64 Get(Int64 key)
		{
			return token.GetIntValue(exm, [key]);
		}
		public void Set(string key, Int64 value)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			Set(dict[key], value);
		}
		public void Set(Int64 key, Int64 value)
		{
			token.SetValue(value, [key]);
		}

		private VariableToken token;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}

	public class GlobalString1dWrapper
	{
		public string this[string key]
		{
			get => Get(key);
			set => Set(key, value);
		}
		public string this[Int64 key]
		{
			get => Get(key);
			set => Set(key, value);
		}

		internal GlobalString1dWrapper(VariableToken token, ExpressionMediator exm, VariableCode variableCode)
		{
			this.token = token;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public string Get(string key)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			return Get(dict[key]);
		}
		public string Get(Int64 key)
		{
			return token.GetStrValue(exm, [key]);
		}
		public void Set(string key, string value)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			Set(dict[key], value);
		}
		public void Set(Int64 key, string value)
		{
			token.SetValue(value, [key]);
		}

		private VariableToken token;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}

	public class GlobalConstString1dWrapper
	{
		public string this[string key]
		{
			get => Get(key);
		}
		public string this[Int64 key]
		{
			get => Get(key);
		}

		internal GlobalConstString1dWrapper(VariableToken token, ExpressionMediator exm, VariableCode variableCode)
		{
			this.token = token;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public string Get(string key)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			return Get(dict[key]);
		}
		public string Get(Int64 key)
		{
			return token.GetStrValue(exm, [key]);
		}

		private VariableToken token;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}
	public class GlobalConstInt1dWrapper
	{
		public Int64 this[string key]
		{
			get => Get(key);
		}
		public Int64 this[Int64 key]
		{
			get => Get(key);
		}

		internal GlobalConstInt1dWrapper(VariableToken token, ExpressionMediator exm, VariableCode variableCode)
		{
			this.token = token;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public Int64 Get(string key)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			return Get(dict[key]);
		}
		public Int64 Get(Int64 key)
		{
			return token.GetIntValue(exm, [key]);
		}

		private VariableToken token;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}
	public class CharInt1dWrapper
	{
		public Int64 this[string key]
		{
			get => Get(key);
			set => Set(key, value);
		}
		public Int64 this[Int64 key]
		{
			get => Get(key);
			set => Set(key, value);
		}

		internal CharInt1dWrapper(Int64 charId, VariableToken token, ExpressionMediator exm, VariableCode variableCode)
		{
			this.charId = charId;
			this.token = token;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public Int64 Get(string key)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			return Get(dict[key]);
		}
		public Int64 Get(Int64 key)
		{
			return token.GetIntValue(exm, [charId, key]);
		}
		public void Set(string key, Int64 value)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			Set(dict[key], value);
		}
		public void Set(Int64 key, Int64 value)
		{
			token.SetValue(value, [charId, key]);
		}

		private Int64 charId;
		private VariableToken token;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}
	public class CharInt2dWrapper
	{
		public Int64 this[string keyA, string keyB]
		{
			get => Get(keyA, keyB);
			set => Set(keyA, keyB, value);
		}
		public Int64 this[Int64 keyA, string keyB]
		{
			get => Get(keyA, keyB);
			set => Set(keyA, keyB, value);
		}
		public Int64 this[string keyA, Int64 keyB]
		{
			get => Get(keyA, keyB);
			set => Set(keyA, keyB, value);
		}
		public Int64 this[Int64 keyA, Int64 keyB]
		{
			get => Get(keyA, keyB);
			set => Set(keyA, keyB, value);
		}

		internal CharInt2dWrapper(Int64 charId, VariableToken token, ExpressionMediator exm, VariableCode variableCode)
		{
			this.charId = charId;
			this.token = token;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public Int64 Get(string keyA, string keyB)
		{
			var errPos = "";
			var dictA = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, keyA);
			var dictB = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, keyB);
			return Get(dictA[keyA], dictB[keyB]);
		}
		public Int64 Get(Int64 keyA, string keyB)
		{
			var errPos = "";
			var dictB = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, keyB);
			return Get(keyA, dictB[keyB]);
		}
		public Int64 Get(string keyA, Int64 keyB)
		{
			var errPos = "";
			var dictA = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, keyA);
			return Get(dictA[keyA], keyB);
		}
		public Int64 Get(Int64 keyA, Int64 keyB)
		{
			return token.GetIntValue(exm, [charId, keyA, keyB]);
		}
		public void Set(string keyA, string keyB, Int64 value)
		{
			var errPos = "";
			var dictA = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, keyA);
			var dictB = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, keyB);
			Set(dictA[keyA], dictB[keyB], value);
		}
		public void Set(Int64 keyA, string keyB, Int64 value)
		{
			var errPos = "";
			var dictB = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, keyB);
			Set(keyA, dictB[keyB], value);
		}
		public void Set(string keyA, Int64 keyB, Int64 value)
		{
			var errPos = "";
			var dictA = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, keyA);
			Set(dictA[keyA], keyB, value);
		}
		public void Set(Int64 keyA, Int64 keyB, Int64 value)
		{
			token.SetValue(value, [charId, keyA, keyB]);
		}

		private Int64 charId;
		private VariableToken token;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}

	public class CharUserdefinedInt1dWrapper
	{
		public Int64 this[string key]
		{
			get => Get(key);
			set => Set(key, value);
		}
		public Int64 this[string key, Int64 idx]
		{
			get => Get(key, idx);
			set => Set(key, value, idx);
		}

		internal CharUserdefinedInt1dWrapper(Int64 charId, Dictionary<string,VariableToken> tokens, ExpressionMediator exm, VariableCode variableCode)
		{
			this.charId = charId;
			this.tokens = tokens;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public Int64 Get(string key, Int64 idx = 0)
		{
			if (Config.ICVariable)
			{
				key = key.ToUpper();
			}
			return tokens[key].GetIntValue(exm, [charId, idx]);
		}
		public void Set(string key, Int64 value, Int64 idx = 0)
		{
			if (Config.ICVariable)
			{
				key = key.ToUpper();
			}
			tokens[key].SetValue(value, [charId, idx]);
		}

		private Int64 charId;
		private Dictionary<string, VariableToken> tokens;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}
	public class CharStringWrapper
	{
		internal CharStringWrapper(Int64 charId, VariableToken token, ExpressionMediator exm, VariableCode variableCode)
		{
			this.charId = charId;
			this.token = token;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public string Get()
		{
			return token.GetStrValue(exm, [charId]);
		}
		public void Set(string value)
		{
			token.SetValue(value, [charId]);
		}

		private Int64 charId;
		private VariableToken token;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}

	public class CharString1dWrapper
	{
		public string this[string key]
		{
			get => Get(key);
			set => Set(key, value);
		}
		public string this[Int64 key]
		{
			get => Get(key);
			set => Set(key, value);
		}
		internal CharString1dWrapper(Int64 charId, VariableToken token, ExpressionMediator exm, VariableCode variableCode)
		{
			this.charId = charId;
			this.token = token;
			this.exm = exm;
			this.variableCode = variableCode;
		}

		public string Get(string key)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			return Get(dict[key]);
		}
		public string Get(Int64 key)
		{
			return token.GetStrValue(exm, [charId, key]);
		}
		public void Set(string key, string value)
		{
			var errPos = "";
			var dict = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, variableCode, 1, key);
			Set(dict[key], value);
		}
		public void Set(Int64 key, string value)
		{
			token.SetValue(value, [charId, key]);
		}

		private Int64 charId;
		private VariableToken token;
		private ExpressionMediator exm;
		private VariableCode variableCode;
	}
}
