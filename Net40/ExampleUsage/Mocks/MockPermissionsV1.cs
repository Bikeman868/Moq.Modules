using ExampleUsage.Interfaces;
using Moq;
using Moq.Modules;

namespace ExampleUsage.Mocks
{
    public class MockPermissionsV1: MockImplementationProvider<IPermissions>
    {
        public bool IsAllowed;

        protected override void SetupMock(IMockProducer mockProducer, Mock<IPermissions> mock)
        {
            mock.SetupGet(s => s.IsAllowed).Returns(() => IsAllowed);
        }
    }
}
