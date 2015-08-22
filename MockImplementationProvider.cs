using System;

namespace Moq.Modules
{
    public abstract class MockImplementationProvider<T1> : IMockImplementationProvider 
        where T1: class
    {
        public Type MockedType
        {
            get { return typeof(T1); }
        }

        public void SetupMock<T2>(IMockProducer mockProducer, Mock<T2> mock) where T2 : class
        {
            SetupMock(mockProducer, (Mock<T1>)(object)mock);
        }

        protected abstract void SetupMock(IMockProducer mockProducer, Mock<T1> mock);
    }
}
