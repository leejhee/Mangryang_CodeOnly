using System;
using System.Collections.Generic;

public class Heap<T> where T : IComparable<T>
{
    private List<T> heap;

    public Heap()
    {
        heap = new List<T>();
    }

    // 힙에 요소 추가
    public void Add(T item)
    {
        heap.Add(item);
        HeapifyUp(heap.Count - 1);
    }

    // 힙에서 최소값 제거 후 반환(List라서 옮긴 다음 마지막 지움.)
    public T Pop()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Your heap is empty");

        T minValue = heap[0];
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);

        HeapifyDown(0);
        return minValue;
    }

    public T Peek()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Your heap is empty");
        return heap[0];
    }

    public bool Contains(T check)
    {
        return heap.Contains(check);
    }

    public int Count => heap.Count;

    // 힙에서 위로 정렬.
    // 요소 삽입 시 진행하며 끝날 때는 index가 0이 되어있을 것이다.
    private void HeapifyUp(int index)
    {
        int parentIndex = (index - 1) / 2;
        while (index > 0 && heap[index].CompareTo(heap[parentIndex]) < 0)
        {
            Swap(index, parentIndex);
            index = parentIndex;
            parentIndex = (index - 1) / 2;
        }
    }

    // 힙에서 아래로 정렬
    // 요소 삭제 시 진행하며 끝날 때는 각 childIndex가 heap.count이상일 것이다.
    private void HeapifyDown(int index)
    {
        int smallest = index;
        int leftChildIndex = 2 * index + 1;
        int rightChildIndex = 2 * index + 2;

        if (leftChildIndex < heap.Count && heap[leftChildIndex].CompareTo(heap[smallest]) < 0)
        {
            smallest = leftChildIndex;
        }

        if (rightChildIndex < heap.Count && heap[rightChildIndex].CompareTo(heap[smallest]) < 0)
        {
            smallest = rightChildIndex;
        }

        if (smallest != index)
        {
            Swap(index, smallest);
            HeapifyDown(smallest);
        }
    }

    private void Swap(int index1, int index2)
    {
        T temp = heap[index1];
        heap[index1] = heap[index2];
        heap[index2] = temp;
    }
}

