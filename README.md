# Moq.Modules
A mechanism for library authors to provide mocks to users of their library.

This NuGet package solves a couple of problems

1. If you build an application that depends on a library, you can either wrap that
   library in your own interface, then mock that interface in your unit tests, or
   you can make calls to the library in your unit tests. Both approaches have problems.

   a. Wrapping the library in your own interface is a lot of work.

   b. Calling the library from tests makes it hard to test all the possible use cases.

   c. The library might have additional dependencies, for example on a database or a service.

2. If you write mocks within each unit test, where you have multiple classes that depend on the
   same interface, you end up mocking the same interface multiple times, and maintaining all 
   those mocks is time consuming.

This package solves both of these problems by allowing library authors to distribute mocks
with their library, so that users of their library can use a mock of the library in their unit
tests.

For example if you install the [Prius ORM](https://github.com/Bikeman868/Prius) and use it to
access your database, you can install the Prius.Mocks NuGet package into your unit test project
and use a mock of Prius in your tests. This allows you to run stand-alone unit tests with mocked
data that do not depend on a database connection.

## Writing a re-usable mock using Moq
The example below uses Moq.Modules and Moq to provide a mock implementation of an ILog interface.
In this example the mock implementation sends log output to the trace output - which NUnit will 
include in the test results.

    using Moq;
    using Moq.Modules;
	
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

The key elements of this example are:

1. Use the NuGet package manager to install the Moq.Modules package into your mocks project.
2. Add a using statement for Moq.Modules and Moq.
3. Your mock should inherit from `MockImplementationProvider<T>` where `T` is the interface 
   that you are writing a mock for (`ILog` in the example above).
4. You must override the abstract `SetupMock` method of `MockImplementationProvider<T>`.
   This is where you use Moq to define the behaviour of your mock, using all the features of 
   Moq you are familiar with.
5. If your mock depends on other mocks, then you can use the `mockProducer` parameter to 
   get access to the mocks of the other interfaces that you need. Note that this is not 
   illustrated in the example above.

## Writing a re-useable mock using a class
This is and example of mocking an interface by writing a class that implements the interface 
rather than using Moq. The way that Moq.Modules works means that you can switch between
using Moq and writing a class, and the unit tests that use the mock will still compile and run
without any changes.

    using Moq.Modules;
	
    public class MockQueueFactory: ConcreteImplementationProvider<IQueueFactory>
    {
        protected override IQueueFactory GetImplementation(IMockProducer mockProducer)
        {
            return new QueueFactory().Initialize();
        }
    }

The key elements of this example are:

1. Your mock should inherit from `ConcreteImplementationProvider<T>` where `T` is the interface that you
   are writing a mock for (`IQueueFactory` in the example above).
2. You must override the abstract `GetImplementation` method, and a return an object that implements the
   interface that is being mocked.
3. If your mock depends on other mocks, then you can use the `mockProducer` parameter to get access
   to the mocks of the other interfaces you need. Note that this is not illustrated in the example above.

## Making mocks available to unit tests
If you include the assembly containing your mocks in the project that contains the unit tests, these mocks
will be available to those unit tests. The base class that unit tests derive from will use reflection to find
and load the mocks automatically. This means that library authors just need to provide a NuGet
package containing mocks, and installing this package into the unit test project will instantly make all the 
mocks available to unit tests without any additional coding.

## Writing unit tests
To use this package in your unit tests:

1. Install the `Moq.Modules` package into your unit test project.
2. Add `using Moq.Modules;` at the top of each unit test source file.
3. Change your unit test class to inherit from `Moq.Modules.TestBase`.
4. In the initialization method of your unit test, where you construct the object under test,
   call the `SetupMock<T>()` method to get a mock implementation of each interface that the
   object depends on. If there is a mock implementation available for this interface 
   Moq.Modules will use it, otherwise it will use Moq to return a mock with no behaviour set up.
5. Write your unit tests as you normally would

Example unit test setup:
```
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
```

## How to change the behaviour of the mock
In your unit tests you will want to test different use cases by altering the behaviour
of the mocked implementation. For example check what happens if the mocked class throws
an exception, or returns different responses. This is usually where you set up different
mock behaviour by using the features of Moq inside each unit test.

This library offers an alternate approach. With this package installed you can make
re-usable mocks that can have their behaviour modified. This means that unit tests can
be very simple, or they can still use Moq to define behaviours where this makes sense.

One of the reasons for suggesting this approach, is this: If I write a NuGet package
and distribute it to 100 other developers, each of those developers go ahead and 
write a whole bunch of mocks for the interfaces in my library, that's a lot of dev hours.
As the package author, if I write a set of re-usable mocks and distribute them with my
package, this will be a bit more work for me, but the other 100 developers that use my 
library don't have to do any work at all. I think that's a net win for the community as
a whole.

Below is an example of a re-usable mock that can have it's behaviour controlled by the
unit test. In my code I never use the `DataTime` class to retrieve the current time
directly because it's hard to test that the code counts time correctly. Instead I define
an `ITimeSource` interface and inject this into classes that need it, then I can mock
this interface and move time back and forth in my unit tests.

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
which retrieves a reference to the `MockTimeSource` instance, and allows the test to change 
the time as the test runs.
