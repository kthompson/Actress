using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Actress.Tests
{
    interface IOption<T>
    {
        T Value { get; }
        bool IsNone { get; }
        bool IsSome { get; }
    }
}
