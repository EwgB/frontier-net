namespace FrontierSharp.Common {
	public interface ITimeCappedModule : IBaseModule {
		void Update(double stopAt);
	}
}
