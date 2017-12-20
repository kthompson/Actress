using System;
using System.Collections.Generic;

namespace Actress
{
    class Observable<T> : IObservable<T>, IObserver<T>, IDisposable
    {
        public Observable()
        {
            _observers = new List<IObserver<T>>();
        }

        private readonly List<IObserver<T>> _observers;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);

            return new Unsubscriber(_observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<T>> _observers;
            private readonly IObserver<T> _observer;

            public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }

        public void OnNext(T item)
        {
            CallOnObservers(observer => observer.OnNext(item));
        }

        public void OnError(Exception error)
        {
            CallOnObservers(observer => observer.OnError(error));
        }

        public void OnCompleted()
        {
            CallOnObservers(observer => observer.OnCompleted());
        }

        private void CallOnObservers(Action<IObserver<T>> action)
        {
            foreach (var observer in _observers.ToArray())
                if (_observers.Contains(observer))
                    action(observer);
        }

        public void Dispose()
        {
            this.OnCompleted();
            _observers.Clear();
        }
    }
}