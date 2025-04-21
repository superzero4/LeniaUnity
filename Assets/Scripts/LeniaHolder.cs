using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "LeniaHolder", menuName = "ScriptableObjects/LeniaHolder")]
    public class LeniaHolder : ScriptableObject
    {
        public Lenia3D _lenia;

        public Lenia3D lenia
        {
            get => _lenia;
            set
            {
                _lenia = new Lenia3D()
                {
                    generations = value.generations.Select(gen => new Lenia3D.Generation(gen)).ToList()
                };
            }
        }
    }
}