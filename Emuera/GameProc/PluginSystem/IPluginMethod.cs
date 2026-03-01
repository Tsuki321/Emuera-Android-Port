using MinorShift.Emuera.GameData.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinorShift.Emuera.GameProc.PluginSystem
{
	public interface IPluginMethod
	{

		public abstract string Name { get; }
		public abstract string Description { get; }
		public abstract void Execute(PluginMethodParameter[] args);

	}
}
