namespace Moq.Modules
{
    public interface IMockProducer
    {
        T SetupMock<T>() where T : class;
    }
}
