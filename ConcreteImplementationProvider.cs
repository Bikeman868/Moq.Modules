using System;

namespace Moq.Modules
{
    public abstract class ConcreteImplementationProvider<T> : IConcreteImplementationProvider 
        where T: class
    {
        public Type MockedType
        {
            get { return typeof(T); }
        }

        public TImpl GetImplementation<TImpl>(IMockProducer mockProducer) where TImpl : class
        {
            return (TImpl)(object)GetImplementation(mockProducer);
        }

        protected abstract T GetImplementation(IMockProducer mockProducer);
    }
}
