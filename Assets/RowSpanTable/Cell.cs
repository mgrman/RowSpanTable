using UnityEngine;

namespace RowSpanTable
{
    public class Cell : MonoBehaviour
    {
        [SerializeField] 
        private int colSpan = 1;

        [SerializeField] 
        private int rowSpan = 1;

        public int ColSpan => Mathf.Max(1, colSpan);
        public int RowSpan => Mathf.Max(1, rowSpan);
    }
}