using System;

namespace Actress.Tests
{
    abstract class Option<T>
    {
        public abstract T Value { get; }
        public abstract bool IsSome { get; }
        public bool IsNone => !IsSome;

        public static implicit operator Option<T>(Option.NoneType rhs) => Option.OfNone<T>();
    }

    class Option
    {
        public class NoneType
        {
        }

        public static Option<T> Some<T>(T value)
        {
            return new SomeInstance<T>(value);
        }


        public static Option<T> OfNone<T>()
        {
            return new NoneInstance<T>();
        }

        public static readonly NoneType None = new NoneType();

        class NoneInstance<T> : Option<T>
        {
            public override bool IsSome => false;

            public override T Value => throw new NotSupportedException();
        }

        class SomeInstance<T> : Option<T>
        {
            public override bool IsSome => true;

            public override T Value { get; }

            public SomeInstance(T value)
            {
                Value = value;
            }
        }
    }
}