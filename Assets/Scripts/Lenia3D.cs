using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Lenia3D
{
    [SerializeField] public List<Generation> generations;

    public Lenia3D()
    {
        generations = new();
    }
    [Serializable]
    public class Generation
    {
        [FormerlySerializedAs("cells")] [SerializeField]
        public Grid[] grids;
        [SerializeField,Label("Count")]
        private int index;
        public Grid[] cells => grids;

        public Generation(int size)
        {
            grids = new Grid[size];
            index = 0;
        }
        public void Add(Grid item)
        {
            grids[index] = item;
            index++;
        }
        public int Count => index;


        public Grid this[Index i]
        {
            get => grids[i.IsFromEnd ? index - i.Value : i.Value];
        }
    }
    
    [Serializable]
    public class Grid
    {
        [FormerlySerializedAs("cells")] [SerializeField]
        public Row[] rows;
        [SerializeField,Label("Count")]
        private int index;
        public Row[] cells => rows;

        public Grid(int size)
        {
            rows = new Row[size];
            index = 0;
        }

        public void Add(Row row)
        {
            rows[index] = row;
            index++;
        }


        public Row this[Index i]
        {
            get => rows[i.IsFromEnd ? index - i.Value : i.Value];
        }
    }

    [Serializable]
    public class Row
    {
        [SerializeField] public float[] cells;
        [SerializeField,Label("Count")]
        private int index;

        public Row(int size)
        {
            cells = new float[size];
            for (int i = 0; i < size; i++)
            {
                cells[i] = -2;
            }
            index = 0;
        }

        public int Count => index;

        public void Add(float value)
        {
            cells[index] = value;
            index++;
        }

        public float this[Index offset]
        {
            get => cells[offset.IsFromEnd ? index - offset.Value : offset.Value];
        }
    }

}