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
            var mockImplementationProviderOnterface = typeof(IMockImplementationProvider);

            _mockProviders = ReflectionHelper.GetTypes(
                t => 
                    t.IsClass && 
                    !t.IsAbstract &&
                    mockImplementationProviderOnterface.IsAssignableFrom(t))
                .Select(t => t.GetConstructor(Type.EmptyTypes).Invoke(null))
                .Cast<IMockImplementationProvider>()
                .ToDictionary(p => p.MockedType);

            var concreteImplementationProviderOnterface = typeof(IConcreteImplementationProvider);

            _concreteProviders = ReflectionHelper.GetTypes
                (t => 
                    t.IsClass && 
                    !t.IsAbstract &&
                    concreteImplementationProviderOnterface.IsAssignableFrom(t))
                .Select(t => t.GetConstructor(Type.EmptyTypes).Invoke(null))
                .Cast<IConcreteImplementationProvider>()
                .ToDictionary(p => p.MockedType);
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
