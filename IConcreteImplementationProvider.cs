using System;

namespace Moq.Modules
{
    public interface IConcreteImplementationProvider : IImplementationProvider
    {
        T GetImplementation<T>(IMockProducer mockProducer) where T : class;
    }
}
