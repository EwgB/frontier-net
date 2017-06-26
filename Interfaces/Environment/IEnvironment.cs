﻿namespace FrontierSharp.Interfaces.Environment {
    using Property;

    ///<summary>The environment. Lighting, fog, and so on.</summary>
    public interface IEnvironment : IHasProperties, IModule {
        EnvironmentData GetCurrent();
    }
}
