using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinorShift.Emuera.GameProc.PluginSystem
{
	public abstract class PluginManifestAbstract
	{
		//For future cases
		public abstract string PluginName { get; }
		public abstract string PluginDescription { get; }
		public abstract string PluginVersion { get; }
		public abstract string PluginAuthor { get; }

		public PluginManifestAbstract() { }

		public List<IPluginMethod> GetPluginMethods()
		{
			return methods;
		}

		protected List<IPluginMethod> methods = new List<IPluginMethod>();
	}
}
