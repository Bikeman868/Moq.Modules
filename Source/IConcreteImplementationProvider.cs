using System;

namespace Moq.Modules
{
    public interface IConcreteImplementationProvider
    {
        Type MockedType { get; }
        T GetImplementation<T>(IMockProducer mockProducer) where T : class;
    }
}
