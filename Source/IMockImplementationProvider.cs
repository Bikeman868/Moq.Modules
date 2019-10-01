using System;

namespace Moq.Modules
{
    public interface IMockImplementationProvider
    {
        Type MockedType { get; }
        void SetupMock<T>(IMockProducer mockProducer, Mock<T> mock) where T : class;
    }
}
