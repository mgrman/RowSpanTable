using UnityEngine;

namespace RowSpanTable.v2
{

    public class Cell_v2 : MonoBehaviour
    {
        [SerializeField]
        private int colSpan = 1;

        [SerializeField]
        private int rowSpan = 1;

        public int ColSpan => Mathf.Max(1, colSpan);
        public int RowSpan => Mathf.Max(1, rowSpan);
    }
}