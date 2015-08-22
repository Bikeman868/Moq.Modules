# Moq.Modules
A mechanism for library writers to provide mocks to users of their library.

This NuGet package solves a couple of problems

1. If you build an application that depends on a library, you can either wrap that
   library in your own interface, then mock that interface in your unit tests, or
   you can make calls to the library in your unit tests. Both appraches have problems
   a. Wrapping the library in your own interface is a lot of work.
   b. Calling the library from tests makes it hard to test all the possible use cases.
   c. The library might have additional dependencies, for example on a database or a service.

2. If you write mocks within each unit test, where you have multiple classes that depend on the
   same interface, you end up mockingthe same interface multiple times, and maintaining all 
   those mocks is time consuming.

This package solves both of these problems, by allowing library developers to distribute mocks
with thsir library, so that users of their library can mock the library in their unit tests.

For example if you install the [Prius ORM](https://github.com/Bikeman868/Prius) and use it to
access your database, you can install the Prius.Mocks NuGet package into your unit test project
and mock Prius in your tests. This allows you to run stand-alone unit tests with mocked data
that do not depend on a database connection.

## Example of writing a re-usable mock using Moq
This class uses Moq to provide a mock implementation of an ILog interface. In this case it
logs to the trace output, which NUnit will include in the test results.

    public class MockLog: MockImplementationProvider<ILog>
    {
        protected override void SetupMock(IMockProducer mockProducer, Mock<ILog> mock)
        {
            mock
                .Setup(m => m.WriteLine(It.IsAny<object[]>()))
                .Callback<object[]>(
                    p => 
                    {
                        Trace.WriteLine(p.Aggregate("", (s, o) => o == null ? s : s + o.ToString()));
                    });
            mock
                .Setup(m => m.Write(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<Exception, string, object[]>(
                    (ex, subject, p) => 
                    {
                        var line = "EXCEPTION: " + subject + " " + ex.Message;
                        if (p != null)
                            p.Aggregate(line + " ", (s, o) => o == null ? s : s + o.ToString());
                        Trace.WriteLine(line);
                    });
        }
    }

## Example of writing a re-useable mock using a class
In this example, the interface is mocked using an instance of the real implementation, rather than 
using Moq. The unit tests that use this mock will not know the difference, you should choose 
the style of mocking that suits your situation.

    public class MockQueueFactory: ConcreteImplementationProvider<IQueueFactory>
    {
        protected override IQueueFactory GetImplementation(IMockProducer mockProducer)
        {
            return new QueueFactory().Initialize();
        }
    }

## Making mocks available
You just have to include the assembly in your project for the mocks to be picked up and used
by your unit tests. The base class that unit tests derrive from will use reflection to find
and load the mocks automatically. This means that library authors just need to provide a NuGet
package containg mocks, and installing this package will instantly make all the mocks available
to unit tests without any additional coding.

## Writing unit tests
To use this package in your unit tests:

1. Install the Moq.Modules package into your unit test project.
2. Add `using Moq.Modules;` at the top of each unit test source file.
3. Change your unit test class to inherit from `Moq.Modules.TestBase`.
4. In the initialization method of your unit test, where you construct the object under test,
   call the `SetupMock<T>()` for each parameter to the constructor. If there is a mock 
   implementation available it will use it, otherwise it will use Moq to return a mock
   with no behaviour set up.
5. Write your unit tests as usual

    [TestFixture]
    public class ExternalHealthCheckerTests : TestBase
    {
        private IExternalServiceHealthChecker _externalServiceHealthChecker;

        [SetUp]
        public void SetUp()
        {
            var dictionaryFactory = SetupMock<IDictionaryFactory>();
            var timeSource = SetupMock<ITimeSource>();
            var workScheduler = SetupMock<IWorkScheduler>();
            var errorReporter = SetupMock<IErrorReporter>();
            var webClientFactory = SetupMock<IWebClientFactory>();
            var historyBucketQueueFactory = SetupMock<IHistoryBucketQueueFactory>();
            var performanceTimerFactory = SetupMock<IPerformanceTimerFactory>();
            var log = SetupMock<ILog>();

            _externalServiceHealthChecker = new ExternalServiceHealthChecker(
                dictionaryFactory,
                timeSource,
                workScheduler,
                errorReporter,
                webClientFactory,
                historyBucketQueueFactory,
                performanceTimerFactory,
                log)
                .Initialize();
        }
	}

## How to change the behaviour of the mock
In your unit tests you will want to test different use cases by altering the behaviour
of the mocked implementation. For example check what happens if the mocked class throws
an exception, or returns different responses. This is usually where you set up different
mock behaviour by setting up the mock inside each unit test.

This library takes a different approach. With this package installed, the idea is to make
re-usable mocks that can have their behaviour modified. Your unit test can still use all
the power of Moq where you need it.

One of the reasons for suggesting this way of working, is this. If I write a NuGet package
and distribute it to 100 other developers, each of those developers might go ahead and 
write a whole bunch of mocks for the interfaces in my library. That's a lot of dev hours.
If I write a re-usable mock, this moght be a bit more work for me, but the other 100
developers that use my library don't have to do any work at all. I think that's a net
win for the community as a whole.

This is an example of a re-usable mock that can have it's behaviour controlled by the
unit test. I never use the `DataTime` class to retrieve the current time directly,
because it's hard to test that the code counts time correctly. Instead I define an
`ITimeSource` interface and inject this into classes that need it. Then I can move
time back and forth in my unit tests to test classes.

This is the definition of `ITimeSource`

    public interface ITimeSource
    {
        double UnixTimeUtc { get; }
        DateTime NowUtc { get; }
    }

This is my re-usable mock for this interface

    public class MockTimeSource: MockImplementationProvider<ITimeSource>
    {
        private DateTime _timeNowUtc;

        public MockTimeSource()
        {
            var now = DateTime.UtcNow;
            SetTimeTo(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0));
        }

        public void SetTimeTo(DateTime dateTime)
        {
            _timeNowUtc = dateTime;
        }

        public void AddTime(TimeSpan time)
        {
            _timeNowUtc += time;
        }

        public void AddSeconds(double seconds)
        {
            AddTime(TimeSpan.FromSeconds(seconds));
        }

        public void AddSeconds(int seconds)
        {
            AddTime(TimeSpan.FromSeconds(seconds));
        }

        protected override void SetupMock(IMockProducer mockProducer, Moq.Mock<ITimeSource> mock)
        {
            mock.Setup(ts => ts.NowUtc).Returns(() => _timeNowUtc);
            mock.Setup(ts => ts.UnixTimeUtc).Returns(() => _timeNowUtc.ToUnixTimestamp(true));
        }

This is an example test that uses this mock

        public void Should_check_google()
        {
            var sla = new ServiceLevelAgreementDto
            {
                ExpectedAverageResponseTime = TimeSpan.FromSeconds(0.5),
                AcceptableResponseTimeDeviation = TimeSpan.FromSeconds(0.25),
                DelayBeforeMarkDown = TimeSpan.FromSeconds(10),
                DelayBeforeMarkUp = TimeSpan.FromSeconds(5),
            };

            var isUp = false;
            _externalServiceHealthChecker.InstallHealthCheck(
                sla, 
                TimeSpan.FromSeconds(1), 
                new GoogleChecker(), 
                () => isUp = false,
                () => isUp = true);

            var mockTimeSource = GetMock<MockTimeSource, ITimeSource>();
            var endTime = DateTime.UtcNow + TimeSpan.FromSeconds(60);
            while (DateTime.UtcNow < endTime)
            {
                mockTimeSource.SetTimeTo(DateTime.UtcNow);
                Thread.Sleep(10);
            }

			Assert.IsTrue(isUp);
        }

The important line of code is `var mockTimeSource = GetMock<MockTimeSource, ITimeSource>();` 
which retrieves a reference to the MockTimeSource instance, and allows the test to change the
time.