namespace MinorShift.Emuera.GameProc.PluginSystem
{
	public interface IPluginMethod
	{

		public abstract string Name { get; }
		public abstract string Description { get; }
		public abstract void Execute(PluginMethodParameter[] args);

	}
}
