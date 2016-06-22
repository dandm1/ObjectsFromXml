using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectsFromXml
{
    class MultiEnumerable<T> : IEnumerable<T>
    {
        internal List<ObjectOrEnumerable<T>> _items;

        public void Add(T item)
        {
            _items.Add(new ObjectOrEnumerable<T>(item));
        }

        public void Add(IEnumerable<T> list)
        {
            _items.Add(new ObjectOrEnumerable<T>(list));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MultiEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MultiEnumerator<T>(this);
        }

        private class ObjectOrEnumerable<U>
        {
            private IEnumerable<U> _enum;
            private U _item;
            private bool _isEnum = false;

            public ObjectOrEnumerable(U item)
            {
                _item = item;
            }

            public ObjectOrEnumerable(IEnumerable<U> enumerable)
            {
                _enum = enumerable;
                _isEnum = true;
            }

            public bool IsEnumerable
            {
                get
                {
                    return _isEnum;
                }
            }

            public U Item
            {
                get
                {
                    return _item;
                }
            }

            public IEnumerable<U> Enumerable
            {
                get
                { return _enum; }
            }
        }

        public class MultiEnumerator<V> : IEnumerator<V>
        {
            private MultiEnumerable<V> _list;
            private IEnumerator<ObjectOrEnumerable<V>> _listItemsEnumerator;
            private IEnumerator<V> _currentItemEnumerator;

            public MultiEnumerator(MultiEnumerable<V> list)
            {
                _list = list;
                _listItemsEnumerator = _list._items.GetEnumerator();
            }

            public V Current
            {
                get
                {
                    if (_listItemsEnumerator.Current.IsEnumerable)
                        return _currentItemEnumerator.Current;
                    else
                        return _listItemsEnumerator.Current.Item;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_listItemsEnumerator.Current.IsEnumerable)
                        return _currentItemEnumerator.Current;
                    else
                        return _listItemsEnumerator.Current.Item;
                }
            }

            public void Dispose()
            {
                if (_listItemsEnumerator.Current.IsEnumerable)
                    _currentItemEnumerator.Dispose();

                _listItemsEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
