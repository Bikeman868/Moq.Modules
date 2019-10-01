using System;

namespace Moq.Modules
{
    public interface IImplementationProvider
    {
        Type MockedType { get; }
    }
}
