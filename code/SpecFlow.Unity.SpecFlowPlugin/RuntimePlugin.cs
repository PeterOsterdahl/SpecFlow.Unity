using BoDi;
using Microsoft.Practices.Unity;
using SpecFlow.Unity;
using System;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.UnitTestProvider;

[assembly: RuntimePlugin(typeof(RuntimePlugin))]

namespace SpecFlow.Unity
{
    public class RuntimePlugin : IRuntimePlugin
    {
        private static Object _registrationLock = new Object();
        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters,
            UnitTestProviderConfiguration unitTestProviderConfiguration)
        {
            runtimePluginEvents.CustomizeGlobalDependencies += (sender, args) =>
            {
                // temporary fix for CustomizeGlobalDependencies called multiple times
                // see https://github.com/techtalk/SpecFlow/issues/948
                if (!args.ObjectContainer.IsRegistered<IContainerFinder>())
                {
                    // an extra lock to ensure that there are not two super fast threads re-registering the same stuff
                    lock (_registrationLock)
                    {
                        if (!args.ObjectContainer.IsRegistered<IContainerFinder>())
                        {
                            args.ObjectContainer.RegisterTypeAs<UnityBindingInstanceResolver, ITestObjectResolver>();
                            args.ObjectContainer.RegisterTypeAs<ContainerFinder, IContainerFinder>();
                        }
                    }

                    // workaround for parallel execution issue - this should be rather a feature in BoDi?
                    args.ObjectContainer.Resolve<IContainerFinder>();
                }
            };

            runtimePluginEvents.CustomizeScenarioDependencies += (sender, args) =>
            {
                args.ObjectContainer.RegisterFactoryAs<IUnityContainer>(() =>
                {
                    var containerFinder = args.ObjectContainer.Resolve<IContainerFinder>();
                    var createScenarioContainerBuilder = containerFinder.GetCreateScenarioContainer();
                    var container = createScenarioContainerBuilder();
                    RegisterSpecflowDependecies(args.ObjectContainer, container);
                    return container;
                });
            };
        }
        /// <summary>
        ///     Fix for https://github.com/gasparnagy/SpecFlow.Autofac/issues/11 Cannot resolve ScenarioInfo
        ///     Extracted from
        ///     https://github.com/techtalk/SpecFlow/blob/master/TechTalk.SpecFlow/Infrastructure/ITestObjectResolver.cs
        ///     The test objects might be dependent on particular SpecFlow infrastructure, therefore the implemented
        ///     resolution logic should support resolving the following objects (from the provided SpecFlow container):
        ///     <see cref="ScenarioContext" />, <see cref="FeatureContext" />, <see cref="TestThreadContext" /> and
        ///     <see cref="IObjectContainer" /> (to be able to resolve any other SpecFlow infrastucture). So basically
        ///     the resolution of these classes has to be forwarded to the original container.
        /// </summary>
        /// <param name="objectContainer">SpecFlow DI container.</param>
        /// <param name="containerBuilder">Autofac ContainerBuilder.</param>
        private void RegisterSpecflowDependecies(
            IObjectContainer objectContainer,
           IUnityContainer container)
        {
            container
                .RegisterType<IObjectContainer>(
                    new InjectionFactory((c, t, n) => objectContainer))
                .RegisterType<ScenarioContext>(
                    new InjectionFactory((ctx, t, n) =>
                        {
                            var specflowContainer = ctx.Resolve<IObjectContainer>();
                            var scenarioContext = specflowContainer.Resolve<ScenarioContext>();
                            return scenarioContext;
                        }))
                .RegisterType<FeatureContext>(
                    new InjectionFactory((ctx, t, n) =>
                        {
                            var specflowContainer = ctx.Resolve<IObjectContainer>();
                            var scenarioContext = specflowContainer.Resolve<FeatureContext>();
                            return scenarioContext;
                        }))
                .RegisterType<TestThreadContext>(
                    new InjectionFactory((ctx, t, n) =>
                        {
                            var specflowContainer = ctx.Resolve<IObjectContainer>();
                            var scenarioContext = specflowContainer.Resolve<TestThreadContext>();
                            return scenarioContext;
                        }));
        }
    }
}