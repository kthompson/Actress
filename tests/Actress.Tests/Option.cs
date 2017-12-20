using System;

namespace Actress.Tests
{
    internal class Option
    {
        public static IOption<T> Some<T>(T value)
        {
            return new SomeInstance<T>(value);
        }

        public static IOption<T> None<T>()
        {
            return new NoneInstance<T>();
        }

        class NoneInstance<T> : IOption<T>
        {
            public bool IsNone
            {
                get { return true; }
            }

            public bool IsSome
            {
                get { return false; }
            }

            public T Value
            {
                get { throw new NotSupportedException(); }
            }
        }

        class SomeInstance<T> : IOption<T>
        {
            private T _value;
            public bool IsNone 
            {
                get { return false; }
            }

            public bool IsSome 
            {
                get { return true; }
            }

            public T Value
            {
                get { return _value; }
            }

            public SomeInstance(T value)
            {
                _value = value;
            }
        }
    }
}