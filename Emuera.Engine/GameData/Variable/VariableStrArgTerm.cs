using MinorShift.Emuera.GameData.Expression;
using MinorShift.Emuera.Sub;
using System.Collections.Generic;
using trerror = EvilMask.Emuera.Lang.Error;

namespace MinorShift.Emuera.GameData.Variable;

//変数の引数のうち文字列型のもの。
internal sealed class VariableStrArgTerm : IOperandTerm
{
	#region EE_ERD
	// public VariableStrArgTerm(VariableCode code, IOperandTerm strTerm, int index)
	public VariableStrArgTerm(VariableCode code, IOperandTerm strTerm, int index, string varname)
	#endregion
		: base(typeof(long))
	{
		this.strTerm = strTerm;
		parentCode = code;
		this.index = index;
		#region EE_ERD
		this.varname = varname;
		#endregion

	}
	IOperandTerm strTerm;
	readonly VariableCode parentCode;
	readonly int index;
	#region EE_ERD
	readonly string varname;
	#endregion
	Dictionary<string, int> dic;
	string errPos;

	public override long GetIntValue(ExpressionMediator exm)
	{
		if (dic == null)
			#region EE_ERD
			// dic = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, parentCode, index);
			dic = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, parentCode, index, varname);
		#endregion
		string key = strTerm.GetStrValue(exm);
		if (string.IsNullOrEmpty(key))
			throw new CodeEE(trerror.KeywordCanNotEmpty.Text);
		#region EE_ERD
		if (dic == null && key != "")
			throw new CodeEE(string.Format(trerror.NotDefinedErdKey.Text, parentCode.ToString(), key));
		#endregion

		if (!dic.TryGetValue(key, out int i))
		{
			if (errPos == null)
				throw new CodeEE(string.Format(trerror.CanNotSpecifiedByString.Text, parentCode.ToString()));
			else
				throw new CodeEE(string.Format(trerror.NotDefinedKey.Text, errPos, key));
		}
		return i;
	}

	public override IOperandTerm Restructure(ExpressionMediator exm)
	{
		if (dic == null)
			#region EE_ERD
			// dic = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, parentCode, index);
			dic = exm.VEvaluator.Constant.GetKeywordDictionary(out errPos, parentCode, index, null);
		#endregion

		strTerm = strTerm.Restructure(exm);
		if (!(strTerm is SingleTerm))
			return this;
		return new SingleLongTerm(GetIntValue(exm));
	}
}
