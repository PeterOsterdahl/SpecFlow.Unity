﻿using BoDi;
using Microsoft.Practices.Unity;
using System;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlow.Unity
{
    public class UnityBindingInstanceResolver : ITestObjectResolver
    {
        public object ResolveBindingInstance(Type bindingType, IObjectContainer scenarioContainer)
        {
            var componentContext = scenarioContainer.Resolve<IUnityContainer>();
            return componentContext.Resolve(bindingType);
        }
    }
}