using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GigaBoy.Components
{
    public class FixedSizeQueue<T> : IEnumerable<T>
    {
        public int Capacity { get; init; }
        public int Count { get; protected set; }

        public readonly T?[] Buffer;
        int queueBaseIndex = 0;
        int queueEndIndex = 0;

        public T this[Index index]
        {
            get
            {
                return Peek(index);
            }
            set
            {
                ReplaceAt(index, value);
            }
        }
        public T this[int index]
        {
            get
            {
                return Peek(index);
            }
            set {
                ReplaceAt(index, value);
            }
        }


        public FixedSizeQueue(int size)
        {
            Capacity = size;
            Buffer = new T?[size];
        }
        public FixedSizeQueue(T?[] buffer)
        {
            Capacity = buffer.Length;
            this.Buffer = buffer;
        }

        public void Enqueue(T element) {
            if (!TryEnqueue(element))
            {
                throw new OverflowException("Could not enqueue an element: The queue is full.");
            }
        }
        public bool TryEnqueue(T element) {
            if (queueBaseIndex == queueEndIndex && Count != 0)
            {
                return false;
            }
            Buffer[queueEndIndex] = element;
            queueEndIndex = (queueEndIndex + 1) % Capacity;
            ++Count;
            return true;
        }

        public T Dequeue() {
            if (TryDequeue(out T? element)) {
#pragma warning disable CS8603
                return element; // Under normal single-threaded circumstances a null value should be impossible here, unless the element at this index in the queue was enqueued as a null value. Either way we can ignore the warning here as long as this is only used in single-threaded code. It can be made thread-safe with locks.
#pragma warning restore CS8603
            }
            throw new IndexOutOfRangeException("Could not dequeue an element: The queue is empty.");
        }
        public bool TryDequeue(out T? element) {

            if (Count <= 0)
            {
                element = default;
                return false;
            }
            element = Buffer[queueBaseIndex];
            Buffer[queueBaseIndex] = default;
            queueBaseIndex = (queueBaseIndex + 1) % Capacity;
            --Count;
            return true;
        }

        public T Peek(Index indexer) {
            return Peek(indexer.GetOffset(Count));
        }
        public T Peek(int index) {
            if (TryPeek(index, out T? value)) {
#pragma warning disable CS8603 // Możliwe zwrócenie odwołania o wartości null.
                return value;
#pragma warning restore CS8603 // Możliwe zwrócenie odwołania o wartości null.
            }
            throw new IndexOutOfRangeException("Could not peek the queue: The index is out of range of the queue.");
        }
        public bool TryPeek(Index index, out T? value) {
            return TryPeek(index.GetOffset(Count),out value);
        }
        public bool TryPeek(int index,out T? value) {
            if (index >= Count) {
                value = default;
                return false;
            }
            value = Buffer[(queueBaseIndex + index) % Capacity];
            return true;
        }

        public void ReplaceAt(Index indexer, T value)
        {
            ReplaceAt(indexer.GetOffset(Count),value);
        }
        public void ReplaceAt(int index, T value)
        {
            if (!TryReplaceAt(index, value))
            {
                throw new IndexOutOfRangeException("Could not replace element in the queue: The index is out of range of the queue.");
            }
        }
        public bool TryReplaceAt(Index index, T value)
        {
            return TryReplaceAt(index.GetOffset(Count), value);
        }
        public bool TryReplaceAt(int index, T value)
        {
            if (index >= Count || index < 0)
            {
                return false;
            }
            Buffer[(queueBaseIndex + index) % Capacity] = value;
            return true;
        }
        
        public void TrimToSize(int size) {
            if (size < 0 || size > Capacity) return;

            while (Count > size) {
                TryDequeue(out _);
            }
        }
        public void Clear() {
            Array.Clear(Buffer, 0, Capacity);
            queueBaseIndex = 0;
            queueEndIndex = 0;
            Count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Count < 0) yield break;
            int baseIndex = queueBaseIndex;
            while (baseIndex != queueEndIndex) {
#pragma warning disable CS8603 // Możliwe zwrócenie odwołania o wartości null.
                yield return Buffer[baseIndex++ % Capacity];
#pragma warning restore CS8603 // Możliwe zwrócenie odwołania o wartości null.
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
