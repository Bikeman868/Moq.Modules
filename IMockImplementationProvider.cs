using System;

namespace Moq.Modules
{
    public interface IMockImplementationProvider : IImplementationProvider
    {
        void SetupMock<T>(IMockProducer mockProducer, Mock<T> mock) where T : class;
    }
}
