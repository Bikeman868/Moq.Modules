using System;
using ExampleUsage.Classes;
using ExampleUsage.Interfaces;
using ExampleUsage.Mocks;
using Moq.Modules;
using NUnit.Framework;

namespace ExampleUsage.Tests
{
    [TestFixture]
    public class IncrementerTests: TestBase
    {
        private Incrementer _incrementer;

        [SetUp]
        public void Setup()
        {
            Reset();

            _incrementer = new Incrementer(SetupMock<IPermissions>());
        }

        protected override Type ResolveConflict(Type interfaceType)
        {
            if (interfaceType == typeof(IPermissions))
                return typeof(MockPermissionsV1);

            return base.ResolveConflict(interfaceType);
        }

        [Test]
        public void Should_increment_when_allowed()
        {
            var mockPermissions = GetMock<MockPermissionsV1, IPermissions>();
            mockPermissions.IsAllowed = true;

            Assert.AreEqual(10, _incrementer.Increment(9));
        }

        [Test]
        public void Should_not_increment_when_not_allowed()
        {
            var mockPermissions = GetMock<MockPermissionsV1, IPermissions>();
            mockPermissions.IsAllowed = false;

            Assert.AreEqual(9, _incrementer.Increment(9));
        }
    }
}
