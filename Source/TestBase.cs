using System;
using System.Collections.Generic;
using System.Linq;

namespace Moq.Modules
{
    public class TestBase : IMockProducer
    {
        protected static IDictionary<Type, IMockImplementationProvider> _mockProviders;
        protected static IDictionary<Type, IConcreteImplementationProvider> _concreteProviders;

        static TestBase()
        {
            var mockImplementationProviderInterface = typeof(IMockImplementationProvider);

            var mockProviders = ReflectionHelper.GetTypes(t => 
                    t.IsClass && 
                    !t.IsAbstract &&
                    mockImplementationProviderInterface.IsAssignableFrom(t))
                .ToList();

            _mockProviders = new Dictionary<Type, IMockImplementationProvider>();
            foreach (var providerType in mockProviders)
            {
                var provider = providerType.GetConstructor(Type.EmptyTypes).Invoke(null) as IMockImplementationProvider;
                if (provider == null) continue;
                if (_mockProviders.ContainsKey(provider.MockedType))
                    throw new Exception(
                        providerType.FullName + " and " +
                        _mockProviders[provider.MockedType].GetType().FullName +
                        " both provide mocked implementations of " + provider.MockedType.FullName);
                _mockProviders.Add(provider.MockedType, provider);
            }

            var concreteImplementationProviderInterface = typeof(IConcreteImplementationProvider);

            var concreteProviders = ReflectionHelper.GetTypes(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    concreteImplementationProviderInterface.IsAssignableFrom(t))
                .ToList();

            _concreteProviders = new Dictionary<Type, IConcreteImplementationProvider>();
            foreach (var providerType in concreteProviders)
            {
                var provider = providerType.GetConstructor(Type.EmptyTypes).Invoke(null) as IConcreteImplementationProvider;
                if (provider == null) continue;
                if (_concreteProviders.ContainsKey(provider.MockedType))
                    throw new Exception(
                        providerType.FullName + " and " + 
                        _concreteProviders[provider.MockedType].GetType().FullName +
                        " both provide concrete implementations of " + provider.MockedType.FullName);
                _concreteProviders.Add(provider.MockedType, provider);
            }
        }

        public T SetupMock<T>() where T : class
        {
            IConcreteImplementationProvider concreteProvider;
            if (_concreteProviders.TryGetValue(typeof(T), out concreteProvider))
                return concreteProvider.GetImplementation<T>(this);

            // For all other types, use Moq library to mock the interface
            var mock = new Mock<T>(MockBehavior.Loose);
            mock.SetupAllProperties();

            IMockImplementationProvider mockProvider;
            if (_mockProviders.TryGetValue(typeof(T), out mockProvider))
                mockProvider.SetupMock<T>(this, mock);

            return mock.Object;
        }

        protected TMock GetMock<TMock, I>()
        {
            if (_concreteProviders.ContainsKey(typeof(I)))
                return (TMock)_concreteProviders[typeof(I)];

            if (_mockProviders.ContainsKey(typeof(I)))
                return (TMock)_mockProviders[typeof(I)];

            return default(TMock);
        }

    }
}
