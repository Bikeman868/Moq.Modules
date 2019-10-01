using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Moq.Modules
{
    public class TestBase : IMockProducer
    {
        private static IDictionary<Type, ConstructorInfo> _mockProviderConstructors;
        private static IDictionary<Type, ConstructorInfo> _concreteProviderConstructors;

        private IDictionary<Type, IMockImplementationProvider> _mockProviders;
        private IDictionary<Type, IConcreteImplementationProvider> _concreteProviders;

        public TestBase()
        {
            EnsureInitialize();
        }

        private void EnsureInitialize()
        {
            if (_mockProviderConstructors == null || _concreteProviderConstructors == null)
                Initialize();
        }

        private void Initialize()
        {
            _mockProviderConstructors = FindConstructors<IMockImplementationProvider>();
            _concreteProviderConstructors = FindConstructors<IConcreteImplementationProvider>();
        }

        private IDictionary<Type, ConstructorInfo> FindConstructors<T>()
        {
            var providerInterface = typeof(T);

            var providers = ReflectionHelper.GetTypes(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    providerInterface.IsAssignableFrom(t))
                .ToList();

            var constructors = new Dictionary<Type, ConstructorInfo>();
            foreach (var providerType in providers)
            {
                var constructor = providerType.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new Exception(providerType.FullName + " must have a default public constructor");

                var instance = (IImplementationProvider)constructor.Invoke(Type.EmptyTypes);
                if (constructors.ContainsKey(instance.MockedType))
                {
                    var conflictingConstructor = constructors[instance.MockedType];
                    var typeToUse = ResolveConflict(instance.MockedType);
                    if (typeToUse == null)
                    {
                        throw new Exception(
                            conflictingConstructor.ReflectedType.FullName + " and " + constructor.ReflectedType.FullName +
                            " both provide mocked implementations of " + instance.MockedType.FullName +
                            ". You can override the ResolveConflict() method to write code that chooses which one to use.");

                    }
                    constructor = typeToUse.GetConstructor(Type.EmptyTypes);
                }
                constructors[instance.MockedType] = constructor;
            }
            return constructors;
        }

        /// <summary>
        /// Override this method to resolve situations where there are two mocks for the same interface.
        /// Do not call the one in the base class if you overrides this method
        /// </summary>
        /// <param name="interfaceType">The interface that was mocked</param>
        /// <returns>Which mock implementation to use</returns>
        protected virtual Type ResolveConflict(Type interfaceType)
        {
            return null;
        }

        protected void Reset()
        {
            _concreteProviders = null;
            _mockProviders = null;
        }

        private void EnsureSetup<T>()
        {
            if (_concreteProviders == null)
                _concreteProviders = new Dictionary<Type, IConcreteImplementationProvider>();

            if (_concreteProviders.ContainsKey(typeof(T)))
                return;

            if (_mockProviders == null)
                _mockProviders = new Dictionary<Type, IMockImplementationProvider>();

            if (_mockProviders.ContainsKey(typeof(T)))
                return;

            ConstructorInfo constructor;
            if (_concreteProviderConstructors.TryGetValue(typeof(T), out constructor))
            {
                var instance = (IConcreteImplementationProvider)constructor.Invoke(Type.EmptyTypes);
                _concreteProviders[typeof(T)] = instance;
                return;
            }

            if (_mockProviderConstructors.TryGetValue(typeof(T), out constructor))
            {
                var instance = (IMockImplementationProvider)constructor.Invoke(Type.EmptyTypes);
                _mockProviders[typeof(T)] = instance;
            }
        }

        /// <summary>
        /// Returns a mock implementation of an interface
        /// </summary>
        /// <typeparam name="T">The type of interface to mock</typeparam>
        public T SetupMock<T>() where T : class
        {
            EnsureSetup<T>();

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
            EnsureSetup<I>();

            if (_concreteProviders.ContainsKey(typeof(I)))
                return (TMock)_concreteProviders[typeof(I)];

            if (_mockProviders.ContainsKey(typeof(I)))
                return (TMock)_mockProviders[typeof(I)];

            return default(TMock);
        }

    }
}
