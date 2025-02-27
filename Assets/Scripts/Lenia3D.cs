using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct Lenia3D
{
    [SerializeField]
    public List<Generation> generations;
    [Serializable]
    public class Row
    {
        [SerializeField]
        public List<float> cells;

        public Row()
        {
            cells = new();
        }

        public int Count => cells.Count;

        public void Add(float value)
        {
            cells.Add(value);
        }

        public float this[int offset]
        {
            get => cells[offset];
        }
    }
    [Serializable]
    public class Grid
    {
        [FormerlySerializedAs("cells")] [SerializeField]
        public List<Row> rows;
        public List<Row> cells => rows;
        public Grid()
        {
            rows = new();
        }
        public void Add(Row row)
        {
            rows.Add(row);
        }

        public Row this[Index index]
        {
            get => rows[index];
        }
    }
    [Serializable]
    public class Generation
    {
        [FormerlySerializedAs("cells")] [SerializeField]
        public List<Grid> grids;
        public List<Grid> cells => grids;

        public Generation()
        {
            grids = new();
        }
        //Just forwards to implement the IList so all code remains compatible even with the generation encapsulation

        public void Add(Grid item)
        {
            grids.Add(item);
        }

        public void Clear()
        {
            grids.Clear();
        }
        

        public int Count => grids.Count;

        public bool IsReadOnly => false;

        public Grid this[int index]
        {
            get => grids[index];
            set => grids[index] = value;
        }
    }
}